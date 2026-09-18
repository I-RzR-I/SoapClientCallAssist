using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

[TestClass]
public sealed class CrossProtocolReaderTests
{

    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(500);

    private const string SlowOperationDelayMs = "8000";

    private static readonly TimeSpan TimeoutUpperBound = TimeSpan.FromSeconds(5);

    private const string SendFailureMessage = "An error occurred while trying to send SOAP request message.";

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    public async Task SendRequestAsync_EchoValue_RoundTripsBothArgumentsOverTheWire_Test(SoapProtocolType protocol)
    {
        var client = CrossProtocolSupport.CreateClient(protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            protocol,
            client,
            Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync {protocol}");

        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{protocol} | {envelope}");

        var payload = CrossProtocolSupport.GetBodyChild(protocol, envelope);
        SoapAssert.AssertElementValue(payload, "EchoValueResult", "abc:7");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    public async Task BuildAndSendGet_QueryForm_AddressesTheOperationAndEchoesBothArguments_Test(SoapProtocolType protocol)
    {
        var client = CrossProtocolSupport.CreateClient(protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildGet(
            protocol,
            client,
            Soap12FunctionalSupport.EchoValueBody(XNamespace.None),
            correlationId,
            buildGetRequestAsSlashUrl: false);

        var expected = $"{CrossProtocolSupport.EndpointFor(protocol)}/EchoValue?value=abc&count=7";

        Assert.IsNotNull(request.RequestUri, $"{protocol}");
        Assert.AreEqual(expected, request.RequestUri.ToString(), $"{protocol}");

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync {protocol}");

        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{protocol} | {envelope}");

        SoapAssert.AssertElementValue(CrossProtocolSupport.GetBodyChild(protocol, envelope), "EchoValueResult", "abc:7");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    public async Task BuildAndSendGet_SlashForm_AppendsValuesAsPathSegmentsInOrder_Test(SoapProtocolType protocol)
    {
        var client = CrossProtocolSupport.CreateClient(protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildGet(
            protocol,
            client,
            Soap12FunctionalSupport.EchoValueBody(XNamespace.None),
            correlationId,
            buildGetRequestAsSlashUrl: true);

        var expected = $"{CrossProtocolSupport.EndpointFor(protocol)}/EchoValue/abc/7";

        Assert.IsNotNull(request.RequestUri, $"{protocol}");
        Assert.AreEqual(expected, request.RequestUri.ToString(), $"{protocol}");

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync {protocol}");

        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{protocol} | {envelope}");

        SoapAssert.AssertElementValue(CrossProtocolSupport.GetBodyChild(protocol, envelope), "EchoValueResult", "abc:7");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    public void SendRequest_WhenTheServiceOutlastsTheTimeout_FailsWithoutThrowing_Test(SoapProtocolType protocol)
    {
        var client = CrossProtocolSupport.CreateDirectClient(protocol);

        var timeoutApplied = client.SetClientTimeout(ShortTimeout);

        Assert.IsTrue(timeoutApplied.IsSuccess, $"{protocol} | {NegativeTestSupport.Describe(timeoutApplied)}");

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                CrossProtocolSupport.EndpointFor(protocol),
                NegativeTestSupport.Bodies("SlowOp", ("delayMs", SlowOperationDelayMs))),
            $"BuildRequest {protocol}");

        var stopwatch = Stopwatch.StartNew();

        var result = client.SendRequest(request);

        stopwatch.Stop();

        Assert.IsFalse(result.IsSuccess, $"{protocol} | {NegativeTestSupport.Describe(result)}");

        Assert.AreEqual(SendFailureMessage, NegativeTestSupport.FirstMessageInfo(result), $"{protocol}");

        Assert.IsTrue(
            stopwatch.Elapsed < TimeoutUpperBound,
            $"{protocol} | {ShortTimeout.TotalMilliseconds} | {SlowOperationDelayMs} | {stopwatch.Elapsed}");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    public async Task GetXmlNodeResponseBody_WithLiveHelloWorldResponse_ExtractsTheOperationResponseElement_Test(SoapProtocolType protocol)
    {
        var client = CrossProtocolSupport.CreateClient(protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            protocol,
            client,
            Soap12FunctionalSupport.HelloWorldBody(),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync {protocol}");

        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{protocol} | {envelope}");

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXmlNodeResponseBody(envelope),
            $"GetXmlNodeResponseBody {protocol}");

        Assert.AreEqual("HelloWorldResponse", node.LocalName, $"{protocol} | {envelope}");
        Assert.AreEqual(SoapAssert.ServiceNs, node.NamespaceURI, $"{protocol}");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    public async Task GetXNodeResponseBody_WithLiveHelloWorldResponse_ExtractsTheOperationResponseElement_Test(SoapProtocolType protocol)
    {
        var client = CrossProtocolSupport.CreateClient(protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            protocol,
            client,
            Soap12FunctionalSupport.HelloWorldBody(),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync {protocol}");

        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{protocol} | {envelope}");

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXNodeResponseBody(envelope),
            $"GetXNodeResponseBody {protocol}");

        var document = node as XDocument;

        Assert.IsNotNull(document, $"{node.GetType().Name} | {protocol}");
        Assert.IsNotNull(document!.Root, $"{protocol}");

        Assert.AreEqual(
            (Soap12FunctionalSupport.Service + "HelloWorldResponse").ToString(),
            document.Root!.Name.ToString(),
            $"{protocol} | {envelope}");

        SoapAssert.AssertElementValue(document.Root, "HelloWorldResult", "Hello World");
    }
}
