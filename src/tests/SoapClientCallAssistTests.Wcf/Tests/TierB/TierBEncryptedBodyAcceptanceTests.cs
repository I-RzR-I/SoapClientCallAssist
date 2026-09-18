using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Helpers.Symmetric;
using SoapClientCallAssistTests.Wcf.Helpers.TierB;
using SoapClientCallAssistTests.Wcf.Service.Common;
using SoapClientCallAssistTests.Wcf.Service.TierB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Security;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Wcf.Tests.TierB;

[TestClass]
public sealed class TierBEncryptedBodyAcceptanceTests
{

    private const string InvalidSecurityCode = "InvalidSecurity";

    private const string FailedAuthenticationCode = "FailedAuthentication";

    private const string WrongPassword = "not-the-tier-b-secret";

    private const string DoctypePlaintext = "<!DOCTYPE a [<!ENTITY e \"x\">]><a>&e;</a>";

    private static TierBProbeHost? _host;

    public TestContext TestContext { get; set; } = null!;

    private static TierBProbeHost Host => _host ?? throw new InvalidOperationException("The Tier B host did not start.");

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _host = TierBProbeHost.Open();

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
    public async Task WhoAmI_CertificateCredential_IsAuthenticatedAndTheEncryptedResponseDecryptsAndVerifies_Test(
        SoapProtocolType protocol, SoapSecureConversationVersionType version)
    {
        var client = WcfCallSupport.CreateClient(protocol);
        var endpoint = Host.Address(TierBProbeHost.CertificateCredential, version, TierBCallSupport.DefaultOrder, protocol);
        var what = $"WhoAmI({protocol}, {version}, certificate)";

        using var exchange = await SymmetricCallSupport.SendAcceptedAsync(
            client,
            TierBCallSupport.Build(client, endpoint, TierBCallSupport.WhoAmIBody(), TierBProbeContract.WhoAmIAction, TierBCallSupport.CertificateSecurity(Host, version)),
            what);

        AssertEncryptedOnTheWire(exchange, "WhoAmI", "WhoAmIResult", true);

        var plaintext = TierBCallSupport.DecryptAccepted(exchange, what);
        var identity = TierBCallSupport.ReadWhoAmI(client, plaintext);

        TestContext.WriteLine($"WhoAmI={identity.AuthenticationType}:{identity.Name}");
        TestContext.WriteLine(DescribeResponseShape(exchange));

        Assert.AreEqual(WcfProbeContract.X509AuthenticationType, identity.AuthenticationType);
        Assert.AreEqual(Host.ClientCertificate.Thumbprint, identity.Name);
        Assert.AreEqual(0, TierBCallSupport.CountElements(plaintext, "EncryptedData"));
        Assert.AreEqual(1, TierBCallSupport.CountElements(plaintext, "Signature"));
        Assert.AreEqual(2, TierBCallSupport.CountElements(plaintext, "SignatureConfirmation"));
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_UserNameCredential_IsAuthenticatedAsThatUserAndTheEncryptedResponseDecryptsAndVerifies_Test(SoapProtocolType protocol)
    {
        var client = WcfCallSupport.CreateClient(protocol);
        var endpoint = Host.Address(TierBProbeHost.UserNameCredential, SoapSecureConversationVersionType.February2005, TierBCallSupport.DefaultOrder, protocol);
        var what = $"WhoAmI({protocol}, username)";

        using var exchange = await SymmetricCallSupport.SendAcceptedAsync(
            client,
            TierBCallSupport.Build(client, endpoint, TierBCallSupport.WhoAmIBody(), TierBProbeContract.WhoAmIAction,
                TierBCallSupport.UserNameSecurity(Host, TierBProbeHost.KnownPassword, SoapSecureConversationVersionType.February2005)),
            what);

        AssertEncryptedOnTheWire(exchange, "WhoAmI", "WhoAmIResult", true);
        Assert.IsFalse(exchange.RequestWire.Contains(TierBProbeHost.KnownPassword));

        var plaintext = TierBCallSupport.DecryptAccepted(exchange, what);
        var identity = TierBCallSupport.ReadWhoAmI(client, plaintext);

        TestContext.WriteLine($"WhoAmI={identity.AuthenticationType}:{identity.Name}");
        TestContext.WriteLine(DescribeResponseShape(exchange));

        Assert.AreEqual(TierBProbeHost.KnownUserName, identity.Name);
        Assert.AreEqual(nameof(InMemoryUserNamePasswordValidator), identity.AuthenticationType);
    }

    [DataTestMethod]
    [DataRow(MessageProtectionOrder.SignBeforeEncryptAndEncryptSignature, true, DisplayName = "encrypted signatures")]
    [DataRow(MessageProtectionOrder.SignBeforeEncrypt, false, DisplayName = "signatures in clear")]
    public async Task Echo_UserNameCredential_RoundTripsUnderBothProtectionOrders_Test(MessageProtectionOrder order, bool encryptSignature)
    {
        var client = WcfCallSupport.CreateClient(SoapProtocolType.SOAP_1_1);
        var endpoint = Host.Address(TierBProbeHost.UserNameCredential, SoapSecureConversationVersionType.February2005, order, SoapProtocolType.SOAP_1_1);
        var value = $"tier-b-{order}-{Guid.NewGuid():N}";
        var what = $"Echo({order})";

        using var exchange = await SymmetricCallSupport.SendAcceptedAsync(
            client,
            TierBCallSupport.Build(client, endpoint, TierBCallSupport.EchoBody(value), TierBProbeContract.EchoAction,
                TierBCallSupport.UserNameSecurity(Host, TierBProbeHost.KnownPassword, SoapSecureConversationVersionType.February2005,
                    security => security.Encryption!.EncryptSignature = encryptSignature)),
            what);

        AssertEncryptedOnTheWire(exchange, "Echo", "EchoResult", encryptSignature);
        Assert.IsFalse(exchange.RequestWire.Contains(value));
        Assert.IsFalse(exchange.ResponseWire.Contains(value));

        var plaintext = TierBCallSupport.DecryptAccepted(exchange, what);

        TestContext.WriteLine(DescribeResponseShape(exchange));

        Assert.AreEqual(value, TierBCallSupport.ReadEcho(client, plaintext));
    }

    [TestMethod]
    public async Task Verify_OverTheEncryptedResponse_RefusesByNameAndConsumesTheMaterial_Test()
    {
        using var exchange = await SendWhoAmIAsync(TierBCallSupport.DefaultOrder, null, "WhoAmI(verify)");
        var material = TierBExchangeMaterial.Of(Host, exchange);

        var verified = new WsSecurityResponseSecurity().Verify(exchange.Request, exchange.ResponseWire);

        Assert.IsFalse(verified.IsSuccess);
        Assert.AreEqual(TierBCallSupport.DecryptionNotAllowedCode, ResultRendering.FirstKey(verified));
        ResultLeakSweep.AssertNoSecret(verified, TierBCallSupport.Secrets(Host, material), "Verify");

        TierBCallSupport.DecryptRefused(exchange.Request, exchange.ResponseWire, TierBCallSupport.ConsumedCode, TierBCallSupport.Secrets(Host, material), "DecryptAndVerify after Verify");
    }

    [TestMethod]
    public async Task DecryptAndVerify_ASecondTimeOnTheSameRequest_IsRefusedAsConsumed_Test()
    {
        using var exchange = await SendWhoAmIAsync(TierBCallSupport.DefaultOrder, null, "WhoAmI(consumed)");
        var material = TierBExchangeMaterial.Of(Host, exchange);

        TierBCallSupport.DecryptAccepted(exchange, "WhoAmI(consumed)");
        TierBCallSupport.DecryptRefused(exchange.Request, exchange.ResponseWire, TierBCallSupport.ConsumedCode, TierBCallSupport.Secrets(Host, material), "second DecryptAndVerify");
    }

    [TestMethod]
    public async Task DecryptAndVerify_WithTamperedCipherText_RefusesUniformlyWithoutPlaintext_Test()
    {
        using var exchange = await SendWhoAmIAsync(TierBCallSupport.DefaultOrder, null, "WhoAmI(tampered cipher text)");
        var material = TierBExchangeMaterial.Of(Host, exchange);

        TierBCallSupport.DecryptRefused(exchange.Request, material.TamperBodyCipherText(), TierBCallSupport.DecryptionCode, Secrets(material), "tampered cipher text");
    }

    [TestMethod]
    public async Task DecryptAndVerify_UnderAKeyTheTokenDoesNotDerive_RefusesUniformlyWithoutPlaintext_Test()
    {
        using var exchange = await SendWhoAmIAsync(TierBCallSupport.DefaultOrder, null, "WhoAmI(wrong key)");
        var material = TierBExchangeMaterial.Of(Host, exchange);

        TierBCallSupport.DecryptRefused(exchange.Request, material.WithFreshEncryptionTokenNonce(), TierBCallSupport.DecryptionCode, Secrets(material), "wrong key");
    }

    [TestMethod]
    public async Task DecryptAndVerify_WithAPlantedForeignEncryptedKey_RefusesBeforeAnyKeyIsTouched_Test()
    {
        using var exchange = await SendWhoAmIAsync(TierBCallSupport.DefaultOrder, null, "WhoAmI(foreign EncryptedKey)");
        var material = TierBExchangeMaterial.Of(Host, exchange);

        TierBCallSupport.DecryptRefused(exchange.Request, material.PlantForeignEncryptedKey(), TierBCallSupport.EncryptedShapeCode, Secrets(material), "foreign EncryptedKey");
    }

    [TestMethod]
    public async Task DecryptAndVerify_WithAPlantedDuplicateId_RefusesBeforeAnyKeyIsTouched_Test()
    {
        using var exchange = await SendWhoAmIAsync(TierBCallSupport.DefaultOrder, null, "WhoAmI(duplicate Id)");
        var material = TierBExchangeMaterial.Of(Host, exchange);

        TierBCallSupport.DecryptRefused(exchange.Request, material.PlantDuplicateBodyId(), TierBCallSupport.EncryptedShapeCode, Secrets(material), "duplicate Id");
    }

    [TestMethod]
    public async Task DecryptAndVerify_WithACapTheCipherTextCannotFitUnder_RefusesBeforeDecrypting_Test()
    {
        using var exchange = await SendWhoAmIAsync(TierBCallSupport.DefaultOrder, security => security.ResponseSecurity!.MaxPlaintextBytes = 16, "WhoAmI(oversize)");
        var material = TierBExchangeMaterial.Of(Host, exchange);

        TierBCallSupport.DecryptRefused(exchange.Request, exchange.ResponseWire, TierBCallSupport.CipherCapCode, Secrets(material), "oversize");
    }

    [TestMethod]
    public async Task DecryptAndVerify_WithADoctypeReEncryptedUnderTheRealKey_RefusesUniformlyWithoutPlaintext_Test()
    {
        using var exchange = await SendWhoAmIAsync(TierBCallSupport.DefaultOrder, null, "WhoAmI(DOCTYPE)");
        var material = TierBExchangeMaterial.Of(Host, exchange);

        TierBCallSupport.DecryptRefused(exchange.Request, material.ReEncryptBody(DoctypePlaintext), TierBCallSupport.DecryptionCode, Secrets(material, "&e;", "ENTITY"), "DOCTYPE in the plaintext");
    }

    [TestMethod]
    public async Task DecryptAndVerify_WithAClearSignatureTamperedAfterSigning_RefusesUniformlyWithoutPlaintext_Test()
    {
        using var exchange = await SendWhoAmIAsync(MessageProtectionOrder.SignBeforeEncrypt, security => security.Encryption!.EncryptSignature = false, "WhoAmI(tampered signature)");
        var material = TierBExchangeMaterial.Of(Host, exchange);

        Assert.AreEqual(1, TierBCallSupport.CountElements(exchange.ResponseWire, "Signature"));

        TierBCallSupport.DecryptRefused(exchange.Request, material.TamperSignatureValue(), TierBCallSupport.DecryptionCode, Secrets(material), "tampered signature");
    }

    [TestMethod]
    public async Task WhoAmI_WithAPlaintextBodyOnTheEncryptAndSignContract_IsRejectedByTheService_Test()
    {
        var client = WcfCallSupport.CreateClient(SoapProtocolType.SOAP_1_1);
        var endpoint = Host.Address(TierBProbeHost.UserNameCredential, SoapSecureConversationVersionType.February2005, TierBCallSupport.DefaultOrder, SoapProtocolType.SOAP_1_1);

        var security = TierBCallSupport.UserNameSecurity(Host, TierBProbeHost.KnownPassword, SoapSecureConversationVersionType.February2005, security =>
        {
            security.Encryption = null;
            security.ResponseSecurity = new SoapResponseSecurityDto { RequireSignatureConfirmation = false };
        });

        var rejected = await SymmetricCallSupport.SendRejectedAsync(
            client,
            TierBCallSupport.Build(client, endpoint, TierBCallSupport.WhoAmIBody(), TierBProbeContract.WhoAmIAction, security),
            "WhoAmI(plaintext Body)");

        TestContext.WriteLine($"plaintext Body on EncryptAndSign: {rejected.Fault}");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityCode, "plaintext Body");
        rejected.AssertNoSecret(TierBCallSupport.Secrets(Host, null), "plaintext Body");
    }

