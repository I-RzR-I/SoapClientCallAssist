using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class ActionHeaderDefectTests
{
    private const string Action = SoapAssert.ServiceNs + "EchoValue";

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    public async Task SendRequest_WithAction_AlsoSendsANonStandardBareActionHeaderOnTheWire_Test(SoapProtocolType protocol)
    {
        var client = CrossProtocolSupport.CreateClient(protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            protocol,
            client,
            Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
            correlationId,
            action: Action);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync({protocol}, EchoValue, action)");

        Assert.AreEqual(
            HttpStatusCode.OK,
            response.StatusCode,
            $"Arranging this test requires a successful {protocol} call.");

        var recorded = Soap12FunctionalSupport.FindRecorded(correlationId);

        Assert.IsTrue(
            recorded.Headers.TryGetValue("SOAPAction", out var soapActionValues)
            && soapActionValues.Contains(Action),
            $"The service must receive the standard SOAPAction header carrying [{Action}] for {protocol}.");

        Assert.IsTrue(
            recorded.Headers.TryGetValue("Action", out var actionValues)
            && actionValues.Contains(Action),
            "DEFECT-BARE-ACTION-HEADER: pinning current behaviour: BaseEndpointClient.BuildSoapRequestMessage " +
            "(content.Headers.Add(\"Action\", soapRequest.Action)) adds a non-standard bare 'Action' content header " +
            "in addition to SOAPAction, for both protocols. 'Action' is not a defined HTTP or SOAP header. " +
            $"The service nonetheless received it for {protocol}.");
    }

    [TestMethod]
    [Ignore("DEFECT-BARE-ACTION-HEADER: unpins when fixed")]
    public async Task SendRequest_WithAction_ShouldNotSendTheNonStandardBareActionHeader_Test()
    {
        var client = CrossProtocolSupport.CreateClient(SoapProtocolType.SOAP_1_1);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            SoapProtocolType.SOAP_1_1,
            client,
            Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
            correlationId,
            action: Action);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync(SOAP 1.1, EchoValue, action)");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Arranging this test requires a successful call.");

        var recorded = Soap12FunctionalSupport.FindRecorded(correlationId);

        Assert.IsFalse(
            recorded.Headers.ContainsKey("Action"),
            "'Action' is not a defined HTTP or SOAP header and must not be sent; only SOAPAction is standard.");
    }
}
