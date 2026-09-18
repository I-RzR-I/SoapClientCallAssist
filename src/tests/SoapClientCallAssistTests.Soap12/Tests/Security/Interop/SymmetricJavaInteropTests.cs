#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers.Interop;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Interop;

[TestClass]
public sealed class SymmetricJavaInteropTests
{

    private const string PrimarySignatureTarget = "primary signature";

    [ClassCleanup]
    public static void ReleaseInteropWorkspace() => JavaInteropSupport.Cleanup();

    [DataTestMethod]
    [DataRow(SymmetricJavaInteropSupport.CertificateCase, "certificate credential, WS-SC February 2005, endorsed")]
    [DataRow(SymmetricJavaInteropSupport.UserNameCase, "username credential, token signed then encrypted")]
    [DataRow(SymmetricJavaInteropSupport.CertificateDecember2005Case, "certificate credential, WS-SC December 2005")]
    [DataRow(SymmetricJavaInteropSupport.EncryptedSignatureCase, "username credential with the primary signature encrypted")]
    public void JavaVerifier_UnwrapsTheSecretAndDerivesTheSameKeysAsTheLibrary_Test(string caseName, string shape)
    {
        var run = SymmetricJavaInteropSupport.Verified();
        var result = SymmetricJavaInteropSupport.Case(run, caseName);
        var expected = SymmetricJavaInteropSupport.Expectation(run, caseName);

        Assert.AreEqual(SymmetricTestSupport.RsaOaepMgf1p, result.KeyWrapAlgorithm, $"{caseName} | {shape} | {result}");

        JavaInteropAssert.AssertValid(
            result.KeyUnwrap,
            caseName,
            $"{result}");

        var signatureKey = result.DerivedKey(expected.SignatureTokenId);

        Assert.IsNotNull(signatureKey, $"{expected.SignatureTokenId} | {string.Join(" | ", result.DerivedKeys)} | {result}");

        Console.WriteLine(
            $"[KAT] {caseName} signature key: java={signatureKey.KeyBase64} library={expected.LibrarySignatureKeyBase64} " +
            $"recomputed={expected.RecomputedSignatureKeyBase64} (offset={signatureKey.Offset} length={signatureKey.Length} nonce={signatureKey.NonceBase64})");

        Assert.AreEqual(expected.LibrarySignatureKeyBase64, signatureKey.KeyBase64, $"{caseName} | {signatureKey} | {expected}");

        Assert.AreEqual(expected.RecomputedSignatureKeyBase64, signatureKey.KeyBase64, $"{caseName} | {signatureKey} | {expected}");

        Assert.AreEqual(SymmetricTestSupport.DefaultLabel, signatureKey.Label, $"{caseName} | {signatureKey}");

        if (expected.EncryptionTokenId is null)
        {
            Assert.AreEqual(1, result.DerivedKeys.Count, $"{caseName} | {result}");

            return;
        }

        var encryptionKey = result.DerivedKey(expected.EncryptionTokenId);

        Assert.IsNotNull(encryptionKey, $"{expected.EncryptionTokenId} | {string.Join(" | ", result.DerivedKeys)} | {result}");

        Console.WriteLine(
            $"[KAT] {caseName} encryption key: java={encryptionKey.KeyBase64} library={expected.LibraryEncryptionKeyBase64} " +
            $"(offset={encryptionKey.Offset} length={encryptionKey.Length} nonce={encryptionKey.NonceBase64})");

        Assert.AreEqual(expected.LibraryEncryptionKeyBase64, encryptionKey.KeyBase64, $"{caseName} | {encryptionKey} | {expected}");

        Assert.AreEqual(2, result.DerivedKeys.Count, $"{caseName} | {result}");
    }

