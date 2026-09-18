using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Service.Common;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Wcf.Tests.Transport;

[TestClass]
public sealed class PlainEndpointTests
{

    private const string ActionNotSupportedCode = "ActionNotSupported";

    public TestContext TestContext { get; set; } = null!;

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task Echo_RoundTripsTheValue_Test(SoapProtocolType protocol)
    {
        var client = WcfCallSupport.CreateClient(protocol);
        var value = $"echo-{protocol}";

        var request = WcfCallSupport.BuildPost(
            client,
            WcfHostFixture.Address(WcfHostFixture.PlainRelativePath, protocol),
            WcfCallSupport.EchoBody(value),
            WcfProbeContract.EchoAction);

        var body = await WcfCallSupport.SendAcceptedAsync(client, request, $"Echo({protocol})");

        Assert.AreEqual(value, WcfCallSupport.ReadEcho(client, body));
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_IsAnonymous_Test(SoapProtocolType protocol)
    {
        var client = WcfCallSupport.CreateClient(protocol);

        var request = WcfCallSupport.BuildPost(
            client,
            WcfHostFixture.Address(WcfHostFixture.PlainRelativePath, protocol),
            WcfCallSupport.WhoAmIBody(),
            WcfProbeContract.WhoAmIAction);

        var identity = WcfCallSupport.ReadWhoAmI(
            client,
            await WcfCallSupport.SendAcceptedAsync(client, request, $"WhoAmI({protocol})"));

        Assert.AreEqual(WcfProbeContract.AnonymousAuthenticationType, identity.AuthenticationType);
        Assert.AreEqual(string.Empty, identity.Name);
    }

    [TestMethod]
    public async Task Soap12_WithoutBodyElementDispatch_IsRejectedAsActionNotSupported_Test()
    {
        var client = WcfCallSupport.CreateClient(SoapProtocolType.SOAP_1_2);

        var request = WcfCallSupport.BuildPost(
            client,
            WcfHostFixture.Address(WcfHostFixture.StrictActionRelativePath, SoapProtocolType.SOAP_1_2),
            WcfCallSupport.EchoBody("strict"),
            WcfProbeContract.EchoAction);

        var rejected = await WcfCallSupport.SendRejectedAsync(client, request, "Echo(SOAP 1.2, action-dispatched endpoint)");

        TestContext.WriteLine($"Fault: {rejected.Fault}");

        rejected.AssertFault(
            WsSecurityNames.AddressingNoneNamespace,
            ActionNotSupportedCode,
            "action-dispatched endpoint");
    }
}
