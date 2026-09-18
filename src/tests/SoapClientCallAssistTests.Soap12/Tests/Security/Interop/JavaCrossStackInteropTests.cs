#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Soap12.Helpers.Interop;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Interop;

[TestClass]
public sealed class JavaCrossStackInteropTests
{

    [ClassCleanup]
    public static void ReleaseInteropWorkspace() => JavaInteropSupport.Cleanup();

    [DataTestMethod]
    [DataRow("A_defaults", "RSA-SHA256 over SHA-256 digests, exclusive C14N, signed timestamp")]
    [DataRow("B_sha512", "RSA-SHA512 over SHA-512 digests")]
    [DataRow("D_namespaces", "a body whose children straddle two namespaces")]
    [DataRow("D2_unusedns", "a body declaring prefixes no descendant uses")]
    [DataRow("E_whitespace", "a body carrying leading, trailing, tab and CRLF whitespace")]
    [DataRow("F_bodyonly", "the body alone, with no timestamp, so the signature carries one reference")]
    [DataRow("G_unicode", "a non-ASCII UTF-8 payload")]
    [DataRow("I_actor", "a Security header targeting a SOAP role")]
    [DataRow("K_qname", "body text carrying QNames bound by envelope-level prefixes")]
    [DataRow("L_saml20_hok", "a holder-of-key SAML 2.0 assertion covered by the signature and named from its KeyInfo")]
    [DataRow("M_saml11_hok", "a holder-of-key SAML 1.1 assertion covered by the signature and named from its KeyInfo")]
    [DataRow("N_sym_cert", "a symmetric binding, certificate credential, HMAC-SHA256 under a key Java derived from the unwrapped EncryptedKey")]
    [DataRow("O_sym_user", "a symmetric binding, username credential, the token decrypted by Java before its digest is checked")]
    [DataRow("P_sym_cert_dec2005", "a symmetric binding, certificate credential, WS-SecureConversation December 2005 derived key tokens")]
    [DataRow("Q_sym_encsig", "a symmetric binding whose primary signature Java decrypts before validating it")]
    public void JavaVerifier_AcceptsEnvelopeSignedWithExclusiveC14N_Test(string caseName, string shape)
    {
        var run = Verified();
        var result = Case(run, caseName);

        JavaInteropAssert.AssertValid(
            result.CoreValidity,
            caseName,
            $"{caseName} | {shape} | {result}");

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
        }
    }

    [DataTestMethod]
    [DataRow("C_inclusive", "mustUnderstand set")]
    [DataRow("H_incl_nomu", "mustUnderstand cleared, isolating the canonicalization from the header attribute")]
    public void JavaVerifier_RejectsEnvelopeSignedWithInclusiveC14N_Test(string caseName, string variant)
    {
        var run = Verified();
        var result = Case(run, caseName);

        Assert.AreEqual(
            WsSecurityTestSupport.InclusiveC14N,
            result.CanonicalizationMethod, $"{caseName} | {JavaInteropVerdict.Or(result.CanonicalizationMethod)} | {result}");

        JavaInteropAssert.AssertInvalid(
            result.CoreValidity,
            caseName,
            $"{caseName} | {variant} | {result}");

        JavaInteropAssert.AssertInvalid(
            result.SignatureValueValidity,
            caseName,
            $"{caseName} | {result}");
    }

    [TestMethod]
    public void JavaVerifier_WhenTheSignedBodyIsTampered_ReportsTheBodyDigestInvalidWhileSignedInfoStaysValid_Test()
    {
        var run = Verified();
        var result = Case(run, JavaInteropSupport.TamperedCase);

        var bodyUri = run.BodyReferenceUris.TryGetValue(JavaInteropSupport.TamperedCase, out var uri) ? uri : null;

        Assert.IsNotNull(bodyUri, $"{result}");

        var body = result.Reference(bodyUri);

        Assert.IsNotNull(body, $"{bodyUri} | {string.Join(", ", result.References.Select(reference => reference.Uri))} | {result}");

        JavaInteropAssert.AssertInvalid(
            body.DigestValid,
            $"{bodyUri}",
            $"{body}");

        Assert.IsFalse(
            string.IsNullOrEmpty(body.Dereferenced) || body.Dereferenced.Contains("NULL"),
            $"{JavaInteropVerdict.Or(body.Dereferenced)} | {body}");

        Assert.IsFalse(
            string.IsNullOrEmpty(body.CalculatedDigest) || body.CalculatedDigest == "<null>",
            $"{JavaInteropVerdict.Or(body.CalculatedDigest)} | {body}");

        Assert.AreNotEqual(body.ExpectedDigest, body.CalculatedDigest, $"{JavaInteropVerdict.Or(body.ExpectedDigest)} | {body}");

        JavaInteropAssert.AssertValid(
            result.SignatureValueValidity,
            "",
            $"{result}");

        JavaInteropAssert.AssertInvalid(
            result.CoreValidity,
            "CORE VALIDITY",
            $"{result}");
    }

    [TestMethod]
    public void JavaVerifier_RegistersWsuIdAttributesSoEveryReferenceResolves_Test()
    {
        var run = Verified();
        var result = Case(run, JavaInteropSupport.DefaultsCase);

        Assert.IsTrue(result.RegisteredWsuIdCount >= 2, $"{JavaInteropSupport.DefaultsCase} | {result.RegisteredWsuIdCount} | {result}");

        foreach (var reference in result.References)
        {
            Assert.IsTrue(reference.Resolved, $"{reference.Uri} | {JavaInteropVerdict.Or(reference.Dereferenced)} | {reference}");
        }
    }

    [TestMethod]
    public void JavaVerifier_ReportsEveryVerdictAsOneOfValidInvalidOrError_Test()
    {
        var run = Verified();

        foreach (var result in run.Cases.Values)
        {
            Assert.IsFalse(result.CoreValidity.IsUnreported, $"{result}");

            Assert.IsFalse(result.SignatureValueValidity.IsUnreported, $"{result}");

            foreach (var reference in result.References)
            {
                Assert.IsFalse(reference.DigestValid.IsUnreported, $"{reference} | {result}");
            }
        }
    }

    [DataTestMethod]
    [DataRow("1", true)]
    [DataRow("true", true)]
    [DataRow("yes", true)]
    [DataRow("TRUE", true)]
    [DataRow(" Yes ", true)]
    [DataRow("0", false)]
    [DataRow("false", false)]
    [DataRow("on", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void AllowEnvironmentSkip_TurnsOnOnlyForAffirmativeSpellings_Test(string value, bool expected)
    {
        Assert.AreEqual(expected, EnvironmentFlag.IsAffirmative(value), $"{EnvironmentFlag.AllowEnvironmentSkipVariable} | {JavaInteropVerdict.Or(value)}");
    }

    private static JavaInteropRun Verified()
    {
        var run = JavaInteropSupport.Run();

        if (run.SkipReason is not null)
        {
            if (EnvironmentFlag.AllowsEnvironmentSkip())
            {
                Assert.Inconclusive(
                    $"{run.SkipReason} Skipped, not run: {EnvironmentFlag.AllowEnvironmentSkipVariable} is " +
                    "set, so the missing JDK was knowingly downgraded from a failure to a skip on this machine. " +
                    "None of the Java cross-stack signature assertions executed.");
            }

            Assert.Fail(
                $"{run.SkipReason} This run FAILED rather than being skipped, so that a machine without a JDK " +
                "cannot report a green build in which the Java cross-stack signature assertions never ran. Set " +
                $"{EnvironmentFlag.AllowEnvironmentSkipVariable}=1 (or true, or yes) to knowingly downgrade " +
                "this to a skip on a machine that cannot host a JDK.");
        }

        Assert.IsNull(run.FailureReason, $"{run.FailureReason} | {run.Output}");

        return run;
    }

    private static JavaInteropCaseResult Case(JavaInteropRun run, string caseName)
    {
        Assert.IsTrue(run.Cases.ContainsKey(caseName), $"{caseName} | {string.Join(", ", run.Cases.Keys)} | {run.Output}");

        var result = run.Cases[caseName];

        Assert.IsNull(result.HarnessError, $"{caseName} | {result.HarnessError} | {result}");

        return result;
    }

}