    [DataTestMethod]
    [DataRow(SymmetricJavaInteropSupport.CertificateCase)]
    [DataRow(SymmetricJavaInteropSupport.UserNameCase)]
    [DataRow(SymmetricJavaInteropSupport.CertificateDecember2005Case)]
    [DataRow(SymmetricJavaInteropSupport.EncryptedSignatureCase)]
    public void JavaVerifier_AcceptsThePrimaryHmacSignatureUnderTheDerivedKey_Test(string caseName)
    {
        var run = SymmetricJavaInteropSupport.Verified();
        var result = SymmetricJavaInteropSupport.Case(run, caseName);
        var expected = SymmetricJavaInteropSupport.Expectation(run, caseName);

        Assert.AreEqual(WsSecurityTestSupport.HmacSha256Signature, result.SignatureMethod, $"{caseName} | {result}");

        Assert.AreEqual(WsSecurityTestSupport.ExclusiveC14N, result.CanonicalizationMethod, $"{caseName} | {result}");

        Assert.AreEqual("absent", result.HmacOutputLength, $"{caseName} | {result}");

        JavaInteropAssert.AssertValid(
            result.CoreValidity,
            caseName,
            $"{caseName} | {result}");

        JavaInteropAssert.AssertValid(
            result.SignatureValueValidity,
            caseName,
            $"{caseName} | {result}");

        Assert.IsTrue(result.References.Count > 0, $"{caseName} | {result}");

        foreach (var reference in result.References)
        {
            JavaInteropAssert.AssertValid(
                reference.DigestValid,
                $"{reference.Uri} | {caseName}",
                $"{caseName} | {reference}");

            Assert.IsTrue(reference.Resolved, $"{reference.Uri} | {caseName} | {reference}");
        }

        Assert.IsNotNull(
            result.Reference(expected.BodyReferenceUri),
            $"{caseName} | {expected.BodyReferenceUri} | {string.Join(", ", result.References.Select(reference => reference.Uri))} | {result}");
    }

    [DataTestMethod]
    [DataRow(SymmetricJavaInteropSupport.CertificateCase)]
    [DataRow(SymmetricJavaInteropSupport.CertificateDecember2005Case)]
    public void JavaVerifier_AcceptsTheEndorsingRsaSignatureAndResolvesItsReferenceToThePrimarySignature_Test(string caseName)
    {
        var run = SymmetricJavaInteropSupport.Verified();
        var result = SymmetricJavaInteropSupport.Case(run, caseName);
        var expected = SymmetricJavaInteropSupport.Expectation(run, caseName);

        Assert.AreEqual(WsSecurityTestSupport.RsaSha256Signature, result.EndorsingSignatureMethod, $"{caseName} | {result}");

        JavaInteropAssert.AssertValid(
            result.EndorsingCoreValidity,
            caseName,
            $"{caseName} | {result}");

        JavaInteropAssert.AssertValid(
            result.EndorsingSignatureValueValidity,
            caseName,
            $"{caseName} | {result}");

        JavaInteropAssert.AssertValid(
            result.EndorsingDigestValid,
            caseName,
            $"{caseName} | {result}");

        Assert.IsFalse(string.IsNullOrEmpty(expected.PrimarySignatureId), $"{caseName} | {expected}");

        Assert.AreEqual("#" + expected.PrimarySignatureId, result.EndorsingReferenceUri, $"{caseName} | {result}");

        Assert.AreEqual(PrimarySignatureTarget, result.EndorsingTarget, $"{caseName} | {result}");
    }

    [DataTestMethod]
    [DataRow(SymmetricJavaInteropSupport.UserNameCase)]
    [DataRow(SymmetricJavaInteropSupport.EncryptedSignatureCase)]
    public void JavaVerifier_DecryptsTheUsernameTokenAndVerifiesItsDigestUnderThePrimarySignature_Test(string caseName)
    {
        var run = SymmetricJavaInteropSupport.Verified();
        var result = SymmetricJavaInteropSupport.Case(run, caseName);
        var expected = SymmetricJavaInteropSupport.Expectation(run, caseName);

        Assert.AreEqual(expected.EncryptedDataCount, result.Decryptions.Count, $"{caseName} | {result}");

        foreach (var decryption in result.Decryptions)
        {
            JavaInteropAssert.AssertValid(
                decryption.Verdict,
                $"{decryption.Id} | {caseName}",
                $"{caseName} | {decryption} | {result}");
        }

        Assert.IsTrue(
            result.Decryptions.Any(decryption => decryption.ElementLocalName == "UsernameToken"),
            $"{caseName} | {string.Join(" | ", result.Decryptions)} | {result}");

        JavaInteropAssert.AssertValid(
            result.UsernameToken,
            caseName,
            $"{caseName} | {result}");

        Assert.AreEqual(WsSecurityFoundationTestSupport.Username, result.Username, $"{caseName} | {result}");

        Assert.IsNotNull(expected.UsernameTokenReferenceUri, $"{caseName} | {expected}");

        Assert.AreEqual(expected.UsernameTokenReferenceUri, "#" + result.UsernameTokenId, $"{caseName} | {result}");

        var tokenReference = result.Reference(expected.UsernameTokenReferenceUri);

        Assert.IsNotNull(
            tokenReference,
            $"{caseName} | {expected.UsernameTokenReferenceUri} | {string.Join(", ", result.References.Select(reference => reference.Uri))} | {result}");

        Assert.IsTrue(tokenReference.Resolved, $"{caseName} | {tokenReference}");

        JavaInteropAssert.AssertValid(
            tokenReference.DigestValid,
            caseName,
            $"{caseName} | {tokenReference}");
    }

