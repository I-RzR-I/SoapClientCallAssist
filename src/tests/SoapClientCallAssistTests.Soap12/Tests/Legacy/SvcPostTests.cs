using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
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

        Assert.AreEqual((LegacyBodyBuilders.Service + "HelloWorldResponse").ToString(), payload.Name.ToString(), $"{envelope}");
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

        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));
        Assert.IsNotNull(built.Response);

        using var request = built.Response;
        using var response = Soap12FunctionalSupport.Unwrap(client.SendRequest(request), "SendRequest");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

        var recorded = Soap12FunctionalSupport.FindRecorded(correlationId);
        Assert.IsTrue(recorded.Headers.TryGetValue("UserAgent", out var values) && values.Contains("test"));
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

        using var response = Soap12FunctionalSupport.Unwrap(client.SendRequest(request), "SendRequest");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");
        StringAssert.Contains(envelope, "<s:Body>");

        var payload = SoapAssert.GetBodyChild(envelope, SoapAssert.Soap12Ns);
        SoapAssert.AssertElementValue(payload, "IsValidResult", "1");

        var faultCheck = client.CheckBodyForFaultCode(envelope);
        Assert.IsTrue(faultCheck.IsSuccess, NegativeTestSupport.Describe(faultCheck));

        var xmlNode = client.GetXmlNodeResponseBody(envelope, soapXmlBodyTag: "s:Body");
        Assert.IsTrue(xmlNode.IsSuccess, NegativeTestSupport.Describe(xmlNode));
        Assert.IsNotNull(xmlNode.Response);
        Assert.AreEqual("IsValidResponse", xmlNode.Response!.LocalName, $"{envelope}");
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

        using var response = Soap12FunctionalSupport.UnwrapRejected(
            client.SendRequest(request),
            NegativeTestSupport.HttpSoapFaultCode,
            $"SendRequest {Protocol}");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode, $"{envelope}");

        var faultCheck = client.CheckBodyForFaultCode(envelope);
        Assert.IsFalse(faultCheck.IsSuccess, $"{envelope}");
        StringAssert.Contains(NegativeTestSupport.FirstMessageInfo(faultCheck), "Argument 'product' is required.", $"{envelope}");
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

        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));
        Assert.IsNotNull(built.Response);

        using var request = built.Response;
        using var response = Soap12FunctionalSupport.Unwrap(client.SendRequest(request), "SendRequest");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(Protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

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
        Assert.IsTrue(xmlNodeResponse.IsSuccess, NegativeTestSupport.Describe(xmlNodeResponse));
        Assert.IsNotNull(xmlNodeResponse.Response);

        var xNodeResponse = client.GetXNodeResponseBody(envelope, soapXmlBodyTag: "s:Body");
        Assert.IsTrue(xNodeResponse.IsSuccess, NegativeTestSupport.Describe(xNodeResponse));
        Assert.IsNotNull(xNodeResponse.Response);
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

        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));
        Assert.IsNotNull(built.Response);

        using var request = built.Response;
        using var response = Soap12FunctionalSupport.Unwrap(client.SendRequest(request), "SendRequest");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(Protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

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
        Assert.IsTrue(xmlNodeResponse.IsSuccess, NegativeTestSupport.Describe(xmlNodeResponse));
        Assert.IsNotNull(xmlNodeResponse.Response);

        var xNodeResponse = client.GetXNodeResponseBody(envelope, soapXmlBodyTag: "s:Body");
        Assert.IsTrue(xNodeResponse.IsSuccess, NegativeTestSupport.Describe(xNodeResponse));
        Assert.IsNotNull(xNodeResponse.Response);
    }
}
