using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

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
            $"SendRequestAsync {protocol}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{protocol}");

        var recorded = Soap12FunctionalSupport.FindRecorded(correlationId);

        Assert.IsTrue(
            recorded.Headers.TryGetValue("SOAPAction", out var soapActionValues)
            && soapActionValues.Contains(Action),
            $"{Action} | {protocol}");

        Assert.IsTrue(
            recorded.Headers.TryGetValue("Action", out var actionValues)
            && actionValues.Contains(Action),
            $"DEFECT-BARE-ACTION-HEADER {protocol}");
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
            "SendRequestAsync");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var recorded = Soap12FunctionalSupport.FindRecorded(correlationId);

        Assert.IsFalse(recorded.Headers.ContainsKey("Action"));
    }
}