    [TestMethod]
    public void JavaVerifier_DecryptsTheEncryptedPrimarySignatureBeforeValidatingIt_Test()
    {
        var run = SymmetricJavaInteropSupport.Verified();
        var result = SymmetricJavaInteropSupport.Case(run, SymmetricJavaInteropSupport.EncryptedSignatureCase);
        var expected = SymmetricJavaInteropSupport.Expectation(run, SymmetricJavaInteropSupport.EncryptedSignatureCase);

        Assert.AreEqual(2, expected.EncryptedDataCount, $"{expected}");

        Assert.IsNull(expected.PrimarySignatureId, $"{expected}");

        Assert.IsTrue(
            result.Decryptions.Any(decryption => decryption.ElementLocalName == "Signature" && decryption.Verdict.IsValid),
            $"{string.Join(" | ", result.Decryptions)} | {result}");

        Assert.IsNotNull(result.PrimarySignature, $"{result}");

        JavaInteropAssert.AssertValid(
            result.CoreValidity,
            "CORE VALIDITY",
            $"{result}");
    }

    [TestMethod]
    public void JavaVerifier_WhenTheSignedBodyIsTampered_ReportsTheBodyDigestInvalidWhileTheHmacOverSignedInfoStaysValid_Test()
    {
        var run = SymmetricJavaInteropSupport.Verified();
        var result = SymmetricJavaInteropSupport.Case(run, SymmetricJavaInteropSupport.TamperedBodyCase);
        var expected = SymmetricJavaInteropSupport.Expectation(run, SymmetricJavaInteropSupport.TamperedBodyCase);

        JavaInteropAssert.AssertValid(
            result.KeyUnwrap,
            "EncryptedKey unwrap",
            $"{result}");

        var body = result.Reference(expected.BodyReferenceUri);

        Assert.IsNotNull(body, $"{expected.BodyReferenceUri} | {string.Join(", ", result.References.Select(reference => reference.Uri))} | {result}");

        Assert.IsTrue(body.Resolved, $"{body}");

        JavaInteropAssert.AssertInvalid(
            body.DigestValid,
            $"{expected.BodyReferenceUri}",
            $"{body}");

        Assert.AreNotEqual(body.ExpectedDigest, body.CalculatedDigest, $"{body}");

        JavaInteropAssert.AssertValid(
            result.SignatureValueValidity,
            "",
            $"{result}");

        JavaInteropAssert.AssertInvalid(
            result.CoreValidity,
            "CORE VALIDITY",
            $"{result}");
    }

