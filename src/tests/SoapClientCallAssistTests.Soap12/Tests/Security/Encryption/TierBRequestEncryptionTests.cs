#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Encryption;

[TestClass]
public sealed class TierBRequestEncryptionTests
{

    private static readonly ForbiddenSecret PasswordSecret = ForbiddenSecret.OfText("the username token password", WsSecurityFoundationTestSupport.Password);

    [TestMethod]
    public void BuildRequest_WithEncryptBody_EncryptsTheBodyContentAndKeepsTheSignedBodyId_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var body = TierBTestSupport.Body(wire.Document);
        var encryptedData = TierBTestSupport.BodyEncryptedData(wire.Document);

        Assert.IsNotNull(encryptedData);
        Assert.AreEqual(1, body.ChildNodes.Count);
        Assert.IsTrue(WsSecurityTestSupport.WsuId(body).Length > 0);
        Assert.AreEqual(TierBTestSupport.ContentType, encryptedData.GetAttribute("Type"));
        Assert.AreEqual(SymmetricTestSupport.Aes256Cbc, SymmetricTestSupport.ChildOf(encryptedData, "EncryptionMethod", SymmetricTestSupport.XencNamespace).GetAttribute("Algorithm"));
        Assert.AreEqual("#" + WsSecurityTestSupport.WsuId(wire.EncryptionToken), SymmetricWire.KeyInfoReference(encryptedData).GetAttribute("URI"));
        Assert.IsFalse(wire.Text.Contains("IsValid"));
        Assert.IsFalse(wire.Text.Contains(WsSecurityFoundationTestSupport.Password));
    }

    [TestMethod]
    public void BuildRequest_WithEncryptBody_ListsTheBodyInTheReferenceListAfterTheDerivedKeyTokens_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);

        var references = SymmetricTestSupport.ChildrenOf(wire.ReferenceList, "DataReference", SymmetricTestSupport.XencNamespace).Select(reference => reference.GetAttribute("URI")).ToList();
        var bodyId = TierBTestSupport.BodyEncryptedData(wire.Document).GetAttribute("Id");

        CollectionAssert.Contains(references, "#" + bodyId);
        Assert.AreEqual(3, references.Count);

        var order = SymmetricTestSupport.SecurityChildLocalNames(wire.Document).ToList();

        Assert.IsTrue(order.IndexOf("ReferenceList") > order.LastIndexOf("DerivedKeyToken"));
    }

    [TestMethod]
    public void BuildRequest_WithEncryptBody_SignsThenEncryptsSoTheSignatureVerifiesOverThePlaintextBody_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);

        Assert.IsNull(wire.PrimarySignature);
        Assert.IsTrue(TierBTestSupport.PrimarySignatureVerifiesOverThePlaintextBody(wire));

        var plaintext = TierBTestSupport.DecryptBodyContent(wire);

        Assert.IsTrue(plaintext.Contains("IsValid"));
        Assert.IsFalse(plaintext.Contains("Body"));
    }

    [TestMethod]
    public void BuildRequest_Twice_UsesAFreshSecretAndAFreshInitialisationVectorPerBuild_Test()
    {
        using var first = TierBTestSupport.BuildRequest();
        using var second = TierBTestSupport.BuildRequest();
        var firstWire = TierBTestSupport.Parse(first);
        var secondWire = TierBTestSupport.Parse(second);

        CollectionAssert.AreNotEqual(firstWire.Secret, secondWire.Secret);

        var firstIv = TierBTestSupport.CipherValueOf(TierBTestSupport.BodyEncryptedData(firstWire.Document)).Take(16).ToArray();
        var secondIv = TierBTestSupport.CipherValueOf(TierBTestSupport.BodyEncryptedData(secondWire.Document)).Take(16).ToArray();

        CollectionAssert.AreNotEqual(firstIv, secondIv);

        var bodyIv = TierBTestSupport.CipherValueOf(TierBTestSupport.BodyEncryptedData(firstWire.Document)).Take(16).ToArray();
        var signatureIv = TierBTestSupport.CipherValueOf(firstWire.EncryptedDatas.Last()).Take(16).ToArray();

        CollectionAssert.AreNotEqual(bodyIv, signatureIv);
    }

    [TestMethod]
    public void BuildRequest_WithEncryptBodyButNotTheSignature_LeavesTheSignatureInClearOverTheEncryptedBody_Test()
    {
        using var request = TierBTestSupport.BuildRequest(security => security.Encryption.EncryptSignature = false);
        var wire = TierBTestSupport.Parse(request);

        Assert.IsNotNull(wire.PrimarySignature);
        Assert.IsNotNull(TierBTestSupport.BodyEncryptedData(wire.Document));
        Assert.AreEqual(2, SymmetricTestSupport.ChildrenOf(wire.ReferenceList, "DataReference", SymmetricTestSupport.XencNamespace).Count);
        Assert.IsTrue(TierBTestSupport.PrimarySignatureVerifiesOverThePlaintextBody(wire));
    }

    [TestMethod]
    public void BuildRequest_WithTheCertificateCredentialAndEncryptBody_EndorsesTheEncryptedPrimarySignature_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(TierBTestSupport.CertificateSecurity());
        var wire = TierBTestSupport.Parse(request);

        Assert.IsNotNull(wire.EndorsingSignature);
        Assert.IsNull(wire.PrimarySignature);
        Assert.IsNotNull(TierBTestSupport.BodyEncryptedData(wire.Document));
        Assert.IsTrue(TierBTestSupport.PrimarySignatureVerifiesOverThePlaintextBody(wire));
    }

    [DataTestMethod]
    [DataRow(SoapDataEncryptionAlgorithmType.Aes128Cbc, 16, TierBTestSupport.Aes128Cbc)]
    [DataRow(SoapDataEncryptionAlgorithmType.Aes192Cbc, 24, "http://www.w3.org/2001/04/xmlenc#aes192-cbc")]
    public void BuildRequest_WithASmallerDataAlgorithm_EncryptsTheBodyUnderThatAlgorithm_Test(SoapDataEncryptionAlgorithmType algorithm, int keyLength, string uri)
    {
        using var request = TierBTestSupport.BuildRequest(security =>
        {
            security.Encryption.DataAlgorithm = algorithm;
            security.SymmetricBinding.EncryptionKeyLength = keyLength;
        });
        var wire = TierBTestSupport.Parse(request);
        var encryptedData = TierBTestSupport.BodyEncryptedData(wire.Document);

        Assert.AreEqual(uri, SymmetricTestSupport.ChildOf(encryptedData, "EncryptionMethod", SymmetricTestSupport.XencNamespace).GetAttribute("Algorithm"));
        Assert.AreEqual(keyLength, wire.EncryptionKey.Length);
        Assert.IsTrue(TierBTestSupport.PrimarySignatureVerifiesOverThePlaintextBody(wire));
    }

    [TestMethod]
    public void BuildRequest_WithAServiceCertificateBelow2048Bits_RefusesUnderTheRecipientRule_Test()
    {
        using var certificate = TierBTestSupport.CreateServiceCertificate(1024, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1), null);

        Refused(certificate, "");
    }

    [TestMethod]
    public void BuildRequest_WithAnExpiredServiceCertificate_RefusesUnderTheRecipientRule_Test()
    {
        using var certificate = TierBTestSupport.CreateServiceCertificate(2048, DateTimeOffset.UtcNow.AddDays(-400), DateTimeOffset.UtcNow.AddDays(-35), null);

        Refused(certificate, "");
    }

    [TestMethod]
    public void BuildRequest_WithANotYetValidServiceCertificate_RefusesUnderTheRecipientRule_Test()
    {
        using var certificate = TierBTestSupport.CreateServiceCertificate(2048, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(30), null);

        Refused(certificate, "");
    }

    [TestMethod]
    public void BuildRequest_WithAServiceCertificateWhoseKeyUsageForbidsKeyEncipherment_RefusesUnderTheRecipientRule_Test()
    {
        using var certificate = TierBTestSupport.CreateServiceCertificate(2048, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1), X509KeyUsageFlags.DigitalSignature);

        Refused(certificate, "");
    }

    [TestMethod]
    public void BuildRequest_WithAServiceCertificateWhoseKeyUsagePermitsKeyEncipherment_Builds_Test()
    {
        using var certificate = TierBTestSupport.CreateServiceCertificate(2048, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1), X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature);

        using var request = TierBTestSupport.BuildRequest(security => security.SymmetricBinding.ServiceCertificate = certificate);

        Assert.IsNotNull(TierBTestSupport.BodyEncryptedData(SymmetricTestSupport.NewDocument(SymmetricTestSupport.Wire(request))));
    }

    [TestMethod]
    public void BuildRequest_WithAServiceCertificateJustInsideTheClockSkew_Builds_Test()
    {
        using var certificate = TierBTestSupport.CreateServiceCertificate(2048, DateTimeOffset.UtcNow.AddMinutes(2), DateTimeOffset.UtcNow.AddDays(1), null);

        using var request = TierBTestSupport.BuildRequest(security => security.SymmetricBinding.ServiceCertificate = certificate);

        Assert.IsNotNull(TierBTestSupport.BodyEncryptedData(SymmetricTestSupport.NewDocument(SymmetricTestSupport.Wire(request))));
    }

    [TestMethod]
    public void BuildRequest_WithoutEncryptBody_DoesNotApplyTheRecipientRule_Test()
    {
        using var certificate = TierBTestSupport.CreateServiceCertificate(2048, DateTimeOffset.UtcNow.AddDays(-400), DateTimeOffset.UtcNow.AddDays(-35), null);

        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security => security.SymmetricBinding.ServiceCertificate = certificate));

        Assert.IsNull(TierBTestSupport.BodyEncryptedData(SymmetricTestSupport.NewDocument(SymmetricTestSupport.Wire(request))));
    }

    [TestMethod]
    public void BuildRequest_WithEncryptBody_RefusesRsa15KeyWrapByName_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(TierBTestSupport.Security(security => security.Encryption.KeyWrap = SoapKeyWrapAlgorithmType.Rsa15)),
            "V-SEC-055",
            "",
            PasswordSecret);

    private static void Refused(X509Certificate2 certificate, string because)
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(TierBTestSupport.Security(security => security.SymmetricBinding.ServiceCertificate = certificate)),
            TierBTestSupport.RecipientCertificateCode,
            because,
            PasswordSecret,
            ForbiddenSecret.OfText("the service certificate subject", certificate.Subject),
            ForbiddenSecret.OfText("the service certificate thumbprint", certificate.Thumbprint));
}
