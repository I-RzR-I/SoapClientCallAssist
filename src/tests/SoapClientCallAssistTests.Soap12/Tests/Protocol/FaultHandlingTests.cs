using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

[TestClass]
public sealed class FaultHandlingTests
{
    private static readonly XNamespace Soap12Namespace = SoapAssert.Soap12Ns;

    private static readonly XNamespace Soap11Namespace = SoapAssert.Soap11Ns;

    private const string FaultCode = "ER-BEC-FLT";

    private const string FaultCheckErrorCode = "ER-BEC-CBFFC";

    private const string NoSingleBodyCode = "ER-BEC-GRB-01";

    [TestMethod]
    public async Task CheckBodyForFaultCode_WithPopulatedFault_FailsAndSurfacesTheReason_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "ThrowFault");

        var send = await client.SendRequestAsync(request);

        using var response = Soap12FunctionalSupport.UnwrapRejected(
            send,
            NegativeTestSupport.HttpSoapFaultCode,
            "SendRequestAsync");
        var body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        var fault = SoapAssert.GetBodyChild(body);

        Assert.AreEqual("Fault", fault.Name.LocalName, $"{body}");
        Assert.AreEqual(SoapAssert.Soap12Ns, fault.Name.NamespaceName);
        SoapAssert.AssertElementValue(fault, "Value", "soap:Sender");
        SoapAssert.AssertElementValue(fault, "Text", "The operation failed on purpose.");

        var check = client.CheckBodyForFaultCode(body);

        Assert.IsFalse(check.IsSuccess, $"{body}");

        var info = NegativeTestSupport.FirstMessageInfo(check);

        StringAssert.Contains(info, "The operation failed on purpose.");

        StringAssert.Contains(info, "soap:Sender");
    }

    [TestMethod]
    [Ignore("DEFECT-FAULT-MESSAGE-CONCAT: unpins when fixed")]
    public async Task CheckBodyForFaultCode_WithPopulatedFault_ShouldSurfaceOnlyTheReason_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "ThrowFault");

        var send = await client.SendRequestAsync(request);
        using var response = send.Response;
        var body = await response.Content.ReadAsStringAsync();

        var check = client.CheckBodyForFaultCode(body);

        Assert.AreEqual("The operation failed on purpose.", NegativeTestSupport.FirstMessageInfo(check));
    }

    [TestMethod]
    public async Task CheckBodyForFaultCode_WithEmptyFaultReason_ReportsTheRawFaultCodeQNameAsTheMessage_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "ThrowEmptyFault");

        var send = await client.SendRequestAsync(request);

        using var response = Soap12FunctionalSupport.UnwrapRejected(
            send,
            NegativeTestSupport.HttpSoapFaultCode,
            "SendRequestAsync");
        var body = await response.Content.ReadAsStringAsync();

        var fault = SoapAssert.GetBodyChild(body);

        Assert.AreEqual("Fault", fault.Name.LocalName, $"{body}");
        Assert.AreEqual(SoapAssert.Soap12Ns, fault.Name.NamespaceName);
        SoapAssert.AssertElementValue(fault, "Text", string.Empty);
        SoapAssert.AssertElementValue(fault, "Value", "soap:Sender");

        var check = client.CheckBodyForFaultCode(body);

        Assert.IsFalse(check.IsSuccess, $"{body}");

        Assert.AreEqual("soap:Sender", NegativeTestSupport.FirstMessageInfo(check));
    }

    [TestMethod]
    [Ignore("DEFECT-FAULT-REASON-EMPTY: unpins when fixed")]
    public async Task CheckBodyForFaultCode_WithEmptyFaultReason_ShouldNotSurfaceTheServerPrefixAsTheMessage_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "ThrowEmptyFault");

        var send = await client.SendRequestAsync(request);
        using var response = send.Response;
        var body = await response.Content.ReadAsStringAsync();

        var check = client.CheckBodyForFaultCode(body);

        Assert.AreNotEqual("soap:Sender", NegativeTestSupport.FirstMessageInfo(check));
    }

    [TestMethod]
    public async Task CheckBodyForFaultCode_WithPopulatedFault_CarriesTheFaultCode_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "ThrowFault");

        var send = await client.SendRequestAsync(request);

        using var response = Soap12FunctionalSupport.UnwrapRejected(
            send,
            NegativeTestSupport.HttpSoapFaultCode,
            "SendRequestAsync");
        var body = await response.Content.ReadAsStringAsync();

        var check = client.CheckBodyForFaultCode(body);

        Assert.IsFalse(check.IsSuccess, $"{body}");

        var messages = NegativeTestSupport.Messages(check);

        Assert.AreEqual(1, messages.Count, NegativeTestSupport.Describe(check));

        Assert.AreEqual(FaultCode, messages[0].Key, NegativeTestSupport.Describe(check));

        Assert.AreNotEqual(FaultCheckErrorCode, messages[0].Key);
    }

    [TestMethod]
    public void CheckBodyForFaultCode_WithFaultCarryingNoText_ReportsSuccessAlthoughAFaultIsPresent_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var fault = NegativeTestSupport.FindFault(NegativeTestSupport.Soap12FaultWithoutAnyText, Soap12Namespace);

        Assert.IsNotNull(fault);
        Assert.AreEqual(string.Empty, fault!.Value);

        var check = client.CheckBodyForFaultCode(NegativeTestSupport.Soap12FaultWithoutAnyText);

        Assert.IsTrue(check.IsSuccess);
    }

    [TestMethod]
    [Ignore("DEFECT-FAULT-DETECTION-BY-TEXT: unpins when fixed")]
    public void CheckBodyForFaultCode_WithFaultCarryingNoText_ShouldReportFailure_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var check = client.CheckBodyForFaultCode(NegativeTestSupport.Soap12FaultWithoutAnyText);

        Assert.IsFalse(check.IsSuccess);
    }

    [TestMethod]
    public void CheckBodyForFaultCode_WithSoap11Fault_IsIgnoredBySoap12ButSeenBySoap11_Test()
    {

        var soap11Fault = NegativeTestSupport.FindFault(NegativeTestSupport.Soap11FaultEnvelope, Soap11Namespace);
        var soap12Fault = NegativeTestSupport.FindFault(NegativeTestSupport.Soap11FaultEnvelope, Soap12Namespace);

        Assert.IsNotNull(soap11Fault);
        Assert.IsNull(soap12Fault);
        StringAssert.Contains(soap11Fault!.Value, "The operation failed on purpose.");

        var soap12Check = SoapClientFactoryHelper.CreateSoap12Client()
            .CheckBodyForFaultCode(NegativeTestSupport.Soap11FaultEnvelope);

        var soap11Check = SoapClientFactoryHelper.CreateSoap11Client()
            .CheckBodyForFaultCode(NegativeTestSupport.Soap11FaultEnvelope);

        Assert.IsTrue(soap12Check.IsSuccess);

        Assert.IsFalse(soap11Check.IsSuccess, NegativeTestSupport.Describe(soap11Check));

        StringAssert.Contains(NegativeTestSupport.FirstMessageInfo(soap11Check), "The operation failed on purpose.");
    }

    [TestMethod]
    public void CheckBodyForFaultCode_WithAFaultPlantedInTheHeader_IgnoresItAndReportsTheBody_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var planted = NegativeTestSupport.FindFault(NegativeTestSupport.Soap12HeaderFaultEnvelope, Soap12Namespace);

        Assert.IsNotNull(planted);
        StringAssert.Contains(planted!.Value, "Account suspended");

        Assert.AreEqual("HelloWorldResponse", SoapAssert.GetBodyChild(NegativeTestSupport.Soap12HeaderFaultEnvelope).Name.LocalName);

        var check = client.CheckBodyForFaultCode(NegativeTestSupport.Soap12HeaderFaultEnvelope);

        Assert.IsTrue(check.IsSuccess, NegativeTestSupport.Describe(check));
    }

    [TestMethod]
    public void CheckBodyForFaultCode_WithFaultFreeResponse_ReportsSuccess_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.IsNull(NegativeTestSupport.FindFault(NegativeTestSupport.UnindentedSoap12Envelope, Soap12Namespace));

        Assert.AreEqual("HelloWorldResponse", SoapAssert.GetBodyChild(NegativeTestSupport.UnindentedSoap12Envelope).Name.LocalName);

        var check = client.CheckBodyForFaultCode(NegativeTestSupport.UnindentedSoap12Envelope);

        Assert.IsTrue(check.IsSuccess, NegativeTestSupport.Describe(check));
    }

    [DataTestMethod]
    [DataRow(
        "<html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>502 Bad Gateway</title></head><body><h1>502</h1></body></html>",
        DisplayName = "well-formed XHTML error page from a proxy")]
    [DataRow(
        "<Envelope xmlns=\"urn:not-soap\"><Body><HelloWorldResponse xmlns=\"http://SoapClientCallAssist.local/\" /></Body></Envelope>",
        DisplayName = "Envelope and Body in an unrelated namespace")]
    [DataRow(
        "<Wrapper><soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\"><soap:Body /></soap:Envelope></Wrapper>",
        DisplayName = "SOAP envelope nested under a non-SOAP document element")]
    public void CheckBodyForFaultCode_WithADocumentThatIsNotASoapEnvelope_ReportsFailure_Test(string document)
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var check = client.CheckBodyForFaultCode(document);

        Assert.IsFalse(check.IsSuccess, NegativeTestSupport.Describe(check));

        Assert.AreEqual(NoSingleBodyCode, NegativeTestSupport.Messages(check)[0].Key, NegativeTestSupport.Describe(check));
    }

    [DataTestMethod]
    [DataRow(
        "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\"><soap:Header /></soap:Envelope>",
        DisplayName = "SOAP 1.2 envelope with no Body")]
    [DataRow(
        "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\"><soap:Body /><soap:Body /></soap:Envelope>",
        DisplayName = "SOAP 1.2 envelope with two Bodies")]
    [DataRow(
        "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\"><soap:Header><soap:Body /></soap:Header></soap:Envelope>",
        DisplayName = "SOAP 1.2 envelope whose only Body sits inside the Header")]
    public void CheckBodyForFaultCode_WithAnEnvelopeInTheClientNamespaceResolvingNoSingleBody_ReportsFailure_Test(string document)
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var check = client.CheckBodyForFaultCode(document);

        Assert.IsFalse(check.IsSuccess, NegativeTestSupport.Describe(check));

        Assert.AreEqual(NoSingleBodyCode, NegativeTestSupport.Messages(check)[0].Key, NegativeTestSupport.Describe(check));
    }
}
