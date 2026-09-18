#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Helpers.Saml;
using SoapClientCallAssistTests.Wcf.Service.Saml;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml;

namespace SoapClientCallAssistTests.Wcf.Tests.Saml;

[TestClass]
public sealed class SamlHolderOfKeyTokenTests
{

    private const string InvalidSecurityCode = "InvalidSecurity";

    private const string FailedAuthenticationCode = "FailedAuthentication";

    private const string InvalidSecurityTokenCode = "InvalidSecurityToken";

    private static readonly TimeSpan ServiceClockSkew = TimeSpan.FromSeconds(5);

    private static SamlProbeHost _host;

    private static TestCertificate _issuer;

    private static TestCertificate _rogueIssuer;

    public TestContext TestContext { get; set; }

    private static X509Certificate2 ClientKey => WcfHostFixture.TrustedCertificate.Certificate;

    private static X509Certificate2 OtherKey => WcfHostFixture.UntrustedCertificate.Certificate;

    [ClassInitialize]
    public static void OpenHost(TestContext testContext)
    {
        _issuer = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.SamlIssuer.HoK");
        _rogueIssuer = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.SamlRogueIssuer.HoK");
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
    public async Task WhoAmI_WithAHolderOfKeyAssertionAndTheMessageSignedByTheNamedKey_IsAuthenticated_Test(SoapProtocolType protocol, bool saml20)
    {
        var claims = await SendAcceptedAsync(protocol, Assertion(protocol, ClientKey, options => options.Saml20 = saml20), ClientKey, $"holder-of-key SAML {(saml20 ? "2.0" : "1.1")}");

        Assert.AreEqual(SamlCallSupport.SubjectName, claims.Name);
        Assert.AreEqual(SamlCallSupport.SubjectRole, claims.Role);
        Assert.AreEqual(SamlProbeContract.IssuerName, claims.Issuer);
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithAPrettyPrintedHolderOfKeyAssertion_IsAuthenticated_Test(SoapProtocolType protocol)
    {
        var assertion = Assertion(protocol, ClientKey, options => options.PrettyPrint = true);

        Assert.IsTrue(assertion.OuterXml.Contains("\r\n    "));

        var claims = await SendAcceptedAsync(protocol, assertion, ClientKey, "pretty-printed holder-of-key");

        Assert.AreEqual(SamlCallSupport.SubjectName, claims.Name);
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithTheMessageSignedByAKeyOtherThanTheOneTheAssertionNames_IsRejected_Test(SoapProtocolType protocol)
    {
        var assertion = Assertion(protocol, OtherKey);

        var rejected = await SendRejectedAsync(protocol, assertion, ClientKey, "wrong holder-of-key key");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityCode, "wrong holder-of-key key");
        rejected.AssertNoSecret(SamlCallSupport.Secrets(assertion), "wrong holder-of-key key");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithAHolderOfKeyAssertionSignedByAnUnregisteredIssuer_IsRejected_Test(SoapProtocolType protocol)
    {
        var assertion = Assertion(protocol, ClientKey, options => options.IssuerCertificate = _rogueIssuer.Certificate);

        var rejected = await SendRejectedAsync(protocol, assertion, ClientKey, "unregistered issuer");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityTokenCode, "unregistered issuer");
        rejected.AssertNoSecret(SamlCallSupport.Secrets(assertion), "unregistered issuer");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithAHolderOfKeyAssertionExpiredBeyondTheServiceSkew_IsRejected_Test(SoapProtocolType protocol)
    {
        var assertion = Assertion(protocol, ClientKey, options =>
        {
            options.NotBefore = DateTime.UtcNow.AddMinutes(-10);
            options.NotOnOrAfter = DateTime.UtcNow.AddMinutes(-1);
        });

        var rejected = await SendRejectedAsync(protocol, assertion, ClientKey, "expired");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, FailedAuthenticationCode, "expired");
        rejected.AssertNoSecret(SamlCallSupport.Secrets(assertion), "expired");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithATamperedHolderOfKeyAssertion_IsRejected_Test(SoapProtocolType protocol)
    {
        var assertion = SamlCallSupport.Tamper(Assertion(protocol, ClientKey));

        var rejected = await SendRejectedAsync(protocol, assertion, ClientKey, "tampered");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityCode, "tampered");
        rejected.AssertNoSecret(SamlCallSupport.Secrets(assertion), "tampered");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithABearerAssertionOnTheHolderOfKeyEndpoint_IsRejected_Test(SoapProtocolType protocol)
    {
        var client = WcfCallSupport.CreateClient(protocol);
        var assertion = Assertion(protocol, null);

        var request = WcfCallSupport.BuildPost(
            client,
            _host.Address(SamlProbeContract.HolderOfKeyRelativePath, protocol),
            SamlCallSupport.WhoAmIBody(),
            SamlProbeContract.WhoAmIAction,
            security: SamlCallSupport.BearerSecurity(assertion));

        var rejected = await WcfCallSupport.SendRejectedAsync(client, request, $"WhoAmI({protocol}, bearer on holder-of-key endpoint)");

        TestContext.WriteLine($"{protocol} bearer on holder-of-key endpoint: {rejected.Fault}");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityCode, "bearer on holder-of-key endpoint");
        rejected.AssertNoSecret(SamlCallSupport.Secrets(assertion), "bearer on holder-of-key endpoint");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithTheAssertionCoveredByTheEndorsingSignature_IsRejectedBecauseTheTransportBindingCannotResolveIt_Test(SoapProtocolType protocol)
    {
        var client = WcfCallSupport.CreateClient(protocol);
        var assertion = Assertion(protocol, ClientKey);

        var security = SamlCallSupport.HolderOfKeySecurity(assertion, ClientKey);
        security.SamlToken.SignAssertion = true;

        var request = WcfCallSupport.BuildPost(
            client,
            _host.Address(SamlProbeContract.HolderOfKeyRelativePath, protocol),
            SamlCallSupport.WhoAmIBody(),
            SamlProbeContract.WhoAmIAction,
            security: security);

        var rejected = await WcfCallSupport.SendRejectedAsync(client, request, $"WhoAmI({protocol}, assertion covered by the signature)");

        TestContext.WriteLine($"{protocol} assertion covered by the signature: {rejected.Fault}");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityCode, "assertion covered by the signature");
        rejected.AssertNoSecret(SamlCallSupport.Secrets(assertion), "assertion covered by the signature");
    }

    private XmlElement Assertion(SoapProtocolType protocol, X509Certificate2 proof, Action<SamlIssuanceOptions> tune = null)
        => SamlCallSupport.Assertion(options =>
        {
            options.IssuerCertificate = _issuer.Certificate;
            options.ProofCertificate = proof;
            options.Audience = _host.Address(SamlProbeContract.HolderOfKeyRelativePath, protocol).AbsoluteUri;
            tune?.Invoke(options);
        });

    private async Task<SamlCallerClaims> SendAcceptedAsync(SoapProtocolType protocol, XmlElement assertion, X509Certificate2 signingKey, string scenario)
    {
        var client = WcfCallSupport.CreateClient(protocol);

        var request = WcfCallSupport.BuildPost(
            client,
            _host.Address(SamlProbeContract.HolderOfKeyRelativePath, protocol),
            SamlCallSupport.WhoAmIBody(),
            SamlProbeContract.WhoAmIAction,
            security: SamlCallSupport.HolderOfKeySecurity(assertion, signingKey));

        var claims = SamlCallSupport.ReadWhoAmI(client, await WcfCallSupport.SendAcceptedAsync(client, request, $"WhoAmI({protocol}, {scenario})"));

        TestContext.WriteLine($"{protocol} {scenario}: {claims.AuthenticationType} name={claims.Name} role={claims.Role} issuer={claims.Issuer}");

        return claims;
    }

    private async Task<RejectedCall> SendRejectedAsync(SoapProtocolType protocol, XmlElement assertion, X509Certificate2 signingKey, string scenario)
    {
        var client = WcfCallSupport.CreateClient(protocol);

        var request = WcfCallSupport.BuildPost(
            client,
            _host.Address(SamlProbeContract.HolderOfKeyRelativePath, protocol),
            SamlCallSupport.WhoAmIBody(),
            SamlProbeContract.WhoAmIAction,
            security: SamlCallSupport.HolderOfKeySecurity(assertion, signingKey));

        var rejected = await WcfCallSupport.SendRejectedAsync(client, request, $"WhoAmI({protocol}, {scenario})");

        TestContext.WriteLine($"{protocol} {scenario}: {rejected.Fault}");

        return rejected;
    }
}