    [DataTestMethod]
    [DataRow(SymmetricJavaInteropSupport.WrongSecretCase, "the EncryptedKey CipherValue carries a different secret wrapped for the same service key")]
    [DataRow(SymmetricJavaInteropSupport.SwappedNonceCase, "the DerivedKeyToken nonce is swapped for another one")]
    public void JavaVerifier_WhenTheDerivedKeyNoLongerMatches_ReportsTheHmacInvalidWhileEveryDigestStaysValid_Test(string caseName, string mutation)
    {
        var run = SymmetricJavaInteropSupport.Verified();
        var result = SymmetricJavaInteropSupport.Case(run, caseName);
        var expected = SymmetricJavaInteropSupport.Expectation(run, caseName);

        JavaInteropAssert.AssertValid(
            result.KeyUnwrap,
            caseName,
            $"{mutation} | {result}");

        var signatureKey = result.DerivedKey(expected.SignatureTokenId);

        Assert.IsNotNull(signatureKey, $"{caseName} | {result}");

        Console.WriteLine(
            $"[KAT] {caseName} signature key: java={signatureKey.KeyBase64} library={expected.LibrarySignatureKeyBase64} " +
            $"original={expected.OriginalSignatureKeyBase64}");

        Assert.AreEqual(expected.LibrarySignatureKeyBase64, signatureKey.KeyBase64, $"{caseName} | {result}");

        Assert.AreNotEqual(expected.OriginalSignatureKeyBase64, signatureKey.KeyBase64, $"{mutation} | {result}");

        JavaInteropAssert.AssertInvalid(
            result.SignatureValueValidity,
            caseName,
            $"{mutation} | {result}");

        JavaInteropAssert.AssertInvalid(
            result.CoreValidity,
            caseName,
            $"{mutation} | {result}");

        Assert.IsTrue(result.References.Count > 0, $"{caseName} | {result}");

        foreach (var reference in result.References)
        {
            JavaInteropAssert.AssertValid(
                reference.DigestValid,
                $"{reference.Uri} | {caseName}",
                $"{caseName} | {reference}");
        }
    }

    [TestMethod]
    public void JavaVerifier_WithTheWrongServiceKeystore_ReportsErrorRatherThanInvalid_Test()
    {
        var run = SymmetricJavaInteropSupport.Verified();
        var result = SymmetricJavaInteropSupport.Case(run, SymmetricJavaInteropSupport.WrongKeystoreCase);

        JavaInteropAssert.AssertError(
            result.KeyUnwrap,
            "EncryptedKey unwrap",
            $"{result}");

        Assert.AreEqual(0, result.DerivedKeys.Count, $"{result}");

        JavaInteropAssert.AssertError(
            result.CoreValidity,
            "CORE VALIDITY",
            $"{result}");

        JavaInteropAssert.AssertError(
            result.SignatureValueValidity,
            "",
            $"{result}");

        Assert.AreEqual(0, result.References.Count, $"{result}");
    }

    [TestMethod]
    public void JavaVerifier_ReportsTheJdkItSelectedAndThatItMeetsTheHmacValidationMinimum_Test()
    {
        var run = SymmetricJavaInteropSupport.Verified();

        Assert.IsNotNull(run.Toolchain);

        Console.WriteLine($"[JDK] {run.Toolchain.SelectionReport}");

        Assert.IsTrue(
            run.Toolchain.MajorVersion >= JavaInteropToolchain.SymmetricMinimumMajorVersion,
            $"{JavaInteropToolchain.SymmetricMinimumMajorVersion} | {run.Toolchain.SelectionReport}");

        Assert.IsNull(run.Toolchain.SymmetricLimitation, run.Toolchain.SymmetricLimitation);

        var result = SymmetricJavaInteropSupport.Case(run, SymmetricJavaInteropSupport.CertificateCase);

        Console.WriteLine($"[JDK] Java reported: {result.JavaRuntime}");

        Assert.AreEqual(
            run.Toolchain.MajorVersion,
            JavaInteropToolchain.ParseMajorVersion(result.JavaRuntime),
            $"{JavaInteropVerdict.Or(result.JavaRuntime)} | {run.Toolchain.SelectionReport}");
    }

    [DataTestMethod]
    [DataRow("1.8.0_302", 8)]
    [DataRow("11.0.16.1", 11)]
    [DataRow("17", 17)]
    [DataRow("21.0.2+13", 21)]
    [DataRow("1.7.0", 7)]
    [DataRow("", JavaInteropToolchain.UnknownMajorVersion)]
    [DataRow(null, JavaInteropToolchain.UnknownMajorVersion)]
    [DataRow("banana", JavaInteropToolchain.UnknownMajorVersion)]
    public void ParseMajorVersion_ReadsBothLegacyAndModernJavaVersionStrings_Test(string version, int expected)
        => Assert.AreEqual(expected, JavaInteropToolchain.ParseMajorVersion(version), JavaInteropVerdict.Or(version));
}
