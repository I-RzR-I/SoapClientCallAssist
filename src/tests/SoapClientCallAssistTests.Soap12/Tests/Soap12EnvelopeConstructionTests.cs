using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssistTests.Soap12.Helpers;
using System;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class Soap12EnvelopeConstructionTests
{

    [TestMethod]
    public void BuildRequest_PostWithNoArguments_ProducesASoap12Envelope_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(HttpMethod.Post, SoapServiceFixture.ServiceUri, Soap12FunctionalSupport.HelloWorldBody()),
            "BuildRequest(POST, HelloWorld)");

        var envelope = Soap12FunctionalSupport.ReadUnsentContent(request);

        var root = SoapAssert.AssertIsSoap12Envelope(envelope);
        SoapAssert.AssertEnvelopeNamespaceIsNot11(envelope);

        var body = Soap12FunctionalSupport.RequireChild(root, Soap12FunctionalSupport.Soap12 + "Body");
        var operation = Soap12FunctionalSupport.RequireChild(body, Soap12FunctionalSupport.Service + "HelloWorld");

        Assert.IsFalse(operation.HasElements, "HelloWorld takes no arguments, so the operation element must be empty.");
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
            "BuildRequest(POST, EchoValue)");

        var envelope = Soap12FunctionalSupport.ReadUnsentContent(request);

        var operation = Soap12FunctionalSupport.GetRequestOperationElement(envelope);

        Assert.AreEqual(
            (Soap12FunctionalSupport.Service + "EchoValue").ToString(),
            operation.Name.ToString(),
            "The operation element must keep the qualified name it was given.");

        Soap12FunctionalSupport.AssertChildValue(operation, Soap12FunctionalSupport.Service + "value", "abc");
        Soap12FunctionalSupport.AssertChildValue(operation, Soap12FunctionalSupport.Service + "count", "7");

        Assert.AreEqual(2, operation.Elements().Count(), "Exactly the two supplied arguments must be present.");
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
            "BuildRequest(POST, AddRecordWithDetail)");

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
            "BuildRequest(POST, EchoValue, action)");

        Assert.IsNotNull(request.Content, "The built request carries no content.");

        Assert.IsFalse(
            request.Headers.Contains("SOAPAction"),
            "SOAPAction is set on the content headers, not the request headers; that placement is being pinned here.");
        Assert.IsFalse(
            request.Headers.Contains("Action"),
            "Action is set on the content headers, not the request headers; that placement is being pinned here.");

        CollectionAssert.AreEqual(
            new[] { action },
            request.Content.Headers.GetValues("SOAPAction").ToArray(),
            "The SOAPAction content header must carry the action verbatim.");
        CollectionAssert.AreEqual(
            new[] { action },
            request.Content.Headers.GetValues("Action").ToArray(),
            "The Action content header must carry the action verbatim.");

        SoapAssert.AssertContentTypeIsSoap12(request);

        var contentType = request.Content.Headers.ContentType!;
        Assert.AreEqual("utf-8", contentType.CharSet, "The default body encoding must be reported as utf-8.");

        var actionParameter = contentType.Parameters.SingleOrDefault(
            parameter => string.Equals(parameter.Name, "ActionParameter", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(
            actionParameter,
            $"The library is expected to emit its non-standard 'ActionParameter'. Full header was: [{contentType}].");

        Assert.IsNull(
            contentType.Parameters.SingleOrDefault(
                parameter => string.Equals(parameter.Name, "action", StringComparison.OrdinalIgnoreCase)),
            "The spec-conformant 'action' parameter is expected to be ABSENT today. If it has appeared, the defect "
            + "was fixed and this pin plus its companion must be flipped.");

        Assert.AreEqual($"\"{action}\"", actionParameter!.Value, "The ActionParameter value must keep its quotes.");
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
            "BuildRequest(POST, EchoValue, no action)");

        Assert.IsNotNull(request.Content, "The built request carries no content.");

        Assert.IsFalse(request.Content.Headers.Contains("SOAPAction"), "No action was supplied, so SOAPAction must be absent.");
        Assert.IsFalse(request.Content.Headers.Contains("Action"), "No action was supplied, so Action must be absent.");

        SoapAssert.AssertContentTypeIsSoap12(request);

        Assert.IsFalse(
            request.Content.Headers.ContentType!.Parameters.Any(
                parameter => string.Equals(parameter.Name, "ActionParameter", StringComparison.OrdinalIgnoreCase)),
            $"No action was supplied, so the content type must carry no ActionParameter. " +
            $"Full header was: [{request.Content.Headers.ContentType}].");

        var envelope = Soap12FunctionalSupport.ReadUnsentContent(request);
        var root = SoapAssert.AssertIsSoap12Envelope(envelope);

        Assert.IsNull(
            root.Element(Soap12FunctionalSupport.Soap12 + "Header"),
            $"No action and no headers were supplied, so the envelope must have no Header element. Envelope was: {envelope}");
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
            "BuildRequest(POST, nine argument overload)");

        using var fromDto = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                new BuildSoapRequestDto(
                    new HttpClientDto(SoapServiceFixture.ServiceUri),
                    new SoapEnvelopeDto(Soap12FunctionalSupport.AddRecordWithDetailBody(), action: action))),
            "BuildRequest(POST, BuildSoapRequestDto overload)");

        var envelopeFromArguments = Soap12FunctionalSupport.ReadUnsentContent(fromArguments);
        var envelopeFromDto = Soap12FunctionalSupport.ReadUnsentContent(fromDto);

        SoapAssert.AssertIsSoap12Envelope(envelopeFromArguments);
        SoapAssert.AssertIsSoap12Envelope(envelopeFromDto);

        Assert.IsTrue(
            XNode.DeepEquals(
                Soap12FunctionalSupport.ParseRoot(envelopeFromArguments),
                Soap12FunctionalSupport.ParseRoot(envelopeFromDto)),
            $"The two overloads produced different envelopes.{Environment.NewLine}" +
            $"Nine argument overload: {envelopeFromArguments}{Environment.NewLine}" +
            $"DTO overload:           {envelopeFromDto}");

        Assert.AreEqual(
            fromArguments.Content!.Headers.ContentType!.ToString(),
            fromDto.Content!.Headers.ContentType!.ToString(),
            "The two overloads produced different content types.");

        CollectionAssert.AreEqual(
            fromArguments.Content.Headers.GetValues("SOAPAction").ToArray(),
            fromDto.Content.Headers.GetValues("SOAPAction").ToArray(),
            "The two overloads produced different SOAPAction headers.");

        Assert.AreEqual(
            fromArguments.RequestUri,
            fromDto.RequestUri,
            "The two overloads addressed different URIs.");
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
            "BuildRequest(POST, EchoValue, action)");

        var contentType = request.Content!.Headers.ContentType!;

        var conformant = contentType.Parameters.SingleOrDefault(
            parameter => string.Equals(parameter.Name, "action", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(
            conformant,
            "SOAP 1.2 requires the action to travel as a content-type parameter named 'action'. Header was: "
            + $"[{contentType}].");
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
            "BuildRequest(POST, unqualified nested child)");

        var operation = Soap12FunctionalSupport.GetRequestOperationElement(
            Soap12FunctionalSupport.ReadUnsentContent(request));

        var product = operation.Elements().Single();

        Assert.AreEqual(
            (ns + "product").ToString(),
            product.Name.ToString(),
            "The unqualified direct child is expected to be promoted into the parent namespace.");

        Assert.AreEqual(
            0,
            product.Elements().Count(),
            "Expected the nested structure to have been destroyed by the rebuild.");

        Assert.AreEqual(
            "19",
            product.Value,
            "Expected Id='1' and PartnerId='9' to survive only as concatenated text.");
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
            "BuildRequest(POST, unqualified nested child)");

        var operation = Soap12FunctionalSupport.GetRequestOperationElement(
            Soap12FunctionalSupport.ReadUnsentContent(request));

        var product = operation.Elements().Single();

        Assert.AreEqual(2, product.Elements().Count(), "Id and Detail must both survive the rebuild.");
        Assert.AreEqual(
            "9",
            product.Elements().Single(e => e.Name.LocalName == "Detail").Elements().Single().Value,
            "Detail/PartnerId must survive at depth.");
    }
}
