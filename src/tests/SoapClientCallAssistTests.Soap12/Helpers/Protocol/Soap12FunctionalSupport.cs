using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapTestService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Protocol;

internal static class Soap12FunctionalSupport
{

    internal const string CorrelationHeader = "X-Test-Correlation";

    internal static readonly XNamespace Service = SoapAssert.ServiceNs;

    internal static readonly XNamespace Soap12 = SoapAssert.Soap12Ns;

    internal static string NewCorrelationId() => Guid.NewGuid().ToString("N");

    internal static Dictionary<string, IEnumerable<string>> CorrelationHeaders(
        string correlationId, IReadOnlyDictionary<string, string>? extraHeaders = null)
    {
        var headers = new Dictionary<string, IEnumerable<string>> { { CorrelationHeader, new[] { correlationId } } };

        if (extraHeaders is null)
            return headers;

        foreach (var extra in extraHeaders)
            headers[extra.Key] = new[] { extra.Value };

        return headers;
    }

    internal static void AssertHeaderReceived(RecordedRequest recorded, string name, string value, string because)
        => Assert.IsTrue(
            recorded.Headers.TryGetValue(name, out var values) && values.Contains(value, StringComparer.Ordinal),
            $"{because} | {name} | {value} | {string.Join(", ", recorded.Headers.Keys)}");

    internal static XElement[] HelloWorldBody()
        => new[] { new XElement(Service + "HelloWorld") };

    internal static XElement[] EchoValueBody(XNamespace ns, string value = "abc", string count = "7")
        => new[]
        {
            new XElement(
                ns + "EchoValue",
                new XElement("value", value),
                new XElement("count", count))
        };

    internal static XElement[] GetProductBody(int id)
        => new[]
        {
            new XElement(
                Service + "GetProduct",
                new XElement(Service + "id", id.ToString()))
        };

    internal static XElement[] AddRecordWithDetailBody(
        string id = "77",
        string partnerId = "177",
        string manufacturerId = "277",
        string supplierId = "377")
        => new[]
        {
            new XElement(
                Service + "AddRecordWithDetail",
                new XElement(
                    Service + "product",
                    new XElement(Service + "Id", id),
                    new XElement(Service + "Code", $"P-{id}"),
                    new XElement(Service + "Name", $"Product {id}"),
                    new XElement(Service + "IsActive", "true"),
                    new XElement(
                        Service + "Detail",
                        new XElement(Service + "PartnerId", partnerId),
                        new XElement(Service + "ManufacturerId", manufacturerId),
                        new XElement(Service + "SupplierId", supplierId)))),
        };

    internal static T Unwrap<T>(IResult<T> result, string what)
    {
        Assert.IsTrue(result.IsSuccess, $"{what} | {Describe(result)}");
        Assert.IsNotNull(result.Response, what);

        return result.Response;
    }

    internal static HttpResponseMessage UnwrapRejected(IResult<HttpResponseMessage> result, string expectedCode, string what)
    {
        Assert.IsFalse(result.IsSuccess, $"{what} | {Describe(result)}");

        Assert.AreEqual(expectedCode, NegativeTestSupport.Messages(result)[0].Key, $"{what} | {Describe(result)}");

        Assert.IsNotNull(result.Response, $"{what} | {Describe(result)}");

        return result.Response;
    }

    internal static string Describe(IResult result) => ResultRendering.Describe(result);

    internal static void AssertFailureMessageContains(IResult result, string expectedText)
    {
        Assert.IsFalse(result.IsSuccess);

        var candidates = result.Messages is null
            ? new List<string>()
            : result.Messages
                .Select(message => message.Message?.Info)
                .Where(info => !string.IsNullOrEmpty(info))
                .Select(info => info!)
                .ToList();

        Assert.IsTrue(
            candidates.Any(candidate => candidate.Contains(expectedText, StringComparison.Ordinal)),
            $"{expectedText} | {Describe(result)}");
    }

    internal static HttpRequestMessage BuildPost(
        ISoapClientEndpoint client,
        IEnumerable<XElement> bodies,
        string correlationId,
        Uri? endpoint = null,
        string? action = null)
        => Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                endpoint ?? SoapServiceFixture.ServiceUri,
                bodies,
                action: action,
                httpClientHeaders: CorrelationHeaders(correlationId)),
            "BuildRequest");

    internal static HttpRequestMessage BuildGet(
        ISoapClientEndpoint client,
        IEnumerable<XElement> bodies,
        string correlationId,
        bool buildGetRequestAsSlashUrl,
        string? action = null)
        => Unwrap(
            client.BuildRequest(
                HttpMethod.Get,
                SoapServiceFixture.ServiceUri,
                bodies,
                action: action,
                httpClientHeaders: CorrelationHeaders(correlationId),
                buildGetRequestAsSlashUrl: buildGetRequestAsSlashUrl),
            "BuildRequest");

    internal static RecordedRequest FindRecorded(string correlationId)
    {
        var matches = SoapServiceFixture.Recorder.Snapshot()
            .Where(entry =>
                entry.Headers.TryGetValue(CorrelationHeader, out var values)
                && values.Contains(correlationId, StringComparer.Ordinal))
            .ToList();

        Assert.AreEqual(
            1,
            matches.Count, $"{CorrelationHeader} | {correlationId} | {SoapServiceFixture.Recorder.Snapshot().Count} | {RequestRecorder.Capacity}");

        return matches[0];
    }

    internal static RecordedRequest AssertRequestWasSoap12OnTheWire(string correlationId)
    {
        var recorded = FindRecorded(correlationId);

        Assert.IsTrue(MediaTypeHeaderValue.TryParse(recorded.ContentType, out var parsed), $"{recorded.ContentType ?? "<null>"}");

        Assert.AreEqual(SoapAssert.Soap12MediaType, parsed!.MediaType, $"{SoapAssert.Soap11MediaType} | {recorded.ContentType}");

        return recorded;
    }

    internal static XElement ParseRoot(string xml)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(xml));

        XDocument document;

        try
        {
            using var reader = SoapXml.CreateReader(new StringReader(xml));
            document = XDocument.Load(reader);
        }
        catch (XmlException exception)
        {
            throw new AssertFailedException($"{exception.Message} | {xml}");
        }

        return document.Root ?? throw new AssertFailedException(xml);
    }

    internal static XElement AssertEnvelopeNamespace(string xml, string expectedNamespace)
    {
        var root = ParseRoot(xml);

        Assert.AreEqual("Envelope", root.Name.LocalName, xml);
        Assert.AreEqual(expectedNamespace, root.Name.NamespaceName, xml);

        return root;
    }

    internal static XElement GetRequestOperationElement(string requestEnvelope)
    {
        var root = SoapAssert.AssertIsSoap12Envelope(requestEnvelope);

        var body = root.Element(Soap12 + "Body")
                   ?? throw new AssertFailedException(requestEnvelope);

        return body.Elements().FirstOrDefault()
               ?? throw new AssertFailedException(requestEnvelope);
    }

    internal static XElement RequireChild(XElement parent, XName name)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var child = parent.Element(name);

        if (child is not null)
            return child;

        var present = parent.Elements().Select(element => element.Name.ToString()).ToArray();

        throw new AssertFailedException($"{parent.Name} | {name} | {string.Join(", ", present)}");
    }

    internal static void AssertChildValue(XElement parent, XName name, string expected)
        => Assert.AreEqual(expected, RequireChild(parent, name).Value, $"{name}");

    internal static string ReadUnsentContent(HttpRequestMessage request)
    {
        Assert.IsNotNull(request.Content);

        return request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
}
