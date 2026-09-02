using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

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
            $"SendRequestAsync({protocol}, EchoValue)");

        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope for {protocol} was: {envelope}");

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

        Assert.IsNotNull(request.RequestUri, $"The GET build for {protocol} produced no request URI.");
        Assert.AreEqual(expected, request.RequestUri.ToString(), $"RequestUri.ToString() (unescaped form) for {protocol}.");

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync({protocol}, GET EchoValue, query form)");

        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope for {protocol} was: {envelope}");

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

        Assert.IsNotNull(request.RequestUri, $"The GET build for {protocol} produced no request URI.");
        Assert.AreEqual(expected, request.RequestUri.ToString(), $"RequestUri.ToString() (unescaped form) for {protocol}.");

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync({protocol}, GET EchoValue, slash form)");

        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope for {protocol} was: {envelope}");

        SoapAssert.AssertElementValue(CrossProtocolSupport.GetBodyChild(protocol, envelope), "EchoValueResult", "abc:7");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    public void SendRequest_WhenTheServiceOutlastsTheTimeout_FailsWithoutThrowing_Test(SoapProtocolType protocol)
    {
        var client = CrossProtocolSupport.CreateDirectClient(protocol);

        var timeoutApplied = client.SetClientTimeout(ShortTimeout);

        Assert.IsTrue(
            timeoutApplied.IsSuccess,
            $"The timeout must be accepted for {protocol}. Got: {NegativeTestSupport.Describe(timeoutApplied)}");

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                CrossProtocolSupport.EndpointFor(protocol),
                NegativeTestSupport.Bodies("SlowOp", ("delayMs", SlowOperationDelayMs))),
            $"BuildRequest(POST, {protocol}, SlowOp)");

        var stopwatch = Stopwatch.StartNew();

        var result = client.SendRequest(request);

        stopwatch.Stop();

        Assert.IsFalse(result.IsSuccess, $"A timed-out {protocol} call must be a failure. Got: {NegativeTestSupport.Describe(result)}");

        Assert.AreEqual(
            SendFailureMessage,
            NegativeTestSupport.FirstMessageInfo(result),
            $"A timeout must be reported as a send failure for {protocol}.");

        Assert.IsTrue(
            stopwatch.Elapsed < TimeoutUpperBound,
            $"The {protocol} call was configured to give up after {ShortTimeout.TotalMilliseconds} ms against a " +
            $"{SlowOperationDelayMs} ms delay, but it took {stopwatch.Elapsed}. The failure was therefore " +
            "probably not the timeout.");
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
            $"SendRequestAsync({protocol}, HelloWorld)");

        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope for {protocol} was: {envelope}");

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXmlNodeResponseBody(envelope),
            $"GetXmlNodeResponseBody({protocol}, HelloWorld response)");

        Assert.AreEqual("HelloWorldResponse", node.LocalName, $"Extracted the wrong node for {protocol}. Envelope was: {envelope}");
        Assert.AreEqual(SoapAssert.ServiceNs, node.NamespaceURI, $"The extracted node must stay in the service namespace for {protocol}.");
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
            $"SendRequestAsync({protocol}, HelloWorld)");

        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope for {protocol} was: {envelope}");

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXNodeResponseBody(envelope),
            $"GetXNodeResponseBody({protocol}, HelloWorld response)");

        var document = node as XDocument;

        Assert.IsNotNull(document, $"Expected an XDocument but got {node.GetType().Name} for {protocol}.");
        Assert.IsNotNull(document!.Root, $"The extracted document has no root element for {protocol}.");

        Assert.AreEqual(
            (Soap12FunctionalSupport.Service + "HelloWorldResponse").ToString(),
            document.Root!.Name.ToString(),
            $"Extracted the wrong element for {protocol}. Envelope was: {envelope}");

        SoapAssert.AssertElementValue(document.Root, "HelloWorldResult", "Hello World");
    }
}
