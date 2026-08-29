using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class Soap12ResponseExtractionTests
{

    private const string NoSingleBodyMessage = "No or more than one SOAP Body in response.";

    [TestMethod]
    public async Task GetXmlNodeResponseBody_WithDefaultArguments_ExtractsTheOperationResponseElement()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var envelope = await CallHelloWorldAsync(client, prefix: null);

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXmlNodeResponseBody(envelope),
            "GetXmlNodeResponseBody(default prefix)");

        Assert.AreEqual("HelloWorldResponse", node.LocalName, $"Extracted the wrong node. Envelope was: {envelope}");
        Assert.AreEqual(SoapAssert.ServiceNs, node.NamespaceURI, "The extracted node must stay in the service namespace.");

        SoapAssert.AssertElementValue(
            Soap12FunctionalSupport.ParseRoot(node.OuterXml),
            "HelloWorldResult",
            "Hello World");
    }

    [TestMethod]
    public async Task GetXmlNodeResponseBody_WhenTheResponseUsesTheSPrefix_FailsAlthoughTheEnvelopeIsValid()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var envelope = await CallHelloWorldAsync(client, prefix: "s");

        var root = SoapAssert.AssertIsSoap12Envelope(envelope);

        Assert.AreEqual(
            "s",
            root.GetPrefixOfNamespace(Soap12FunctionalSupport.Soap12),
            $"The service did not honour the prefix switch, so this test is not exercising what it claims. Envelope was: {envelope}");

        Assert.IsNotNull(
            root.Element(Soap12FunctionalSupport.Soap12 + "Body"),
            $"The response has no Body in the SOAP 1.2 namespace, so the extraction failure below would be the service's fault. Envelope was: {envelope}");

        var result = client.GetXmlNodeResponseBody(envelope);

        Assert.IsFalse(
            result.IsSuccess,
            $"Extraction unexpectedly succeeded, which means SOAP12-BODY-PREFIX is fixed; unpin the companion test. Envelope was: {envelope}");

        Soap12FunctionalSupport.AssertFailureMessageContains(result, NoSingleBodyMessage);
    }

    [TestMethod]
    [Ignore("DEFECT-SOAP12-BODY-PREFIX: unpins when fixed")]
    public async Task GetXmlNodeResponseBody_WhenTheResponseUsesTheSPrefix_ShouldStillExtractTheBodyChild()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var envelope = await CallHelloWorldAsync(client, prefix: "s");

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXmlNodeResponseBody(envelope),
            "GetXmlNodeResponseBody(s prefix)");

        Assert.AreEqual("HelloWorldResponse", node.LocalName, $"Extracted the wrong node. Envelope was: {envelope}");
        Assert.AreEqual(SoapAssert.ServiceNs, node.NamespaceURI, "The extracted node must stay in the service namespace.");

        SoapAssert.AssertElementValue(
            Soap12FunctionalSupport.ParseRoot(node.OuterXml),
            "HelloWorldResult",
            "Hello World");
    }

    [TestMethod]
    public async Task GetXNodeResponseBody_WithDefaultArguments_ExtractsTheOperationResponseElement()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var envelope = await CallHelloWorldAsync(client, prefix: null);

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXNodeResponseBody(envelope),
            "GetXNodeResponseBody(default prefix)");

        var document = node as XDocument;

        Assert.IsNotNull(document, $"Expected an XDocument but got {node.GetType().Name}.");
        Assert.IsNotNull(document!.Root, "The extracted document has no root element.");

        Assert.AreEqual(
            (Soap12FunctionalSupport.Service + "HelloWorldResponse").ToString(),
            document.Root!.Name.ToString(),
            $"Extracted the wrong element. Envelope was: {envelope}");

        SoapAssert.AssertElementValue(document.Root, "HelloWorldResult", "Hello World");
    }

    [TestMethod]
    public async Task GetXNodeResponseBody_WhenTheResponseUsesTheSPrefix_FailsAlthoughTheEnvelopeIsValid()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var envelope = await CallHelloWorldAsync(client, prefix: "s");

        SoapAssert.AssertIsSoap12Envelope(envelope);

        var result = client.GetXNodeResponseBody(envelope);

        Assert.IsFalse(
            result.IsSuccess,
            $"Extraction unexpectedly succeeded, which means SOAP12-BODY-PREFIX is fixed; unpin the companion test. Envelope was: {envelope}");

        Soap12FunctionalSupport.AssertFailureMessageContains(result, NoSingleBodyMessage);
    }

    [TestMethod]
    [Ignore("DEFECT-SOAP12-BODY-PREFIX: unpins when fixed")]
    public async Task GetXNodeResponseBody_WhenTheResponseUsesTheSPrefix_ShouldStillExtractTheBodyChild()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var envelope = await CallHelloWorldAsync(client, prefix: "s");

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXNodeResponseBody(envelope),
            "GetXNodeResponseBody(s prefix)");

        var document = node as XDocument;

        Assert.IsNotNull(document, $"Expected an XDocument but got {node.GetType().Name}.");
        Assert.AreEqual(
            (Soap12FunctionalSupport.Service + "HelloWorldResponse").ToString(),
            document!.Root!.Name.ToString(),
            $"Extracted the wrong element. Envelope was: {envelope}");
    }

    [DataTestMethod]
    [DataRow(
        SoapProtocolType.SOAP_1_2,
        SoapAssert.Soap12MediaType,
        SoapAssert.Soap12Ns,
        DisplayName = "SOAP 1.2 sends application/soap+xml and the 2003/05 envelope namespace")]
    [DataRow(
        SoapProtocolType.SOAP_1_1,
        SoapAssert.Soap11MediaType,
        SoapAssert.Soap11Ns,
        DisplayName = "SOAP 1.1 sends text/xml and the xmlsoap.org envelope namespace")]
    public void BuildRequest_MediaTypeAndEnvelopeNamespace_AreTheOnesTheProtocolMandates(
        SoapProtocolType protocol,
        string expectedMediaType,
        string expectedEnvelopeNamespace)
    {
        var client = CreateClient(protocol);

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(HttpMethod.Post, SoapServiceFixture.ServiceUri, Soap12FunctionalSupport.HelloWorldBody()),
            $"BuildRequest(POST, {protocol})");

        Assert.IsNotNull(request.Content, "The built request carries no content.");

        Assert.AreEqual(
            expectedMediaType,
            request.Content.Headers.ContentType?.MediaType,
            $"{protocol} must send its own media type. Full header was: [{request.Content.Headers.ContentType}].");

        var envelope = Soap12FunctionalSupport.ReadUnsentContent(request);

        Soap12FunctionalSupport.AssertEnvelopeNamespace(envelope, expectedEnvelopeNamespace);

        var root = Soap12FunctionalSupport.ParseRoot(envelope);
        XNamespace envelopeNamespace = expectedEnvelopeNamespace;

        var body = Soap12FunctionalSupport.RequireChild(root, envelopeNamespace + "Body");

        Soap12FunctionalSupport.RequireChild(body, Soap12FunctionalSupport.Service + "HelloWorld");
    }

    private static ISoapClientEndpoint CreateClient(SoapProtocolType protocol)
        => protocol == SoapProtocolType.SOAP_1_2
            ? SoapClientFactoryHelper.CreateSoap12Client()
            : SoapClientFactoryHelper.CreateSoap11Client();

    private static async Task<string> CallHelloWorldAsync(ISoapClientEndpoint client, string? prefix)
    {
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        var endpoint = prefix is null
            ? SoapServiceFixture.ServiceUri
            : new Uri($"{SoapServiceFixture.ServiceUri.AbsoluteUri}?prefix={prefix}");

        using var request = Soap12FunctionalSupport.BuildPost(
            client,
            Soap12FunctionalSupport.HelloWorldBody(),
            correlationId,
            endpoint);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync(HelloWorld, prefix={prefix ?? "<default>"})");

        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        return envelope;
    }
}
