#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal static class JavaInteropOutputParser
{

    private const string CaseLabel = "CASE ";

    private const string RegisteredIdsLabel = "wsu:Id attributes registered as XML IDs";

    private const string CanonicalizationLabel = "CanonicalizationMethod";

    private const string SignatureMethodLabel = "SignatureMethod";

    private const string CoreValidityLabel = "CORE VALIDITY";

    private const string SignatureValueLabel = "SignedInfo/SignatureValue validity";

    private const string HarnessErrorLabel = "HARNESS ERROR";

    private const string ReferenceLabel = "Reference URI=";

    private const string DigestMethodLabel = "digestMethod";

    private const string DigestValidLabel = "DIGEST VALID";

    private const string ExpectedLabel = "expected(msg)";

    private const string CalculatedLabel = "calculated";

    private const string DereferencedLabel = "dereferenced";

    private const string ModeLabel = "MODE";

    private const string JdkLabel = "JDK";

    private const string KeyWrapLabel = "EncryptedKey/EncryptionMethod";

    private const string KeyUnwrapLabel = "KEY UNWRAP";

    private const string DerivedKeyLabel = "DERIVED KEY";

    private const string DecryptionLabel = "DECRYPTION Id=";

    private const string UsernameTokenIdLabel = "USERNAME TOKEN Id";

    private const string UsernameTokenPasswordLabel = "USERNAME TOKEN PASSWORD";

    private const string UsernameTokenLabel = "USERNAME TOKEN";

    private const string UsernameLabel = "USERNAME";

    private const string PrimarySignatureLabel = "PRIMARY SIGNATURE";

    private const string HmacOutputLengthLabel = "HMACOutputLength";

    private const string EndorsingSignatureMethodLabel = "ENDORSING SignatureMethod";

    private const string EndorsingCoreValidityLabel = "ENDORSING CORE VALIDITY";

    private const string EndorsingSignatureValueLabel = "ENDORSING SignedInfo/SignatureValue validity";

    private const string EndorsingReferenceUriLabel = "ENDORSING REFERENCE URI";

    private const string EndorsingDigestValidLabel = "ENDORSING DIGEST VALID";

    private const string EndorsingTargetLabel = "ENDORSING TARGET";

    internal static IReadOnlyDictionary<string, JavaInteropCaseResult> Parse(string output)
    {
        var cases = new Dictionary<string, JavaInteropCaseResult>(StringComparer.Ordinal);

        JavaInteropCaseResult current = null;
        JavaInteropReferenceResult reference = null;

        foreach (var rawLine in (output ?? string.Empty).Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.StartsWith(CaseLabel, StringComparison.Ordinal))
            {
                var name = Path.GetFileNameWithoutExtension(line.Substring(CaseLabel.Length).Trim());

                current = new JavaInteropCaseResult(name);
                cases[name] = current;
                reference = null;

                continue;
            }

            if (current is null)
                continue;

            if (line.StartsWith(ReferenceLabel, StringComparison.Ordinal))
            {
                reference = new JavaInteropReferenceResult(line.Substring(ReferenceLabel.Length).Trim());
                current.References.Add(reference);

                continue;
            }

            if (TrySymmetric(line, current))
                continue;

            if (TryValue(line, HarnessErrorLabel, out var harnessError))
                current.HarnessError = harnessError;
            else if (TryValue(line, RegisteredIdsLabel, out var registered))
                current.RegisteredWsuIdCount = ParseCount(registered);
            else if (TryValue(line, CanonicalizationLabel, out var canonicalization))
                current.CanonicalizationMethod = canonicalization;
            else if (TryValue(line, SignatureValueLabel, out var signatureValue))
                current.SignatureValueValidity = JavaInteropVerdict.Parse(signatureValue);
            else if (TryValue(line, SignatureMethodLabel, out var signatureMethod))
                current.SignatureMethod = signatureMethod;
            else if (TryValue(line, CoreValidityLabel, out var coreValidity))
                current.CoreValidity = JavaInteropVerdict.Parse(coreValidity);
            else if (reference is null)
                continue;
            else if (TryValue(line, DigestMethodLabel, out var digestMethod))
                reference.DigestMethod = digestMethod;
            else if (TryValue(line, DigestValidLabel, out var digestValid))
                reference.DigestValid = JavaInteropVerdict.Parse(digestValid);
            else if (TryValue(line, ExpectedLabel, out var expected))
                reference.ExpectedDigest = expected;
            else if (TryValue(line, CalculatedLabel, out var calculated))
                reference.CalculatedDigest = calculated;
            else if (TryValue(line, DereferencedLabel, out var dereferenced))
                reference.Dereferenced = dereferenced;
        }

        return cases;
    }

    private static bool TrySymmetric(string line, JavaInteropCaseResult current)
    {
        if (TryValue(line, ModeLabel, out var mode))
            current.Mode = mode;
        else if (TryValue(line, JdkLabel, out var jdk))
            current.JavaRuntime = jdk;
        else if (TryValue(line, KeyWrapLabel, out var keyWrap))
            current.KeyWrapAlgorithm = keyWrap;
        else if (TryValue(line, KeyUnwrapLabel, out var keyUnwrap))
            current.KeyUnwrap = JavaInteropVerdict.Parse(keyUnwrap);
        else if (TryValue(line, DerivedKeyLabel, out var derivedKey))
            current.DerivedKeys.Add(JavaInteropDerivedKeyResult.Parse(derivedKey));
        else if (TryValue(line, DecryptionLabel, out var decryption))
            current.Decryptions.Add(JavaInteropDecryptionResult.Parse(decryption));
        else if (TryValue(line, UsernameTokenIdLabel, out var usernameTokenId))
            current.UsernameTokenId = usernameTokenId;
        else if (line.StartsWith(UsernameTokenPasswordLabel, StringComparison.Ordinal))
            return true;
        else if (TryValue(line, UsernameTokenLabel, out var usernameToken))
            current.UsernameToken = JavaInteropVerdict.Parse(usernameToken);
        else if (TryValue(line, UsernameLabel, out var username))
            current.Username = username;
        else if (TryValue(line, PrimarySignatureLabel, out var primarySignature))
            current.PrimarySignature = primarySignature;
        else if (TryValue(line, HmacOutputLengthLabel, out var hmacOutputLength))
            current.HmacOutputLength = hmacOutputLength;
        else if (TryValue(line, EndorsingSignatureMethodLabel, out var endorsingMethod))
            current.EndorsingSignatureMethod = endorsingMethod;
        else if (TryValue(line, EndorsingCoreValidityLabel, out var endorsingCore))
            current.EndorsingCoreValidity = JavaInteropVerdict.Parse(endorsingCore);
        else if (TryValue(line, EndorsingSignatureValueLabel, out var endorsingSignatureValue))
            current.EndorsingSignatureValueValidity = JavaInteropVerdict.Parse(endorsingSignatureValue);
        else if (TryValue(line, EndorsingReferenceUriLabel, out var endorsingReference))
            current.EndorsingReferenceUri = endorsingReference;
        else if (TryValue(line, EndorsingDigestValidLabel, out var endorsingDigest))
            current.EndorsingDigestValid = JavaInteropVerdict.Parse(endorsingDigest);
        else if (TryValue(line, EndorsingTargetLabel, out var endorsingTarget))
            current.EndorsingTarget = endorsingTarget;
        else
            return false;

        return true;
    }

    private static bool TryValue(string line, string label, out string value)
    {
        value = null;

        if (!line.StartsWith(label, StringComparison.Ordinal))
            return false;

        value = line.Substring(label.Length).TrimStart(' ', ':').Trim();

        return true;
    }

    private static int ParseCount(string value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? count : -1;
}
