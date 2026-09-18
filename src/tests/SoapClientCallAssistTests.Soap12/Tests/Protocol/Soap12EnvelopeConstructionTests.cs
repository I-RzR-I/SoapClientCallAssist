using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using System;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

[TestClass]
public sealed class Soap12EnvelopeConstructionTests
{

    [TestMethod]
    public void BuildRequest_PostWithNoArguments_ProducesASoap12Envelope_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(HttpMethod.Post, SoapServiceFixture.ServiceUri, Soap12FunctionalSupport.HelloWorldBody()),
            "BuildRequest");

        var envelope = Soap12FunctionalSupport.ReadUnsentContent(request);

        var root = SoapAssert.AssertIsSoap12Envelope(envelope);
        SoapAssert.AssertEnvelopeNamespaceIsNot11(envelope);

        var body = Soap12FunctionalSupport.RequireChild(root, Soap12FunctionalSupport.Soap12 + "Body");
        var operation = Soap12FunctionalSupport.RequireChild(body, Soap12FunctionalSupport.Service + "HelloWorld");

        Assert.IsFalse(operation.HasElements);
    }

    [TestMethod]
    public void BuildRequest_PostWithScalarArguments_PlacesThemAsChildrenOfTheOperationElement_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                SoapServiceFixture.ServiceUri,
                Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service)),
            "BuildRequest");

        var envelope = Soap12FunctionalSupport.ReadUnsentContent(request);

        var operation = Soap12FunctionalSupport.GetRequestOperationElement(envelope);

        Assert.AreEqual((Soap12FunctionalSupport.Service + "EchoValue").ToString(), operation.Name.ToString());

        Soap12FunctionalSupport.AssertChildValue(operation, Soap12FunctionalSupport.Service + "value", "abc");
        Soap12FunctionalSupport.AssertChildValue(operation, Soap12FunctionalSupport.Service + "count", "7");

        Assert.AreEqual(2, operation.Elements().Count());
    }

    [TestMethod]
    public void BuildRequest_PostWithNestedGraph_KeepsTheGraphAtDepthInTheEnvelope_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                SoapServiceFixture.ServiceUri,
                Soap12FunctionalSupport.AddRecordWithDetailBody()),
            "BuildRequest");

        var envelope = Soap12FunctionalSupport.ReadUnsentContent(request);

        var operation = Soap12FunctionalSupport.GetRequestOperationElement(envelope);
        var product = Soap12FunctionalSupport.RequireChild(operation, Soap12FunctionalSupport.Service + "product");
        var detail = Soap12FunctionalSupport.RequireChild(product, Soap12FunctionalSupport.Service + "Detail");

        Soap12FunctionalSupport.AssertChildValue(product, Soap12FunctionalSupport.Service + "Id", "77");
        Soap12FunctionalSupport.AssertChildValue(product, Soap12FunctionalSupport.Service + "Code", "P-77");
        Soap12FunctionalSupport.AssertChildValue(product, Soap12FunctionalSupport.Service + "Name", "Product 77");
        Soap12FunctionalSupport.AssertChildValue(product, Soap12FunctionalSupport.Service + "IsActive", "true");

        Soap12FunctionalSupport.AssertChildValue(detail, Soap12FunctionalSupport.Service + "PartnerId", "177");
        Soap12FunctionalSupport.AssertChildValue(detail, Soap12FunctionalSupport.Service + "ManufacturerId", "277");
        Soap12FunctionalSupport.AssertChildValue(detail, Soap12FunctionalSupport.Service + "SupplierId", "377");
    }

    [TestMethod]
    public void BuildRequest_WithAction_PutsActionOnContentHeadersAndInTheContentType_Test()
    {
        const string action = SoapAssert.ServiceNs + "EchoValue";
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                SoapServiceFixture.ServiceUri,
                Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
                action: action),
            "BuildRequest");

        Assert.IsNotNull(request.Content);

        Assert.IsFalse(request.Headers.Contains("SOAPAction"));
        Assert.IsFalse(request.Headers.Contains("Action"));

        CollectionAssert.AreEqual(new[] { action }, request.Content.Headers.GetValues("SOAPAction").ToArray());
        CollectionAssert.AreEqual(new[] { action }, request.Content.Headers.GetValues("Action").ToArray());

        SoapAssert.AssertContentTypeIsSoap12(request);

        var contentType = request.Content.Headers.ContentType!;
        Assert.AreEqual("utf-8", contentType.CharSet);

        var actionParameter = contentType.Parameters.SingleOrDefault(
            parameter => string.Equals(parameter.Name, "ActionParameter", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(actionParameter, $"{contentType}");

        Assert.IsNull(
            contentType.Parameters.SingleOrDefault(
                parameter => string.Equals(parameter.Name, "action", StringComparison.OrdinalIgnoreCase)));

        Assert.AreEqual($"\"{action}\"", actionParameter!.Value);
    }

    [TestMethod]
    public void BuildRequest_WithoutAction_OmitsEveryActionMarker_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                SoapServiceFixture.ServiceUri,
                Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
                action: null),
            "BuildRequest");

        Assert.IsNotNull(request.Content);

        Assert.IsFalse(request.Content.Headers.Contains("SOAPAction"));
        Assert.IsFalse(request.Content.Headers.Contains("Action"));

        SoapAssert.AssertContentTypeIsSoap12(request);

        Assert.IsFalse(
            request.Content.Headers.ContentType!.Parameters.Any(
                parameter => string.Equals(parameter.Name, "ActionParameter", StringComparison.OrdinalIgnoreCase)),
            $"{request.Content.Headers.ContentType}");

        var envelope = Soap12FunctionalSupport.ReadUnsentContent(request);
        var root = SoapAssert.AssertIsSoap12Envelope(envelope);

        Assert.IsNull(root.Element(Soap12FunctionalSupport.Soap12 + "Header"), envelope);
    }

    [TestMethod]
    public void BuildRequest_BothOverloads_ProduceEquivalentRequests_Test()
    {
        const string action = SoapAssert.ServiceNs + "AddRecordWithDetail";
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        using var fromArguments = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                SoapServiceFixture.ServiceUri,
                Soap12FunctionalSupport.AddRecordWithDetailBody(),
                action: action),
            "BuildRequest");

        using var fromDto = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                new BuildSoapRequestDto(
                    new HttpClientDto(SoapServiceFixture.ServiceUri),
                    new SoapEnvelopeDto(Soap12FunctionalSupport.AddRecordWithDetailBody(), action: action))),
            "BuildRequest");

        var envelopeFromArguments = Soap12FunctionalSupport.ReadUnsentContent(fromArguments);
        var envelopeFromDto = Soap12FunctionalSupport.ReadUnsentContent(fromDto);

        SoapAssert.AssertIsSoap12Envelope(envelopeFromArguments);
        SoapAssert.AssertIsSoap12Envelope(envelopeFromDto);

        Assert.IsTrue(
            XNode.DeepEquals(
                Soap12FunctionalSupport.ParseRoot(envelopeFromArguments),
                Soap12FunctionalSupport.ParseRoot(envelopeFromDto)),
            $"{envelopeFromArguments} | {envelopeFromDto}");

        Assert.AreEqual(fromArguments.Content!.Headers.ContentType!.ToString(), fromDto.Content!.Headers.ContentType!.ToString());

        CollectionAssert.AreEqual(
            fromArguments.Content.Headers.GetValues("SOAPAction").ToArray(),
            fromDto.Content.Headers.GetValues("SOAPAction").ToArray());

        Assert.AreEqual(fromArguments.RequestUri, fromDto.RequestUri);
    }

    [TestMethod]
    [Ignore("DEFECT-SOAP12-ACTION-PARAM-NAME: unpins when fixed")]
    public void BuildRequest_WithAction_ShouldNameTheContentTypeParameterAction_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        const string action = "http://SoapClientCallAssist.local/EchoValue";

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                SoapServiceFixture.ServiceUri,
                Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
                action: action),
            "BuildRequest");

        var contentType = request.Content!.Headers.ContentType!;

        var conformant = contentType.Parameters.SingleOrDefault(
            parameter => string.Equals(parameter.Name, "action", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(conformant, $"{contentType}");
    }

    [TestMethod]
    public void BuildRequest_WithUnqualifiedDirectChildThatHasChildren_FlattensItToConcatenatedText_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var ns = Soap12FunctionalSupport.Service;

        var body = new XElement(
            ns + "AddRecordWithDetail",
            new XElement(
                "product",
                new XElement("Id", "1"),
                new XElement("Detail", new XElement("PartnerId", "9"))));

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(HttpMethod.Post, SoapServiceFixture.ServiceUri, new[] { body }),
            "BuildRequest");

        var operation = Soap12FunctionalSupport.GetRequestOperationElement(
            Soap12FunctionalSupport.ReadUnsentContent(request));

        var product = operation.Elements().Single();

        Assert.AreEqual((ns + "product").ToString(), product.Name.ToString());

        Assert.AreEqual(0, product.Elements().Count());

        Assert.AreEqual("19", product.Value);
    }

    [TestMethod]
    [Ignore("DEFECT-BUILD-NEW-BODY-FLATTENS: unpins when fixed")]
    public void BuildRequest_WithUnqualifiedDirectChildThatHasChildren_ShouldPreserveTheNestedStructure_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var ns = Soap12FunctionalSupport.Service;

        var body = new XElement(
            ns + "AddRecordWithDetail",
            new XElement(
                "product",
                new XElement("Id", "1"),
                new XElement("Detail", new XElement("PartnerId", "9"))));

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(HttpMethod.Post, SoapServiceFixture.ServiceUri, new[] { body }),
            "BuildRequest");

        var operation = Soap12FunctionalSupport.GetRequestOperationElement(
            Soap12FunctionalSupport.ReadUnsentContent(request));

        var product = operation.Elements().Single();

        Assert.AreEqual(2, product.Elements().Count());
        Assert.AreEqual("9", product.Elements().Single(e => e.Name.LocalName == "Detail").Elements().Single().Value);
    }
}
