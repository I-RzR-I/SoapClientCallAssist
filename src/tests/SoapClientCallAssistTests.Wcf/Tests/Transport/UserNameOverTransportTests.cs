using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Service.Common;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Tests.Transport;

[TestClass]
public sealed class UserNameOverTransportTests
{

    private const string InvalidSecurityCode = "InvalidSecurity";

    private const string FailedAuthenticationCode = "FailedAuthentication";

    private const string WrongPassword = "not-the-secret";

    private static readonly string[] PasswordSecrets = { WcfHostFixture.KnownPassword, WrongPassword };

    public TestContext TestContext { get; set; } = null!;

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithAKnownUsernameToken_IsAuthenticatedAsThatUser_Test(SoapProtocolType protocol)
    {
        var client = WcfCallSupport.CreateClient(protocol);

        var request = WcfCallSupport.BuildPost(
            client,
            WcfHostFixture.Address(WcfHostFixture.UserNameTransportRelativePath, protocol),
            WcfCallSupport.WhoAmIBody(),
            WcfProbeContract.WhoAmIAction,
            headers: new[] { Token(protocol, WcfHostFixture.KnownPassword) });

        var identity = WcfCallSupport.ReadWhoAmI(
            client,
            await WcfCallSupport.SendAcceptedAsync(client, request, $"WhoAmI({protocol}, UsernameToken)"));

        TestContext.WriteLine($"WhoAmI={identity.AuthenticationType}:{identity.Name}");

        Assert.AreEqual(WcfHostFixture.KnownUserName, identity.Name);
        Assert.AreEqual(nameof(InMemoryUserNamePasswordValidator), identity.AuthenticationType);
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithAWrongPassword_IsRejectedAsFailedAuthentication_Test(SoapProtocolType protocol)
    {
        var rejected = await SendRejectedAsync(protocol, new[] { Token(protocol, WrongPassword) }, "wrong password");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, FailedAuthenticationCode, "wrong password");
        rejected.AssertNoSecret(PasswordSecrets, "wrong password");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithoutAToken_IsRejected_Test(SoapProtocolType protocol)
    {
        var rejected = await SendRejectedAsync(protocol, null, "no token");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityCode, "no token");
        rejected.AssertNoSecret(PasswordSecrets, "no token");
    }

    private async Task<RejectedCall> SendRejectedAsync(SoapProtocolType protocol, XElement[]? headers, string scenario)
    {
        var client = WcfCallSupport.CreateClient(protocol);

        var request = WcfCallSupport.BuildPost(
            client,
            WcfHostFixture.Address(WcfHostFixture.UserNameTransportRelativePath, protocol),
            WcfCallSupport.WhoAmIBody(),
            WcfProbeContract.WhoAmIAction,
            headers: headers);

        var rejected = await WcfCallSupport.SendRejectedAsync(client, request, $"WhoAmI({protocol}, {scenario})");

        TestContext.WriteLine($"{protocol} {scenario}: {rejected.Fault}");

        return rejected;
    }

    private static XElement Token(SoapProtocolType protocol, string password)
        => UsernameTokenHeader.Build(WcfCallSupport.SoapNamespace(protocol), WcfHostFixture.KnownUserName, password);
}