    [TestMethod]
    public async Task WhoAmI_WithAWrongPassword_IsRejectedAsFailedAuthentication_Test()
    {
        var client = WcfCallSupport.CreateClient(SoapProtocolType.SOAP_1_1);
        var endpoint = Host.Address(TierBProbeHost.UserNameCredential, SoapSecureConversationVersionType.February2005, TierBCallSupport.DefaultOrder, SoapProtocolType.SOAP_1_1);

        var rejected = await SymmetricCallSupport.SendRejectedAsync(
            client,
            TierBCallSupport.Build(client, endpoint, TierBCallSupport.WhoAmIBody(), TierBProbeContract.WhoAmIAction,
                TierBCallSupport.UserNameSecurity(Host, WrongPassword, SoapSecureConversationVersionType.February2005)),
            "WhoAmI(wrong password)");

        TestContext.WriteLine($"wrong password: {rejected.Fault}");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, FailedAuthenticationCode, "wrong password");
        rejected.AssertNoSecret(TierBCallSupport.Secrets(Host, null, WrongPassword), "wrong password");
    }

    private async Task<SymmetricExchange> SendWhoAmIAsync(MessageProtectionOrder order, Action<SoapSecurityDto>? configure, string what)
    {
        var client = WcfCallSupport.CreateClient(SoapProtocolType.SOAP_1_1);
        var endpoint = Host.Address(TierBProbeHost.UserNameCredential, SoapSecureConversationVersionType.February2005, order, SoapProtocolType.SOAP_1_1);

        var exchange = await SymmetricCallSupport.SendAcceptedAsync(
            client,
            TierBCallSupport.Build(client, endpoint, TierBCallSupport.WhoAmIBody(), TierBProbeContract.WhoAmIAction,
                TierBCallSupport.UserNameSecurity(Host, TierBProbeHost.KnownPassword, SoapSecureConversationVersionType.February2005, configure)),
            what);

        Assert.AreEqual(0, TierBCallSupport.CountElements(exchange.ResponseWire, "WhoAmIResult"), what);

        return exchange;
    }

    private IEnumerable<string> Secrets(TierBExchangeMaterial material, params string[] extra)
        => TierBCallSupport.Secrets(Host, material, new[] { "WhoAmIResult", TierBProbeHost.KnownUserName, nameof(InMemoryUserNamePasswordValidator) }.Concat(extra).ToArray());

    private static void AssertEncryptedOnTheWire(SymmetricExchange exchange, string operation, string result, bool signatureEncrypted)
    {
        Assert.AreEqual(0, TierBCallSupport.CountElements(exchange.RequestWire, operation));
        Assert.AreEqual(0, TierBCallSupport.CountElements(exchange.ResponseWire, result));
        Assert.AreEqual(signatureEncrypted ? 0 : 1, TierBCallSupport.CountElements(exchange.ResponseWire, "Signature"));
        Assert.IsTrue(TierBCallSupport.CountElements(exchange.ResponseWire, "EncryptedData") >= 1);
        Assert.AreEqual(1, TierBCallSupport.CountElements(exchange.ResponseWire, "ReferenceList"));
        Assert.AreEqual(0, TierBCallSupport.CountElements(exchange.ResponseWire, "EncryptedKey"));
    }

    private static string DescribeResponseShape(SymmetricExchange exchange)
    {
        var material = TierBExchangeMaterial.Of(Host, exchange);

        return $"response encryption token: Length={material.EncryptionTokenLength ?? "<unstated>"}; "
               + $"EncryptedData={TierBCallSupport.CountElements(exchange.ResponseWire, "EncryptedData")}; "
               + $"DerivedKeyToken={TierBCallSupport.CountElements(exchange.ResponseWire, "DerivedKeyToken")}; "
               + $"SignatureConfirmation={TierBCallSupport.CountElements(exchange.ResponseWire, "SignatureConfirmation")}";
    }
}
