using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class FaultHandlingTests
{
    private static readonly XNamespace Soap12Namespace = SoapAssert.Soap12Ns;

    private static readonly XNamespace Soap11Namespace = SoapAssert.Soap11Ns;

    [TestMethod]
    public async Task CheckBodyForFaultCode_WithPopulatedFault_FailsAndSurfacesTheReason()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "ThrowFault");

        var send = await client.SendRequestAsync(request);

        Assert.IsTrue(send.IsSuccess, $"The fault call must complete at the transport level. Got: {NegativeTestSupport.Describe(send)}");

        using var response = send.Response;
        var body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode, "A SOAP 1.2 fault is returned with HTTP 500.");

        var fault = SoapAssert.GetBodyChild(body);

        Assert.AreEqual("Fault", fault.Name.LocalName, $"The body child must be a fault. Body was: {body}");
        Assert.AreEqual(SoapAssert.Soap12Ns, fault.Name.NamespaceName, "The fault must be in the SOAP 1.2 envelope namespace.");
        SoapAssert.AssertElementValue(fault, "Value", "soap:Sender");
        SoapAssert.AssertElementValue(fault, "Text", "The operation failed on purpose.");

        var check = client.CheckBodyForFaultCode(body);

        Assert.IsFalse(check.IsSuccess, $"A populated fault must be classified as a failure. Body was: {body}");

        var info = NegativeTestSupport.FirstMessageInfo(check);

        StringAssert.Contains(info, "The operation failed on purpose.", "The fault reason must reach the caller.");

        StringAssert.Contains(
            info,
            "soap:Sender",
            "Pinning current behaviour: the fault code QName is concatenated into the caller-facing message.");
    }

    [TestMethod]
    [Ignore("DEFECT-FAULT-MESSAGE-CONCAT: unpins when fixed")]
    public async Task CheckBodyForFaultCode_WithPopulatedFault_ShouldSurfaceOnlyTheReason()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "ThrowFault");

        var send = await client.SendRequestAsync(request);
        using var response = send.Response;
        var body = await response.Content.ReadAsStringAsync();

        var check = client.CheckBodyForFaultCode(body);

        Assert.AreEqual(
            "The operation failed on purpose.",
            NegativeTestSupport.FirstMessageInfo(check),
            "The caller-facing message must be the fault reason, not the reason with the code QName prepended.");
    }

    [TestMethod]
    public async Task CheckBodyForFaultCode_WithEmptyFaultReason_ReportsTheRawFaultCodeQNameAsTheMessage()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "ThrowEmptyFault");

        var send = await client.SendRequestAsync(request);

        Assert.IsTrue(send.IsSuccess, $"The fault call must complete at the transport level. Got: {NegativeTestSupport.Describe(send)}");

        using var response = send.Response;
        var body = await response.Content.ReadAsStringAsync();

        var fault = SoapAssert.GetBodyChild(body);

        Assert.AreEqual("Fault", fault.Name.LocalName, $"The body child must be a fault. Body was: {body}");
        Assert.AreEqual(SoapAssert.Soap12Ns, fault.Name.NamespaceName, "The fault must be in the SOAP 1.2 envelope namespace.");
        SoapAssert.AssertElementValue(fault, "Text", string.Empty);
        SoapAssert.AssertElementValue(fault, "Value", "soap:Sender");

        var check = client.CheckBodyForFaultCode(body);

        Assert.IsFalse(check.IsSuccess, $"A fault must be classified as a failure even with an empty reason. Body was: {body}");

        Assert.AreEqual(
            "soap:Sender",
            NegativeTestSupport.FirstMessageInfo(check),
            "Pinning current behaviour: the caller-facing message is the server's prefixed fault code QName.");
    }

    [TestMethod]
    [Ignore("DEFECT-FAULT-REASON-EMPTY: unpins when fixed")]
    public async Task CheckBodyForFaultCode_WithEmptyFaultReason_ShouldNotSurfaceTheServerPrefixAsTheMessage()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "ThrowEmptyFault");

        var send = await client.SendRequestAsync(request);
        using var response = send.Response;
        var body = await response.Content.ReadAsStringAsync();

        var check = client.CheckBodyForFaultCode(body);

        Assert.AreNotEqual(
            "soap:Sender",
            NegativeTestSupport.FirstMessageInfo(check),
            "The caller-facing message must not be the server's namespace prefix concatenated with the fault code.");
    }

    [TestMethod]
    public void CheckBodyForFaultCode_WithFaultCarryingNoText_ReportsSuccessAlthoughAFaultIsPresent()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var fault = NegativeTestSupport.FindFault(NegativeTestSupport.Soap12FaultWithoutAnyText, Soap12Namespace);

        Assert.IsNotNull(fault, "The fixture must contain a SOAP 1.2 fault, otherwise this test proves nothing.");
        Assert.AreEqual(string.Empty, fault!.Value, "The fixture's fault must carry no text at all.");

        var check = client.CheckBodyForFaultCode(NegativeTestSupport.Soap12FaultWithoutAnyText);

        Assert.IsTrue(
            check.IsSuccess,
            "Pinning current behaviour: a textless fault is classified as a success, so the check fails open.");
    }

    [TestMethod]
    [Ignore("DEFECT-FAULT-DETECTION-BY-TEXT: unpins when fixed")]
    public void CheckBodyForFaultCode_WithFaultCarryingNoText_ShouldReportFailure()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var check = client.CheckBodyForFaultCode(NegativeTestSupport.Soap12FaultWithoutAnyText);

        Assert.IsFalse(
            check.IsSuccess,
            "A response carrying a Fault element is a failure whether or not the server filled it in.");
    }

    [TestMethod]
    public void CheckBodyForFaultCode_WithSoap11Fault_IsIgnoredBySoap12ButSeenBySoap11()
    {

        var soap11Fault = NegativeTestSupport.FindFault(NegativeTestSupport.Soap11FaultEnvelope, Soap11Namespace);
        var soap12Fault = NegativeTestSupport.FindFault(NegativeTestSupport.Soap11FaultEnvelope, Soap12Namespace);

        Assert.IsNotNull(soap11Fault, "The fixture must contain a SOAP 1.1 fault.");
        Assert.IsNull(soap12Fault, "The fixture must not contain a SOAP 1.2 fault, or the discrimination is not being tested.");
        StringAssert.Contains(soap11Fault!.Value, "The operation failed on purpose.", "The SOAP 1.1 fault must be populated.");

        var soap12Check = SoapClientFactoryHelper.CreateSoap12Client()
            .CheckBodyForFaultCode(NegativeTestSupport.Soap11FaultEnvelope);

        var soap11Check = SoapClientFactoryHelper.CreateSoap11Client()
            .CheckBodyForFaultCode(NegativeTestSupport.Soap11FaultEnvelope);

        Assert.IsTrue(
            soap12Check.IsSuccess,
            "The SOAP 1.2 client looks for faults in the SOAP 1.2 namespace, so a 1.1 fault is not one of its faults.");

        Assert.IsFalse(
            soap11Check.IsSuccess,
            $"The SOAP 1.1 client must recognise the very same document as a fault. Got: {NegativeTestSupport.Describe(soap11Check)}");

        StringAssert.Contains(
            NegativeTestSupport.FirstMessageInfo(soap11Check),
            "The operation failed on purpose.",
            "The SOAP 1.1 client must surface the fault string.");
    }

    [TestMethod]
    public void CheckBodyForFaultCode_WithFaultFreeResponse_ReportsSuccess()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.IsNull(
            NegativeTestSupport.FindFault(NegativeTestSupport.UnindentedSoap12Envelope, Soap12Namespace),
            "The fault-free fixture must not contain a SOAP 1.2 fault.");

        Assert.AreEqual(
            "HelloWorldResponse",
            SoapAssert.GetBodyChild(NegativeTestSupport.UnindentedSoap12Envelope).Name.LocalName,
            "The fault-free fixture must carry an operation payload.");

        var check = client.CheckBodyForFaultCode(NegativeTestSupport.UnindentedSoap12Envelope);

        Assert.IsTrue(check.IsSuccess, $"A response with no fault must be a success. Got: {NegativeTestSupport.Describe(check)}");
    }
}
