using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Helpers.Symmetric;
using SoapClientCallAssistTests.Wcf.Service.Common;
using SoapClientCallAssistTests.Wcf.Service.Symmetric;
using System;
using System.Linq;
using System.ServiceModel.Security;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Tests.Symmetric;

[TestClass]
public sealed class SymmetricBindingAcceptanceTests
{

    private const string InvalidSecurityCode = "InvalidSecurity";

    private const string FailedAuthenticationCode = "FailedAuthentication";

    private const string ConsumedCode = "V-SEC-031";

    private const string WrongPassword = "not-the-symmetric-secret";

    private static SymmetricProbeHost? _host;

    public TestContext TestContext { get; set; } = null!;

    private static SymmetricProbeHost Host => _host ?? throw new InvalidOperationException("The symmetric host did not start.");

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _host = SymmetricProbeHost.Open();

        foreach (var endpoint in _host.Endpoints())
            context.WriteLine(endpoint);
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _host?.Dispose();
        _host = null;
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, SoapSecureConversationVersionType.February2005, DisplayName = "SOAP 1.1, SC Feb 2005")]
    [DataRow(SoapProtocolType.SOAP_1_2, SoapSecureConversationVersionType.February2005, DisplayName = "SOAP 1.2, SC Feb 2005")]
    [DataRow(SoapProtocolType.SOAP_1_1, SoapSecureConversationVersionType.December2005, DisplayName = "SOAP 1.1, SC Dec 2005")]
    [DataRow(SoapProtocolType.SOAP_1_2, SoapSecureConversationVersionType.December2005, DisplayName = "SOAP 1.2, SC Dec 2005")]
    public async Task WhoAmI_CertificateCredential_IsAuthenticatedAsTheEndorsingCertificateAndTheResponseVerifies_Test(
        SoapProtocolType protocol, SoapSecureConversationVersionType version)
    {
        var client = WcfCallSupport.CreateClient(protocol);
        var endpoint = Host.Address(SymmetricProbeHost.CertificateCredential, version, SymmetricCallSupport.DefaultOrder, protocol);

        using var exchange = await SymmetricCallSupport.SendAcceptedAsync(
            client,
            SymmetricCallSupport.Build(client, endpoint, SymmetricCallSupport.WhoAmIBody(), SymmetricProbeContract.WhoAmIAction,
                SymmetricCallSupport.CertificateSecurity(Host, Host.ClientCertificate, version)),
            $"WhoAmI({protocol}, {version}, certificate)");

        var identity = SymmetricCallSupport.ReadWhoAmI(client, exchange.ResponseWire);

        TestContext.WriteLine($"WhoAmI={identity.AuthenticationType}:{identity.Name}");

        Assert.AreEqual(WcfProbeContract.X509AuthenticationType, identity.AuthenticationType);
        Assert.AreEqual(Host.ClientCertificate.Thumbprint, identity.Name);

        var coverage = SymmetricCallSupport.VerifyAccepted(exchange, $"WhoAmI({protocol}, {version}, certificate)");

        Assert.IsTrue(coverage.BodySigned);
        Assert.IsTrue(coverage.TimestampSigned);
        Assert.IsTrue(coverage.SignedElementLocalNames.Contains("RelatesTo"));
        Assert.AreEqual(2, XDocument.Parse(exchange.ResponseWire).Descendants().Count(element => element.Name.LocalName == "SignatureConfirmation"));
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, SoapSecureConversationVersionType.February2005, DisplayName = "SOAP 1.1, SC Feb 2005")]
    [DataRow(SoapProtocolType.SOAP_1_2, SoapSecureConversationVersionType.February2005, DisplayName = "SOAP 1.2, SC Feb 2005")]
    [DataRow(SoapProtocolType.SOAP_1_1, SoapSecureConversationVersionType.December2005, DisplayName = "SOAP 1.1, SC Dec 2005")]
    [DataRow(SoapProtocolType.SOAP_1_2, SoapSecureConversationVersionType.December2005, DisplayName = "SOAP 1.2, SC Dec 2005")]
    public async Task WhoAmI_UserNameCredential_IsAuthenticatedAsThatUserAndTheResponseVerifies_Test(
        SoapProtocolType protocol, SoapSecureConversationVersionType version)
    {
        var client = WcfCallSupport.CreateClient(protocol);
        var endpoint = Host.Address(SymmetricProbeHost.UserNameCredential, version, SymmetricCallSupport.DefaultOrder, protocol);

        using var exchange = await SymmetricCallSupport.SendAcceptedAsync(
            client,
            SymmetricCallSupport.Build(client, endpoint, SymmetricCallSupport.WhoAmIBody(), SymmetricProbeContract.WhoAmIAction,
                SymmetricCallSupport.UserNameSecurity(Host, SymmetricProbeHost.KnownPassword, version)),
            $"WhoAmI({protocol}, {version}, username)");

        var identity = SymmetricCallSupport.ReadWhoAmI(client, exchange.ResponseWire);

        TestContext.WriteLine($"WhoAmI={identity.AuthenticationType}:{identity.Name}");

        Assert.AreEqual(SymmetricProbeHost.KnownUserName, identity.Name);
        Assert.AreEqual(nameof(InMemoryUserNamePasswordValidator), identity.AuthenticationType);
        Assert.IsFalse(exchange.RequestWire.Contains(SymmetricProbeHost.KnownPassword));

        var coverage = SymmetricCallSupport.VerifyAccepted(exchange, $"WhoAmI({protocol}, {version}, username)");

        Assert.IsTrue(coverage.BodySigned);
        Assert.IsTrue(coverage.TimestampSigned);
    }

    [DataTestMethod]
    [DataRow(MessageProtectionOrder.SignBeforeEncryptAndEncryptSignature, true, DisplayName = "encrypted request signature")]
    [DataRow(MessageProtectionOrder.SignBeforeEncrypt, false, DisplayName = "plain request signature")]
    public async Task Echo_UserNameCredential_RoundTripsWithAndWithoutTheSignatureEncrypted_Test(MessageProtectionOrder order, bool encryptSignature)
    {
        var client = WcfCallSupport.CreateClient(SoapProtocolType.SOAP_1_1);
        var endpoint = Host.Address(SymmetricProbeHost.UserNameCredential, SoapSecureConversationVersionType.February2005, order, SoapProtocolType.SOAP_1_1);
        var value = $"symmetric-{order}";

        using var exchange = await SymmetricCallSupport.SendAcceptedAsync(
            client,
            SymmetricCallSupport.Build(client, endpoint, SymmetricCallSupport.EchoBody(value), SymmetricProbeContract.EchoAction,
                SymmetricCallSupport.UserNameSecurity(Host, SymmetricProbeHost.KnownPassword, SoapSecureConversationVersionType.February2005,
                    security => security.Encryption = new SoapEncryptionDto { EncryptSignature = encryptSignature })),
            $"Echo({order})");

        Assert.AreEqual(value, SymmetricCallSupport.ReadEcho(client, exchange.ResponseWire));
        Assert.AreEqual(encryptSignature ? 0 : 1, XDocument.Parse(exchange.RequestWire).Descendants().Count(element => element.Name.LocalName == "Signature"));

        SymmetricCallSupport.VerifyAccepted(exchange, $"Echo({order})");
    }

    [TestMethod]
    public async Task Verify_ASecondTimeOnTheSameRequest_IsRefusedAsConsumed_Test()
    {
        var client = WcfCallSupport.CreateClient(SoapProtocolType.SOAP_1_1);
        var endpoint = Host.Address(SymmetricProbeHost.CertificateCredential, SoapSecureConversationVersionType.February2005, SymmetricCallSupport.DefaultOrder, SoapProtocolType.SOAP_1_1);

        using var exchange = await SymmetricCallSupport.SendAcceptedAsync(
            client,
            SymmetricCallSupport.Build(client, endpoint, SymmetricCallSupport.WhoAmIBody(), SymmetricProbeContract.WhoAmIAction,
                SymmetricCallSupport.CertificateSecurity(Host, Host.ClientCertificate, SoapSecureConversationVersionType.February2005)),
            "WhoAmI(consumed)");

        SymmetricCallSupport.VerifyAccepted(exchange, "WhoAmI(consumed)");

        var second = new WsSecurityResponseSecurity().Verify(exchange.Request, exchange.ResponseWire);

        Assert.IsFalse(second.IsSuccess);
        Assert.AreEqual(ConsumedCode, ResultRendering.FirstKey(second));
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_UserNameCredentialWithAWrongPassword_IsRejectedAsFailedAuthentication_Test(SoapProtocolType protocol)
    {
        var rejected = await SendRejectedAsync(
            protocol,
            SymmetricProbeHost.UserNameCredential,
            SymmetricCallSupport.UserNameSecurity(Host, WrongPassword, SoapSecureConversationVersionType.February2005),
            "wrong password");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, FailedAuthenticationCode, "wrong password");
        rejected.AssertNoSecret(SymmetricCallSupport.Secrets(Host, WrongPassword), "wrong password");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_CertificateCredentialWithAnUntrustedCertificate_IsRejectedAsFailedAuthentication_Test(SoapProtocolType protocol)
    {
        var rejected = await SendRejectedAsync(
            protocol,
            SymmetricProbeHost.CertificateCredential,
            SymmetricCallSupport.CertificateSecurity(Host, Host.UntrustedCertificate, SoapSecureConversationVersionType.February2005),
            "untrusted certificate");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, FailedAuthenticationCode, "untrusted certificate");
        rejected.AssertNoSecret(SymmetricCallSupport.Secrets(Host), "untrusted certificate");
    }

    [DataTestMethod]
    [DataRow(SymmetricProbeHost.CertificateCredential, DisplayName = "certificate endpoint")]
    [DataRow(SymmetricProbeHost.UserNameCredential, DisplayName = "username endpoint")]
    public async Task WhoAmI_Unsigned_IsRejectedAsInvalidSecurity_Test(string credential)
    {
        var rejected = await SendRejectedAsync(SoapProtocolType.SOAP_1_1, credential, null, "unsigned");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityCode, "unsigned");
        rejected.AssertNoSecret(SymmetricCallSupport.Secrets(Host), "unsigned");
    }

    [TestMethod]
    public async Task WhoAmI_CertificateCredentialWithoutTheEndorsingSignature_IsRejectedAsInvalidSecurity_Test()
    {
        var rejected = await SendRejectedAsync(
            SoapProtocolType.SOAP_1_1,
            SymmetricProbeHost.CertificateCredential,
            SymmetricCallSupport.CertificateSecurity(Host, Host.ClientCertificate, SoapSecureConversationVersionType.February2005,
                security => security.SymmetricBinding!.EndorseWithSigningCertificate = false),
            "no endorsing signature");

        TestContext.WriteLine(rejected.Fault.ToString());

        Assert.AreEqual(WsSecurityNames.Wsse.NamespaceName, rejected.Fault.NamespaceName);
        rejected.AssertNoSecret(SymmetricCallSupport.Secrets(Host), "no endorsing signature");
    }

    private async Task<RejectedCall> SendRejectedAsync(SoapProtocolType protocol, string credential, SoapSecurityDto? security, string scenario)
    {
        var client = WcfCallSupport.CreateClient(protocol);
        var endpoint = Host.Address(credential, SoapSecureConversationVersionType.February2005, SymmetricCallSupport.DefaultOrder, protocol);

        var request = security is null
            ? WcfCallSupport.BuildPost(client, endpoint, SymmetricCallSupport.WhoAmIBody(), SymmetricProbeContract.WhoAmIAction,
                headers: SymmetricCallSupport.UnsignedAddressingHeaders(endpoint, SymmetricProbeContract.WhoAmIAction))
            : SymmetricCallSupport.Build(client, endpoint, SymmetricCallSupport.WhoAmIBody(), SymmetricProbeContract.WhoAmIAction, security);

        var rejected = await SymmetricCallSupport.SendRejectedAsync(client, request, $"WhoAmI({protocol}, {credential}, {scenario})");

        TestContext.WriteLine($"{protocol} {credential} {scenario}: {rejected.Fault}");

        return rejected;
    }
}
