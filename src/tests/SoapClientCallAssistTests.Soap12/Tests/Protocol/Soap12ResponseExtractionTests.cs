using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

[TestClass]
public sealed class Soap12ResponseExtractionTests
{

    [TestMethod]
    public async Task GetXmlNodeResponseBody_WithDefaultArguments_ExtractsTheOperationResponseElement_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var envelope = await CallHelloWorldAsync(client, prefix: null);

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXmlNodeResponseBody(envelope),
            "GetXmlNodeResponseBody");

        Assert.AreEqual("HelloWorldResponse", node.LocalName, $"{envelope}");
        Assert.AreEqual(SoapAssert.ServiceNs, node.NamespaceURI);

        SoapAssert.AssertElementValue(
            Soap12FunctionalSupport.ParseRoot(node.OuterXml),
            "HelloWorldResult",
            "Hello World");
    }

    [TestMethod]
    public async Task GetXmlNodeResponseBody_WhenTheResponseUsesTheSPrefix_ShouldStillExtractTheBodyChild_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var envelope = await CallHelloWorldAsync(client, prefix: "s");

        var root = SoapAssert.AssertIsSoap12Envelope(envelope);

        Assert.AreEqual("s", root.GetPrefixOfNamespace(Soap12FunctionalSupport.Soap12), $"{envelope}");

        Assert.IsNotNull(root.Element(Soap12FunctionalSupport.Soap12 + "Body"), $"{envelope}");

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXmlNodeResponseBody(envelope),
            "GetXmlNodeResponseBody");

        Assert.AreEqual("HelloWorldResponse", node.LocalName, $"{envelope}");
        Assert.AreEqual(SoapAssert.ServiceNs, node.NamespaceURI);

        SoapAssert.AssertElementValue(
            Soap12FunctionalSupport.ParseRoot(node.OuterXml),
            "HelloWorldResult",
            "Hello World");
    }

    [TestMethod]
    public async Task GetXNodeResponseBody_WithDefaultArguments_ExtractsTheOperationResponseElement_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var envelope = await CallHelloWorldAsync(client, prefix: null);

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXNodeResponseBody(envelope),
            "GetXNodeResponseBody");

        var document = node as XDocument;

        Assert.IsNotNull(document, $"{node.GetType().Name}");
        Assert.IsNotNull(document!.Root);

        Assert.AreEqual((Soap12FunctionalSupport.Service + "HelloWorldResponse").ToString(), document.Root!.Name.ToString(), $"{envelope}");

        SoapAssert.AssertElementValue(document.Root, "HelloWorldResult", "Hello World");
    }

    [TestMethod]
    public async Task GetXNodeResponseBody_WhenTheResponseUsesTheSPrefix_ShouldStillExtractTheBodyChild_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var envelope = await CallHelloWorldAsync(client, prefix: "s");

        SoapAssert.AssertIsSoap12Envelope(envelope);

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXNodeResponseBody(envelope),
            "GetXNodeResponseBody");

        var document = node as XDocument;

        Assert.IsNotNull(document, $"{node.GetType().Name}");
        Assert.AreEqual((Soap12FunctionalSupport.Service + "HelloWorldResponse").ToString(), document!.Root!.Name.ToString(), $"{envelope}");
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
    public void BuildRequest_MediaTypeAndEnvelopeNamespace_AreTheOnesTheProtocolMandates_Test(
        SoapProtocolType protocol,
        string expectedMediaType,
        string expectedEnvelopeNamespace)
    {
        var client = CrossProtocolSupport.CreateClient(protocol);

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(HttpMethod.Post, SoapServiceFixture.ServiceUri, Soap12FunctionalSupport.HelloWorldBody()),
            $"BuildRequest {protocol}");

        Assert.IsNotNull(request.Content);

        Assert.AreEqual(expectedMediaType, request.Content.Headers.ContentType?.MediaType, $"{protocol} | {request.Content.Headers.ContentType}");

        var envelope = Soap12FunctionalSupport.ReadUnsentContent(request);

        Soap12FunctionalSupport.AssertEnvelopeNamespace(envelope, expectedEnvelopeNamespace);

        var root = Soap12FunctionalSupport.ParseRoot(envelope);
        XNamespace envelopeNamespace = expectedEnvelopeNamespace;

        var body = Soap12FunctionalSupport.RequireChild(root, envelopeNamespace + "Body");

        Soap12FunctionalSupport.RequireChild(body, Soap12FunctionalSupport.Service + "HelloWorld");
    }

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
            $"SendRequestAsync {prefix ?? "<default>"}");

        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

        return envelope;
    }
}
