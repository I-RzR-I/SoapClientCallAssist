using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapTestService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers;

internal static class Soap12FunctionalSupport
{

    internal const string CorrelationHeader = "X-Test-Correlation";

    internal static readonly XNamespace Service = SoapAssert.ServiceNs;

    internal static readonly XNamespace Soap12 = SoapAssert.Soap12Ns;

    internal static string NewCorrelationId() => Guid.NewGuid().ToString("N");

    internal static Dictionary<string, IEnumerable<string>> CorrelationHeaders(string correlationId)
        => new() { { CorrelationHeader, new[] { correlationId } } };

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
        Assert.IsTrue(result.IsSuccess, $"{what} failed: {Describe(result)}");
        Assert.IsNotNull(result.Response, $"{what} reported success but carried no value.");

        return result.Response;
    }

    internal static string Describe(IResult result)
    {
        if (result.Messages is null || result.Messages.Count == 0)
            return "<no messages>";

        return string.Join(
            " | ",
            result.Messages.Select(message =>
                $"key=[{message.Key ?? "<null>"}] info=[{message.Message?.Info ?? "<null>"}]"));
    }

    internal static void AssertFailureMessageContains(IResult result, string expectedText)
    {
        Assert.IsFalse(result.IsSuccess, "The result did not fail, so it carries no failure message to assert on.");

        var candidates = result.Messages is null
            ? new List<string>()
            : result.Messages
                .Select(message => message.Message?.Info)
                .Where(info => !string.IsNullOrEmpty(info))
                .Select(info => info!)
                .ToList();

        Assert.IsTrue(
            candidates.Any(candidate => candidate.Contains(expectedText, StringComparison.Ordinal)),
            $"Expected a failure message containing [{expectedText}] but the result reported: {Describe(result)}");
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
            "BuildRequest(POST)");

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
            "BuildRequest(GET)");

    internal static RecordedRequest FindRecorded(string correlationId)
    {
        var matches = SoapServiceFixture.Recorder.Snapshot()
            .Where(entry =>
                entry.Headers.TryGetValue(CorrelationHeader, out var values)
                && values.Contains(correlationId, StringComparer.Ordinal))
            .ToList();

        Assert.AreEqual(
            1,
            matches.Count,
            $"Expected exactly one recorded request carrying {CorrelationHeader}={correlationId}, " +
            $"but the service recorded {matches.Count}. The recorder currently holds " +
            $"{SoapServiceFixture.Recorder.Snapshot().Count} of a maximum {RequestRecorder.Capacity} entries. " +
            "If it is at capacity the entry was most likely evicted by later requests (suspect parallel " +
            "execution), not never sent; if it is below capacity the client genuinely sent nothing, or sent " +
            "the request more than once.");

        return matches[0];
    }

    internal static RecordedRequest AssertRequestWasSoap12OnTheWire(string correlationId)
    {
        var recorded = FindRecorded(correlationId);

        Assert.IsTrue(
            MediaTypeHeaderValue.TryParse(recorded.ContentType, out var parsed),
            $"The request reached the service with an unparseable Content-Type: [{recorded.ContentType ?? "<null>"}].");

        Assert.AreEqual(
            SoapAssert.Soap12MediaType,
            parsed!.MediaType,
            $"The request reached the service with the wrong media type; SOAP 1.1 would send " +
            $"{SoapAssert.Soap11MediaType}. Full header was: [{recorded.ContentType}].");

        return recorded;
    }

    internal static XElement ParseRoot(string xml)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(xml), "Expected an XML document but the content was empty.");

        XDocument document;

        try
        {
            using var reader = SoapXml.CreateReader(new StringReader(xml));
            document = XDocument.Load(reader);
        }
        catch (XmlException exception)
        {
            throw new AssertFailedException($"The content is not well formed XML: {exception.Message}. Content was: {xml}");
        }

        return document.Root ?? throw new AssertFailedException($"The document has no root element. Content was: {xml}");
    }

    internal static XElement AssertEnvelopeNamespace(string xml, string expectedNamespace)
    {
        var root = ParseRoot(xml);

        Assert.AreEqual("Envelope", root.Name.LocalName, $"Expected an Envelope root. Document was: {xml}");
        Assert.AreEqual(
            expectedNamespace,
            root.Name.NamespaceName,
            $"The envelope is in the wrong namespace for the protocol under test. Document was: {xml}");

        return root;
    }

    internal static XElement GetRequestOperationElement(string requestEnvelope)
    {
        var root = SoapAssert.AssertIsSoap12Envelope(requestEnvelope);

        var body = root.Element(Soap12 + "Body")
                   ?? throw new AssertFailedException(
                       $"The request envelope has no SOAP 1.2 Body element. Envelope was: {requestEnvelope}");

        return body.Elements().FirstOrDefault()
               ?? throw new AssertFailedException($"The request envelope Body is empty. Envelope was: {requestEnvelope}");
    }

    internal static XElement RequireChild(XElement parent, XName name)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var child = parent.Element(name);

        if (child is not null)
            return child;

        var present = parent.Elements().Select(element => element.Name.ToString()).ToArray();

        throw new AssertFailedException(
            $"Element {parent.Name} has no direct child named {name}. " +
            $"Direct children present: [{string.Join(", ", present)}].");
    }

    internal static void AssertChildValue(XElement parent, XName name, string expected)
        => Assert.AreEqual(expected, RequireChild(parent, name).Value, $"Unexpected value for {name}.");

    internal static string ReadUnsentContent(HttpRequestMessage request)
    {
        Assert.IsNotNull(request.Content, "The built request carries no content, so no envelope was produced.");

        return request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
}
