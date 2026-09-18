#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class WsSecurityCertificateScenarioTests
{

    private readonly WsSecurityMessageVerifier _verifier = new WsSecurityMessageVerifier();

    public TestContext TestContext { get; set; }

    [TestMethod]
    public void BuildRequest_WithAPfxWrittenToDiskAndLoadedWithAPassword_SignsAndVerifies_Test()
    {
        using var source = CertificateScenarioSupport.CreateRsaCertificate("CN=Scca.Tests.PfxOnDisk", 2048);

        var path = CertificateScenarioSupport.WritePfx(source, CertificateScenarioSupport.PfxPassword);

        try
        {
            using var loaded = CertificateScenarioSupport.LoadPfx(
                path, CertificateScenarioSupport.PfxPassword, X509KeyStorageFlags.EphemeralKeySet);

            Assert.IsTrue(loaded.HasPrivateKey, $"{loaded.Subject}");

            var wire = SignedWire(loaded);

            WsSecurityAssert.Accepted(
                _verifier.Verify(wire, loaded),
                "");
        }
        finally
        {
            CertificateScenarioSupport.Delete(path);
        }
    }

    [DataTestMethod]
    [DataRow(X509KeyStorageFlags.DefaultKeySet, DisplayName = "DefaultKeySet")]
    [DataRow(X509KeyStorageFlags.EphemeralKeySet, DisplayName = "EphemeralKeySet")]
    [DataRow(X509KeyStorageFlags.UserKeySet, DisplayName = "UserKeySet")]
    public void BuildRequest_WithAPfxLoadedUnderEachKeyStorageFlag_SignsAndVerifiesIdentically_Test(
        X509KeyStorageFlags flags)
    {
        using var source = CertificateScenarioSupport.CreateRsaCertificate("CN=Scca.Tests.KeyStorageFlags", 2048);

        var path = CertificateScenarioSupport.WritePfx(source, CertificateScenarioSupport.PfxPassword);

        try
        {
            using var loaded = CertificateScenarioSupport.LoadPfx(path, CertificateScenarioSupport.PfxPassword, flags);

            var wire = SignedWire(loaded);
            var signatureValue = CertificateScenarioSupport.SignatureValue(wire);

            TestContext.WriteLine($"key storage flags={flags} SignatureValue length={signatureValue.Length}");

            Assert.AreEqual(CertificateScenarioSupport.Rsa2048SignatureValueLength, signatureValue.Length, $"{flags}");

            WsSecurityAssert.Accepted(
                _verifier.Verify(wire, loaded),
                $"{flags}");
        }
        finally
        {
            CertificateScenarioSupport.Delete(path);
        }
    }

    [TestMethod]
    public void BuildRequest_WithA4096BitCertificate_DoublesTheSignatureWidthAndStillVerifies_Test()
    {
        using var narrow = CertificateScenarioSupport.CreateRsaCertificate("CN=Scca.Tests.Rsa2048", 2048);
        using var wide = CertificateScenarioSupport.CreateRsaCertificate("CN=Scca.Tests.Rsa4096", 4096);

        var narrowWire = SignedWire(narrow);
        var wideWire = SignedWire(wide);

        var narrowSignature = CertificateScenarioSupport.SignatureValue(narrowWire);
        var wideSignature = CertificateScenarioSupport.SignatureValue(wideWire);
        var narrowToken = CertificateScenarioSupport.BinarySecurityToken(narrowWire);
        var wideToken = CertificateScenarioSupport.BinarySecurityToken(wideWire);

        TestContext.WriteLine(
            $"2048-bit: SignatureValue={narrowSignature.Length} BinarySecurityToken={narrowToken.Length}");
        TestContext.WriteLine(
            $"4096-bit: SignatureValue={wideSignature.Length} BinarySecurityToken={wideToken.Length}");

        Assert.AreEqual(CertificateScenarioSupport.Rsa2048SignatureValueLength, narrowSignature.Length);

        Assert.AreEqual(CertificateScenarioSupport.Rsa4096SignatureValueLength, wideSignature.Length);

        Assert.IsTrue(wideToken.Length > narrowToken.Length, $"{wideToken.Length} | {narrowToken.Length}");

        WsSecurityAssert.Accepted(
            _verifier.Verify(wideWire, wide),
            "");
    }

    [TestMethod]
    public void BuildRequest_WithAnEcdsaCertificate_FailsWithTheSigningKeyReasonRatherThanThrowing_Test()
    {
        using var certificate = CertificateScenarioSupport.CreateEcdsaCertificate("CN=Scca.Tests.Ecdsa");

        Assert.IsTrue(certificate.HasPrivateKey);

        Assert.IsNull(certificate.GetRSAPrivateKey());

        var built = WsSecurityTestSupport.BuildPost(
            WsSecurityTestSupport.Security(security => security.SigningCertificate = certificate));

        Assert.IsFalse(built.IsSuccess, NegativeTestSupport.Describe(built));

        Assert.IsNull(built.Response);

        Assert.AreEqual(
            WsSecurityTestSupport.MissingSigningKeyMessage,
            NegativeTestSupport.FirstMessageInfo(built),
            NegativeTestSupport.Describe(built));
    }

    [TestMethod]
    public void BuildRequest_WithAPublicKeyOnlyCertificate_CannotSignYetThatCertificateStillVerifies_Test()
    {
        using var signing = CertificateScenarioSupport.CreateRsaCertificate("CN=Scca.Tests.PublicOnly", 2048);
        using var publicOnly = X509CertificateLoader.LoadCertificate(signing.Export(X509ContentType.Cert));

        Assert.IsFalse(publicOnly.HasPrivateKey);

        var built = WsSecurityTestSupport.BuildPost(
            WsSecurityTestSupport.Security(security => security.SigningCertificate = publicOnly));

        Assert.IsFalse(built.IsSuccess, NegativeTestSupport.Describe(built));

        WsSecurityAssert.Accepted(
            _verifier.Verify(SignedWire(signing), publicOnly),
            "");
    }

    [TestMethod]
    public void BuildRequest_WithAnExpiredCertificate_SignsAndVerifiesBecauseValidityDatesAreNotChecked_Test()
    {
        using var certificate = CertificateScenarioSupport.CreateExpiredRsaCertificate("CN=Scca.Tests.Expired");

        Assert.IsTrue(certificate.NotAfter < DateTime.Now, $"{certificate.NotAfter:u}");

        var wire = SignedWire(certificate);

        WsSecurityAssert.Accepted(
            _verifier.Verify(wire, certificate),
            "");
    }

    [TestMethod]
    public void BuildRequest_WhenSigningFails_DisclosesNothingAboutTheCertificateFileKeyOrPassword_Test()
    {
        using var source = CertificateScenarioSupport.CreateEcdsaCertificate("CN=Scca.Tests.Disclosure.Probe");

        var path = CertificateScenarioSupport.WritePfx(source, CertificateScenarioSupport.PfxPassword);

        try
        {
            using var loaded = CertificateScenarioSupport.LoadPfx(
                path, CertificateScenarioSupport.PfxPassword, X509KeyStorageFlags.EphemeralKeySet);

            var built = WsSecurityTestSupport.BuildPost(
                WsSecurityTestSupport.Security(security => security.SigningCertificate = loaded));

            Assert.IsFalse(built.IsSuccess);

            var reported = CertificateScenarioSupport.FullMessageText(built);

            foreach (var secret in new[]
                     {
                         path,
                         Path.GetFileName(path),
                         CertificateScenarioSupport.PfxPassword,
                         loaded.Subject,
                         loaded.Thumbprint,
                         "CryptographicException",
                         "Key Container"
                     })
                Assert.IsFalse(reported.Contains(secret, StringComparison.OrdinalIgnoreCase), $"{secret} | {reported}");
        }
        finally
        {
            CertificateScenarioSupport.Delete(path);
        }
    }

    private static string SignedWire(X509Certificate2 certificate)
        => WsSecurityTestSupport.SignedWire(
            WsSecurityTestSupport.Security(security => security.SigningCertificate = certificate));
}
