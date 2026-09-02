using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace SoapClientCallAssistTests.Soap12.Tests.Legacy;

[TestClass]
public sealed class SvcPostTests
{

    private const SoapProtocolType Protocol = SoapProtocolType.SOAP_1_2;

    private static Uri SPrefixedEndpoint => new($"{SoapServiceFixture.ServiceUri}?prefix=s");

    [TestMethod]
    public void SvcPost_HelloWorld_ReturnsGreetingInSoap12Envelope_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        var envelope = LegacyAssert.SendPostAndReadEnvelope(
            Protocol,
            client,
            Soap12FunctionalSupport.HelloWorldBody()[0],
            nameof(SvcPost_HelloWorld_ReturnsGreetingInSoap12Envelope_Test));

        var payload = CrossProtocolSupport.GetBodyChild(Protocol, envelope);

        Assert.AreEqual(
            (LegacyBodyBuilders.Service + "HelloWorldResponse").ToString(),
            payload.Name.ToString(),
            $"Expected the conventional HelloWorldResponse element. Envelope was: {envelope}");
        SoapAssert.AssertElementValue(payload, "HelloWorldResult", "Hello World");
    }

    [TestMethod]
    public void SvcPost_HelloWorld_WithCustomHttpHeader_ReachesServiceOnTheWire_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        var headers = Soap12FunctionalSupport.CorrelationHeaders(correlationId);
        headers.Add("UserAgent", new[] { "test" });

        var built = client.BuildRequest(
            HttpMethod.Post,
            SoapServiceFixture.ServiceUri,
            Soap12FunctionalSupport.HelloWorldBody(),
            httpClientHeaders: headers);

        Assert.IsTrue(built.IsSuccess, $"BuildRequest with a custom HTTP header must succeed. Got: {NegativeTestSupport.Describe(built)}");
        Assert.IsNotNull(built.Response, "A successful build must carry a request.");

        using var request = built.Response;
        using var response = Soap12FunctionalSupport.Unwrap(client.SendRequest(request), "SendRequest(HelloWorld, custom header)");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        var recorded = Soap12FunctionalSupport.FindRecorded(correlationId);
        Assert.IsTrue(
            recorded.Headers.TryGetValue("UserAgent", out var values) && values.Contains("test"),
            "The custom 'UserAgent' header supplied via httpClientHeaders must reach the service unchanged.");
    }

    [TestMethod]
    public void SvcPost_IsValid_UnqualifiedIdInheritsParentNamespace_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        LegacyAssert.AssertIsValidReturnsOne(
            Protocol,
            client,
            LegacyBodyBuilders.IsValidUnqualifiedIdQualifiedIdV2(),
            nameof(SvcPost_IsValid_UnqualifiedIdInheritsParentNamespace_ReturnsOne_Test));
    }

    [TestMethod]
    public void SvcPost_IsValid_BothChildrenInheritParentNamespace_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        LegacyAssert.AssertIsValidReturnsOne(
            Protocol,
            client,
            LegacyBodyBuilders.IsValidBothUnqualified(),
            nameof(SvcPost_IsValid_BothChildrenInheritParentNamespace_ReturnsOne_Test));
    }

    [TestMethod]
    public void SvcPost_IsValid_ParamFromNs_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        LegacyAssert.AssertIsValidReturnsOne(
            Protocol,
            client,
            LegacyBodyBuilders.IsValidQualifiedViaConstructor(),
            nameof(SvcPost_IsValid_ParamFromNs_ReturnsOne_Test));
    }

    [TestMethod]
    public void SvcPost_IsValid_ParamFromNsViaAddCalls_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        LegacyAssert.AssertIsValidReturnsOne(
            Protocol,
            client,
            LegacyBodyBuilders.IsValidQualifiedViaAddCalls(),
            nameof(SvcPost_IsValid_ParamFromNsViaAddCalls_ReturnsOne_Test));
    }

    [TestMethod]
    public void SvcPost_IsValid_ParamFromNsFromSharedParamsList_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        LegacyAssert.AssertIsValidReturnsOne(
            Protocol,
            client,
            LegacyBodyBuilders.IsValidQualifiedFromSharedParamsList(),
            nameof(SvcPost_IsValid_ParamFromNsFromSharedParamsList_ReturnsOne_Test));
    }

    [TestMethod]
    public void SvcPost_IsValid_WithBodyResult_ValidatesFaultCheckAndSPrefixedBodyExtraction_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            Protocol,
            client,
            new[] { LegacyBodyBuilders.IsValidQualifiedViaConstructor() },
            correlationId,
            endpoint: SPrefixedEndpoint);

        using var response = Soap12FunctionalSupport.Unwrap(client.SendRequest(request), "SendRequest(IsValid, s-prefixed envelope)");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");
        StringAssert.Contains(envelope, "<s:Body>", $"Requesting ?prefix=s must render the WCF-style s: prefix. Envelope was: {envelope}");

        var payload = SoapAssert.GetBodyChild(envelope, SoapAssert.Soap12Ns);
        SoapAssert.AssertElementValue(payload, "IsValidResult", "1");

        var faultCheck = client.CheckBodyForFaultCode(envelope);
        Assert.IsTrue(
            faultCheck.IsSuccess,
            $"A fault-free IsValid response must report success. Got: {NegativeTestSupport.Describe(faultCheck)}");

        var xmlNode = client.GetXmlNodeResponseBody(envelope, soapXmlBodyTag: "s:Body");
        Assert.IsTrue(
            xmlNode.IsSuccess,
            $"An explicit s:Body tag must extract the payload from an s-prefixed envelope. Got: {NegativeTestSupport.Describe(xmlNode)}");
        Assert.IsNotNull(xmlNode.Response, "A successful extraction must carry a node.");
        Assert.AreEqual(
            "IsValidResponse",
            xmlNode.Response!.LocalName,
            $"Expected the IsValidResponse element. Envelope was: {envelope}");
    }

    [TestMethod]
    public void SvcPost_AddRecordWithDetail_WithNoData_FaultsWithSenderReason_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            Protocol,
            client,
            new[] { LegacyBodyBuilders.AddRecordWithDetailEmpty() },
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(
            client.SendRequest(request),
            $"SendRequest(AddRecordWithDetail, missing product, {Protocol})");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        Assert.AreEqual(
            HttpStatusCode.InternalServerError,
            response.StatusCode,
            $"A missing required argument must fault with HTTP 500. Envelope was: {envelope}");

        var faultCheck = client.CheckBodyForFaultCode(envelope);
        Assert.IsFalse(faultCheck.IsSuccess, $"A fault response must be classified as a failure. Envelope was: {envelope}");
        StringAssert.Contains(
            NegativeTestSupport.FirstMessageInfo(faultCheck),
            "Argument 'product' is required.",
            $"The fault reason must reach the caller. Envelope was: {envelope}");
    }

    [TestMethod]
    public void SvcPost_AddRecordWithDetail_Success_FlatOverload_EchoesReceivedProduct_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        var built = client.BuildRequest(
            HttpMethod.Post,
            SPrefixedEndpoint,
            new[] { LegacyBodyBuilders.AddRecordWithDetailDualNamespace() },
            ownSoapEnvelopeAttributes: LegacyBodyBuilders.DualNamespaceEnvelopeAttributes(),
            httpClientHeaders: Soap12FunctionalSupport.CorrelationHeaders(correlationId));

        Assert.IsTrue(built.IsSuccess, $"BuildRequest(flat overload) must succeed. Got: {NegativeTestSupport.Describe(built)}");
        Assert.IsNotNull(built.Response, "A successful build must carry a request.");

        using var request = built.Response;
        using var response = Soap12FunctionalSupport.Unwrap(client.SendRequest(request), "SendRequest(AddRecordWithDetail, flat overload)");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(Protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        var payload = SoapAssert.GetBodyChild(envelope, SoapAssert.Soap12Ns);
        Soap12FunctionalSupport.AssertChildValue(payload, LegacyBodyBuilders.Service + "AddRecordWithDetailResult", "true");

        var received = Soap12FunctionalSupport.RequireChild(payload, LegacyBodyBuilders.Service + "ReceivedProduct");
        Soap12FunctionalSupport.AssertChildValue(received, LegacyBodyBuilders.Service + "Id", "1");
        Soap12FunctionalSupport.AssertChildValue(received, LegacyBodyBuilders.Service + "Code", "Code-001");
        Soap12FunctionalSupport.AssertChildValue(received, LegacyBodyBuilders.Service + "Name", "Name-001");
        Soap12FunctionalSupport.AssertChildValue(received, LegacyBodyBuilders.Service + "IsActive", "true");

        var detail = Soap12FunctionalSupport.RequireChild(received, LegacyBodyBuilders.Service + "Detail");
        Soap12FunctionalSupport.AssertChildValue(detail, LegacyBodyBuilders.Service + "ManufacturerId", "1");
        Soap12FunctionalSupport.AssertChildValue(detail, LegacyBodyBuilders.Service + "SupplierId", "2");
        Soap12FunctionalSupport.AssertChildValue(detail, LegacyBodyBuilders.Service + "PartnerId", "3");

        var xmlNodeResponse = client.GetXmlNodeResponseBody(envelope, soapXmlBodyTag: "s:Body");
        Assert.IsTrue(
            xmlNodeResponse.IsSuccess,
            $"An explicit s:Body tag must extract the payload from an s-prefixed envelope. Got: {NegativeTestSupport.Describe(xmlNodeResponse)}");
        Assert.IsNotNull(xmlNodeResponse.Response, "A successful extraction must carry a node.");

        var xNodeResponse = client.GetXNodeResponseBody(envelope, soapXmlBodyTag: "s:Body");
        Assert.IsTrue(
            xNodeResponse.IsSuccess,
            $"An explicit s:Body tag must also work for GetXNodeResponseBody. Got: {NegativeTestSupport.Describe(xNodeResponse)}");
        Assert.IsNotNull(xNodeResponse.Response, "A successful extraction must carry a node.");
    }

    [TestMethod]
    public void SvcPost_AddRecordWithDetail_Success_DtoOverload_EchoesReceivedProduct_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        var built = client.BuildRequest(
            HttpMethod.Post,
            new BuildSoapRequestDto(
                new HttpClientDto(SPrefixedEndpoint, httpClientHeaders: Soap12FunctionalSupport.CorrelationHeaders(correlationId)),
                new SoapEnvelopeDto(
                    new[] { LegacyBodyBuilders.AddRecordWithDetailDualNamespace() },
                    ownSoapEnvelopeAttributes: LegacyBodyBuilders.DualNamespaceEnvelopeAttributes())));

        Assert.IsTrue(built.IsSuccess, $"BuildRequest(DTO overload) must succeed. Got: {NegativeTestSupport.Describe(built)}");
        Assert.IsNotNull(built.Response, "A successful build must carry a request.");

        using var request = built.Response;
        using var response = Soap12FunctionalSupport.Unwrap(client.SendRequest(request), "SendRequest(AddRecordWithDetail, DTO overload)");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(Protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        var payload = SoapAssert.GetBodyChild(envelope, SoapAssert.Soap12Ns);
        Soap12FunctionalSupport.AssertChildValue(payload, LegacyBodyBuilders.Service + "AddRecordWithDetailResult", "true");

        var received = Soap12FunctionalSupport.RequireChild(payload, LegacyBodyBuilders.Service + "ReceivedProduct");
        Soap12FunctionalSupport.AssertChildValue(received, LegacyBodyBuilders.Service + "Id", "1");
        Soap12FunctionalSupport.AssertChildValue(received, LegacyBodyBuilders.Service + "Code", "Code-001");
        Soap12FunctionalSupport.AssertChildValue(received, LegacyBodyBuilders.Service + "Name", "Name-001");
        Soap12FunctionalSupport.AssertChildValue(received, LegacyBodyBuilders.Service + "IsActive", "true");

        var detail = Soap12FunctionalSupport.RequireChild(received, LegacyBodyBuilders.Service + "Detail");
        Soap12FunctionalSupport.AssertChildValue(detail, LegacyBodyBuilders.Service + "ManufacturerId", "1");
        Soap12FunctionalSupport.AssertChildValue(detail, LegacyBodyBuilders.Service + "SupplierId", "2");
        Soap12FunctionalSupport.AssertChildValue(detail, LegacyBodyBuilders.Service + "PartnerId", "3");

        var xmlNodeResponse = client.GetXmlNodeResponseBody(envelope, soapXmlBodyTag: "s:Body");
        Assert.IsTrue(
            xmlNodeResponse.IsSuccess,
            $"An explicit s:Body tag must extract the payload from an s-prefixed envelope. Got: {NegativeTestSupport.Describe(xmlNodeResponse)}");
        Assert.IsNotNull(xmlNodeResponse.Response, "A successful extraction must carry a node.");

        var xNodeResponse = client.GetXNodeResponseBody(envelope, soapXmlBodyTag: "s:Body");
        Assert.IsTrue(
            xNodeResponse.IsSuccess,
            $"An explicit s:Body tag must also work for GetXNodeResponseBody. Got: {NegativeTestSupport.Describe(xNodeResponse)}");
        Assert.IsNotNull(xNodeResponse.Response, "A successful extraction must carry a node.");
    }
}
