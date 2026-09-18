#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Soap12.Helpers.Interop;
using System;
using System.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Interop;

[TestClass]
public sealed class SamlJavaInteropTests
{

    [DataTestMethod]
    [DataRow(JavaInteropSupport.Saml20HolderOfKeyCase, DisplayName = "SAML 2.0 holder-of-key")]
    [DataRow(JavaInteropSupport.Saml11HolderOfKeyCase, DisplayName = "SAML 1.1 holder-of-key")]
    public void JavaVerifier_ResolvesTheAssertionReferenceByItsOwnIdentifierAndAcceptsItsDigest_Test(string caseName)
    {
        var run = JavaInteropSupport.Run();

        if (run.SkipReason is not null)
        {
            if (EnvironmentFlag.AllowsEnvironmentSkip())
                Assert.Inconclusive(run.SkipReason);

            Assert.Fail(run.SkipReason);
        }

        Assert.IsNull(run.FailureReason, $"{run.FailureReason} | {run.Output}");
        Assert.IsTrue(run.Cases.ContainsKey(caseName), $"{caseName} | {string.Join(", ", run.Cases.Keys)}");

        var result = run.Cases[caseName];

        Assert.IsNull(result.HarnessError, $"{result}");
        Assert.AreEqual(3, result.References.Count, $"{result}");

        var bodyUri = run.BodyReferenceUris[caseName];

        var assertionReference = result.References.Single(reference => reference.Uri != bodyUri && !reference.Uri.StartsWith("#ts-", StringComparison.Ordinal));

        Assert.IsTrue(assertionReference.Resolved, $"{assertionReference}");

        Assert.IsTrue(assertionReference.DigestValid.IsValid, $"{assertionReference}");
        Assert.IsTrue(result.CoreValidity.IsValid, $"{result}");
        Assert.IsTrue(result.SignatureValueValidity.IsValid, $"{result}");
    }
}
