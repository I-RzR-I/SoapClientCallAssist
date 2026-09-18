#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Helpers.Saml;
using SoapClientCallAssistTests.Wcf.Service.Saml;
using System;
using System.Threading.Tasks;
using System.Xml;

namespace SoapClientCallAssistTests.Wcf.Tests.Saml;

[TestClass]
public sealed class SamlBearerTokenTests
{

    private const string InvalidSecurityCode = "InvalidSecurity";

    private const string FailedAuthenticationCode = "FailedAuthentication";

    private const string InvalidSecurityTokenCode = "InvalidSecurityToken";

    private static readonly TimeSpan ServiceClockSkew = TimeSpan.FromSeconds(5);

    private static SamlProbeHost _host;

    private static TestCertificate _issuer;

    private static TestCertificate _rogueIssuer;

    public TestContext TestContext { get; set; }

    [ClassInitialize]
    public static void OpenHost(TestContext testContext)
    {
        _issuer = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.SamlIssuer");
        _rogueIssuer = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.SamlRogueIssuer");
        _host = SamlProbeHost.Open(_issuer.Thumbprint, ServiceClockSkew, testContext);
    }

    [ClassCleanup]
    public static void CloseHost()
    {
        _host?.Dispose();
        _issuer?.Dispose();
        _rogueIssuer?.Dispose();
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, true, DisplayName = "SOAP 1.1, SAML 2.0")]
    [DataRow(SoapProtocolType.SOAP_1_2, true, DisplayName = "SOAP 1.2, SAML 2.0")]
    [DataRow(SoapProtocolType.SOAP_1_1, false, DisplayName = "SOAP 1.1, SAML 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, false, DisplayName = "SOAP 1.2, SAML 1.1")]
    public async Task WhoAmI_WithABearerAssertionFromTheTrustedIssuer_IsAuthenticatedWithTheAssertedClaims_Test(SoapProtocolType protocol, bool saml20)
    {
        var claims = await SendAcceptedAsync(protocol, Assertion(protocol, options => options.Saml20 = saml20), $"bearer SAML {(saml20 ? "2.0" : "1.1")}");

        Assert.AreEqual(SamlCallSupport.SubjectName, claims.Name);
        Assert.AreEqual(SamlCallSupport.SubjectRole, claims.Role);
        Assert.AreEqual(SamlProbeContract.IssuerName, claims.Issuer);
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithAPrettyPrintedBearerAssertion_IsAuthenticatedBecauseTheSignedWhitespaceSurvived_Test(SoapProtocolType protocol)
    {
        var assertion = Assertion(protocol, options => options.PrettyPrint = true);

        Assert.IsTrue(assertion.OuterXml.Contains("\r\n    "));

        var claims = await SendAcceptedAsync(protocol, assertion, "pretty-printed bearer assertion");

        Assert.AreEqual(SamlCallSupport.SubjectName, claims.Name);
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithABearerAssertionAndNoTimestamp_IsStillAuthenticated_Test(SoapProtocolType protocol)
    {
        var client = WcfCallSupport.CreateClient(protocol);
        var assertion = Assertion(protocol);

        var request = WcfCallSupport.BuildPost(
            client,
            _host.Address(SamlProbeContract.BearerRelativePath, protocol),
            SamlCallSupport.WhoAmIBody(),
            SamlProbeContract.WhoAmIAction,
            security: SamlCallSupport.BearerSecurity(assertion, includeTimestamp: false));

        var claims = SamlCallSupport.ReadWhoAmI(client, await WcfCallSupport.SendAcceptedAsync(client, request, $"WhoAmI({protocol}, bearer without timestamp)"));

        Assert.AreEqual(SamlCallSupport.SubjectName, claims.Name);
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithAnAssertionSignedByAnUnregisteredIssuer_IsRejected_Test(SoapProtocolType protocol)
    {
        var assertion = Assertion(protocol, options => options.IssuerCertificate = _rogueIssuer.Certificate);

        var rejected = await SendRejectedAsync(protocol, assertion, "unregistered issuer");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityTokenCode, "unregistered issuer");
        rejected.AssertNoSecret(SamlCallSupport.Secrets(assertion), "unregistered issuer");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithAnAssertionForAnotherAudience_IsRejected_Test(SoapProtocolType protocol)
    {
        var assertion = Assertion(protocol, options => options.Audience = "http://SoapClientCallAssist.local/wcf/some-other-service/");

        var rejected = await SendRejectedAsync(protocol, assertion, "wrong audience");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, FailedAuthenticationCode, "wrong audience");
        rejected.AssertNoSecret(SamlCallSupport.Secrets(assertion), "wrong audience");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithAnAssertionExpiredBeyondTheServiceSkew_IsRejected_Test(SoapProtocolType protocol)
    {
        var assertion = Assertion(protocol, options =>
        {
            options.NotBefore = DateTime.UtcNow.AddMinutes(-10);
            options.NotOnOrAfter = DateTime.UtcNow.AddMinutes(-1);
        });

        var rejected = await SendRejectedAsync(protocol, assertion, "expired");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, FailedAuthenticationCode, "expired");
        rejected.AssertNoSecret(SamlCallSupport.Secrets(assertion), "expired");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithATamperedAssertion_IsRejected_Test(SoapProtocolType protocol)
    {
        var assertion = SamlCallSupport.Tamper(Assertion(protocol));

        var rejected = await SendRejectedAsync(protocol, assertion, "tampered");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityCode, "tampered");
        rejected.AssertNoSecret(SamlCallSupport.Secrets(assertion), "tampered");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithAnUnsignedAssertion_IsRejected_Test(SoapProtocolType protocol)
    {
        var assertion = Assertion(protocol, options => options.Sign = false);

        var rejected = await SendRejectedAsync(protocol, assertion, "unsigned");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, FailedAuthenticationCode, "unsigned");
        rejected.AssertNoSecret(SamlCallSupport.Secrets(assertion), "unsigned");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithoutAnyToken_IsRejected_Test(SoapProtocolType protocol)
    {
        var client = WcfCallSupport.CreateClient(protocol);

        var request = WcfCallSupport.BuildPost(
            client,
            _host.Address(SamlProbeContract.BearerRelativePath, protocol),
            SamlCallSupport.WhoAmIBody(),
            SamlProbeContract.WhoAmIAction);

        var rejected = await WcfCallSupport.SendRejectedAsync(client, request, $"WhoAmI({protocol}, no token)");

        TestContext.WriteLine($"{protocol} no token: {rejected.Fault}");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityCode, "no token");
    }

    private XmlElement Assertion(SoapProtocolType protocol, Action<SamlIssuanceOptions> tune = null)
        => SamlCallSupport.Assertion(options =>
        {
            options.IssuerCertificate = _issuer.Certificate;
            options.Audience = _host.Address(SamlProbeContract.BearerRelativePath, protocol).AbsoluteUri;
            tune?.Invoke(options);
        });

    private async Task<SamlCallerClaims> SendAcceptedAsync(SoapProtocolType protocol, XmlElement assertion, string scenario)
    {
        var client = WcfCallSupport.CreateClient(protocol);

        var request = WcfCallSupport.BuildPost(
            client,
            _host.Address(SamlProbeContract.BearerRelativePath, protocol),
            SamlCallSupport.WhoAmIBody(),
            SamlProbeContract.WhoAmIAction,
            security: SamlCallSupport.BearerSecurity(assertion));

        var claims = SamlCallSupport.ReadWhoAmI(client, await WcfCallSupport.SendAcceptedAsync(client, request, $"WhoAmI({protocol}, {scenario})"));

        TestContext.WriteLine($"{protocol} {scenario}: {claims.AuthenticationType} name={claims.Name} role={claims.Role} issuer={claims.Issuer}");

        return claims;
    }

    private async Task<RejectedCall> SendRejectedAsync(SoapProtocolType protocol, XmlElement assertion, string scenario)
    {
        var client = WcfCallSupport.CreateClient(protocol);

        var request = WcfCallSupport.BuildPost(
            client,
            _host.Address(SamlProbeContract.BearerRelativePath, protocol),
            SamlCallSupport.WhoAmIBody(),
            SamlProbeContract.WhoAmIAction,
            security: SamlCallSupport.BearerSecurity(assertion));

        var rejected = await WcfCallSupport.SendRejectedAsync(client, request, $"WhoAmI({protocol}, {scenario})");

        TestContext.WriteLine($"{protocol} {scenario}: {rejected.Fault}");

        return rejected;
    }
}
