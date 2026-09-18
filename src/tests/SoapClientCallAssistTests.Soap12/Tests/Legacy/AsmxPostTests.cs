using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System.Net;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests.Legacy;

[TestClass]
public sealed class AsmxPostTests
{

    private const SoapProtocolType Protocol = SoapProtocolType.SOAP_1_1;

    [TestMethod]
    public void AsmxPost_HelloWorld_Sync_ReturnsGreetingInSoap11Envelope_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        var envelope = LegacyAssert.SendPostAndReadEnvelope(
            Protocol,
            client,
            Soap12FunctionalSupport.HelloWorldBody()[0],
            nameof(AsmxPost_HelloWorld_Sync_ReturnsGreetingInSoap11Envelope_Test));

        var payload = CrossProtocolSupport.GetBodyChild(Protocol, envelope);

        Assert.AreEqual((LegacyBodyBuilders.Service + "HelloWorldResponse").ToString(), payload.Name.ToString(), $"{envelope}");
        SoapAssert.AssertElementValue(payload, "HelloWorldResult", "Hello World");
    }

    [TestMethod]
    public async Task AsmxPost_HelloWorld_Async_ReturnsGreetingInSoap11Envelope_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(Protocol, client, Soap12FunctionalSupport.HelloWorldBody(), correlationId);
        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync {Protocol}");
        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(Protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

        var payload = CrossProtocolSupport.GetBodyChild(Protocol, envelope);

        Assert.AreEqual((LegacyBodyBuilders.Service + "HelloWorldResponse").ToString(), payload.Name.ToString(), $"{envelope}");
        SoapAssert.AssertElementValue(payload, "HelloWorldResult", "Hello World");
    }

    [TestMethod]
    public void AsmxPost_IsValid_NameInBodies_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        LegacyAssert.AssertIsValidReturnsOne(
            Protocol,
            client,
            LegacyBodyBuilders.IsValidQualifiedViaConstructor(),
            nameof(AsmxPost_IsValid_NameInBodies_ReturnsOne_Test));
    }

    [TestMethod]
    public void AsmxPost_IsValid_NameInBodiesUsingBaseUriField_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        LegacyAssert.AssertIsValidReturnsOne(
            Protocol,
            client,
            LegacyBodyBuilders.IsValidQualifiedViaConstructor(),
            nameof(AsmxPost_IsValid_NameInBodiesUsingBaseUriField_ReturnsOne_Test));
    }

    [TestMethod]
    public void AsmxPost_IsValid_ParamFromNs_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        LegacyAssert.AssertIsValidReturnsOne(
            Protocol,
            client,
            LegacyBodyBuilders.IsValidQualifiedViaConstructor(),
            nameof(AsmxPost_IsValid_ParamFromNs_ReturnsOne_Test));
    }

    [TestMethod]
    public void AsmxPost_IsValid_ParamFromNsViaAddCalls_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        LegacyAssert.AssertIsValidReturnsOne(
            Protocol,
            client,
            LegacyBodyBuilders.IsValidQualifiedViaAddCalls(),
            nameof(AsmxPost_IsValid_ParamFromNsViaAddCalls_ReturnsOne_Test));
    }

    [TestMethod]
    public void AsmxPost_IsValid_ParamFromNsFromSharedParamsList_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        LegacyAssert.AssertIsValidReturnsOne(
            Protocol,
            client,
            LegacyBodyBuilders.IsValidQualifiedFromSharedParamsList(),
            nameof(AsmxPost_IsValid_ParamFromNsFromSharedParamsList_ReturnsOne_Test));
    }

    [TestMethod]
    public void AsmxPost_IsValid_WithBodyResult_ValidatesFaultCheckAndXmlNodeExtraction_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        var envelope = LegacyAssert.SendPostAndReadEnvelope(
            Protocol,
            client,
            LegacyBodyBuilders.IsValidQualifiedViaConstructor(),
            nameof(AsmxPost_IsValid_WithBodyResult_ValidatesFaultCheckAndXmlNodeExtraction_Test));

        var payload = CrossProtocolSupport.GetBodyChild(Protocol, envelope);
        SoapAssert.AssertElementValue(payload, "IsValidResult", "1");

        var faultCheck = client.CheckBodyForFaultCode(envelope);
        Assert.IsTrue(faultCheck.IsSuccess, NegativeTestSupport.Describe(faultCheck));

        var xmlNode = client.GetXmlNodeResponseBody(envelope);
        Assert.IsTrue(xmlNode.IsSuccess, NegativeTestSupport.Describe(xmlNode));
        Assert.IsNotNull(xmlNode.Response);
        Assert.AreEqual("IsValidResponse", xmlNode.Response!.LocalName, $"{envelope}");
    }

    [TestMethod]
    public void AsmxPost_AddRecordWithDetail_WithNoData_FaultsWithSenderReason_Test()
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
    public void AsmxPost_AddRecordWithDetail_Success_EchoesReceivedProduct_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        var envelope = LegacyAssert.SendPostAndReadEnvelope(
            Protocol,
            client,
            LegacyBodyBuilders.AddRecordWithDetailSingleNamespace(),
            nameof(AsmxPost_AddRecordWithDetail_Success_EchoesReceivedProduct_Test));

        var payload = CrossProtocolSupport.GetBodyChild(Protocol, envelope);
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
    }
}
