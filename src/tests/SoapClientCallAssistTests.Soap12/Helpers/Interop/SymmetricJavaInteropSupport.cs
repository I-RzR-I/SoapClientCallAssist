#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal static class SymmetricJavaInteropSupport
{

    internal const string CertificateCase = "N_sym_cert";

    internal const string UserNameCase = "O_sym_user";

    internal const string CertificateDecember2005Case = "P_sym_cert_dec2005";

    internal const string EncryptedSignatureCase = "Q_sym_encsig";

    internal const string TamperedBodyCase = "R_sym_tampered_body";

    internal const string WrongSecretCase = "S_sym_wrong_secret";

    internal const string SwappedNonceCase = "T_sym_swapped_nonce";

    internal const string WrongKeystoreCase = "U_sym_wrong_p12";

    internal const string SymmetricMode = "symmetric";

    private const string ServiceKeystoreName = "service.p12";

    private const string OtherKeystoreName = "other.p12";

    private const string KeystorePattern = "*.p12";

    private const string SidecarSuffix = ".symmetric.properties";

    private const string TamperTarget = ">s1<";

    private const string TamperReplacement = ">s2<";

    private const int SecretLength = 32;

    private const int NonceLength = 16;

    private static readonly Type Derivation =
        WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.WsSecurity.WsSecurityKeyDerivation");

    internal static IReadOnlyDictionary<string, SymmetricJavaInteropExpectation> Emit(string directory)
    {
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        File.WriteAllBytes(
            Path.Combine(directory, ServiceKeystoreName),
            WsSecurityFoundationTestSupport.ServiceCertificate.Export(X509ContentType.Pfx, password));

        File.WriteAllBytes(
            Path.Combine(directory, OtherKeystoreName),
            WsSecurityTestSupport.OtherCertificate.Export(X509ContentType.Pfx, password));

        var certificateWire = Wire(SymmetricTestSupport.CertificateSecurity());
        var userNameWire = Wire(SymmetricTestSupport.UserNameSecurity());
        var decemberWire = Wire(SymmetricTestSupport.CertificateSecurity(security =>
            security.SymmetricBinding.Version = SoapSecureConversationVersionType.December2005));
        var encryptedSignatureWire = Wire(SymmetricTestSupport.UserNameSecurity(security =>
            security.Encryption = new SoapEncryptionDto { EncryptSignature = true }));

        var expectations = new Dictionary<string, SymmetricJavaInteropExpectation>(StringComparer.Ordinal);

        Add(expectations, directory, password, CertificateCase, certificateWire, certificateWire, ServiceKeystoreName, null);
        Add(expectations, directory, password, UserNameCase, userNameWire, userNameWire, ServiceKeystoreName, WsSecurityFoundationTestSupport.Username);
        Add(expectations, directory, password, CertificateDecember2005Case, decemberWire, decemberWire, ServiceKeystoreName, null);
        Add(expectations, directory, password, EncryptedSignatureCase, encryptedSignatureWire, encryptedSignatureWire, ServiceKeystoreName, WsSecurityFoundationTestSupport.Username);
        Add(expectations, directory, password, TamperedBodyCase, ReplaceOnce(certificateWire, TamperTarget, TamperReplacement), certificateWire, ServiceKeystoreName, null);
        Add(expectations, directory, password, WrongSecretCase, SubstituteSecret(certificateWire), certificateWire, ServiceKeystoreName, null);
        Add(expectations, directory, password, SwappedNonceCase, SwapNonce(certificateWire), certificateWire, ServiceKeystoreName, null);
        Add(expectations, directory, password, WrongKeystoreCase, certificateWire, certificateWire, OtherKeystoreName, null);

        return expectations;
    }

    internal static void Scrub(string directory)
    {
        if (directory is null || !Directory.Exists(directory))
            return;

        foreach (var file in Directory.EnumerateFiles(directory, KeystorePattern).Concat(Directory.EnumerateFiles(directory, "*" + SidecarSuffix)).ToList())
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    internal static JavaInteropRun Verified()
    {
        var run = JavaInteropSupport.Run();

        if (run.SkipReason is not null)
        {
            if (EnvironmentFlag.AllowsEnvironmentSkip())
            {
                Assert.Inconclusive(
                    $"{run.SkipReason} Skipped, not run: {EnvironmentFlag.AllowEnvironmentSkipVariable} is set, so " +
                    "the missing JDK was knowingly downgraded from a failure to a skip on this machine. None of the " +
                    "symmetric Java cross-stack assertions executed.");
            }

            Assert.Fail(
                $"{run.SkipReason} This run FAILED rather than being skipped, so that a machine without a JDK cannot " +
                "report a green build in which the symmetric Java cross-stack assertions never ran. Set " +
                $"{EnvironmentFlag.AllowEnvironmentSkipVariable}=1 (or true, or yes) to knowingly downgrade this " +
                "to a skip on a machine that cannot host a JDK.");
        }

        Assert.IsNull(run.FailureReason, $"{run.FailureReason} | {run.Output}");

        var limitation = run.Toolchain?.SymmetricLimitation;

        if (limitation is null)
            return run;

        if (EnvironmentFlag.AllowsEnvironmentSkip())
        {
            Assert.Inconclusive(
                $"{limitation} Skipped, not run: {EnvironmentFlag.AllowEnvironmentSkipVariable} is set, so the " +
                "too-old JDK was knowingly downgraded from a failure to a skip on this machine. No symmetric verdict " +
                "was trusted.");
        }

        Assert.Fail(
            $"{limitation} This run FAILED rather than being skipped, so that a machine whose only JDK predates " +
            $"{JavaInteropToolchain.SymmetricMinimumMajorVersion} cannot report a green build in which the symmetric " +
            "HMAC verdicts were never independently checked.");

        return run;
    }

    internal static JavaInteropCaseResult Case(JavaInteropRun run, string caseName)
    {
        Assert.IsTrue(run.Cases.ContainsKey(caseName), $"{caseName} | {string.Join(", ", run.Cases.Keys)} | {run.Output}");

        var result = run.Cases[caseName];

        Assert.IsNull(result.HarnessError, $"{caseName} | {result.HarnessError} | {result}");

        Assert.AreEqual(SymmetricMode, result.Mode?.Split(' ')[0], $"{caseName} | {result}");

        return result;
    }

    internal static SymmetricJavaInteropExpectation Expectation(JavaInteropRun run, string caseName)
    {
        Assert.IsTrue(run.Symmetric.ContainsKey(caseName), $"{caseName} | {string.Join(", ", run.Symmetric.Keys)}");

        return run.Symmetric[caseName];
    }

    private static void Add(
        IDictionary<string, SymmetricJavaInteropExpectation> expectations,
        string directory,
        string password,
        string name,
        string wire,
        string originalWire,
        string keystore,
        string expectedUsername)
    {
        File.WriteAllText(Path.Combine(directory, name + ".xml"), wire, new UTF8Encoding(false));

        var sidecar = new StringBuilder()
            .Append("keystore=").Append(keystore).Append('\n')
            .Append("password=").Append(password).Append('\n');

        if (expectedUsername is not null)
            sidecar.Append("username=").Append(expectedUsername).Append('\n');

        File.WriteAllText(Path.Combine(directory, name + SidecarSuffix), sidecar.ToString(), new UTF8Encoding(false));

        expectations[name] = Expectation(name, wire, originalWire);
    }

    private static SymmetricJavaInteropExpectation Expectation(string name, string wireText, string originalWireText)
    {
        var wire = SymmetricTestSupport.Parse(wireText);
        var original = ReferenceEquals(wireText, originalWireText) ? wire : SymmetricTestSupport.Parse(originalWireText);

        var body = wire.Document.DocumentElement.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Single(element => element.LocalName == "Body");

        return new SymmetricJavaInteropExpectation(
            name,
            "#" + body.GetAttribute("Id", WsSecurityTestSupport.WsuNamespace),
            WsuId(wire.SignatureToken),
            wire.EncryptionToken is null ? null : WsuId(wire.EncryptionToken),
            Convert.ToBase64String(LibraryDerivedKey(wire, wire.SignatureToken)),
            wire.EncryptionToken is null ? null : Convert.ToBase64String(LibraryDerivedKey(wire, wire.EncryptionToken)),
            Convert.ToBase64String(wire.SignatureKey),
            Convert.ToBase64String(LibraryDerivedKey(original, original.SignatureToken)),
            wire.PrimarySignature is null ? null : wire.PrimarySignature.GetAttribute("Id"),
            UsernameTokenReferenceUri(wire),
            wire.EncryptedDatas.Count);
    }

    private static string UsernameTokenReferenceUri(SymmetricWire wire)
    {
        if (wire.EncryptionKey is null)
            return null;

        var token = wire.EncryptedDatas
            .Select(encryptedData => SymmetricTestSupport.DecryptElement(encryptedData, wire.EncryptionKey))
            .FirstOrDefault(element => element.LocalName == "UsernameToken");

        return token is null ? null : "#" + WsuId(token);
    }

    private static byte[] LibraryDerivedKey(SymmetricWire wire, XmlElement token)
    {
        var key = (byte[])Derivation
            .GetMethod("DeriveKey", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { wire.Secret, wire.LabelOf(token), wire.NonceOf(token), wire.OffsetOf(token), wire.LengthOf(token) });

        Assert.IsNotNull(key);

        return key;
    }

    private static string WsuId(XmlElement element) => element.GetAttribute("Id", WsSecurityTestSupport.WsuNamespace);

    private static string Wire(SoapSecurityDto security)
        => SymmetricTestSupport.Wire(SymmetricTestSupport.BuildRequest(security, WsSecurityTestSupport.DefaultBody()));

    private static string SubstituteSecret(string wire)
    {
        var parsed = SymmetricTestSupport.Parse(wire);

        using var publicKey = WsSecurityFoundationTestSupport.ServiceCertificate.GetRSAPublicKey();

        var otherWrapped = publicKey.Encrypt(RandomNumberGenerator.GetBytes(SecretLength), RSAEncryptionPadding.OaepSHA1);

        return ReplaceOnce(wire, Convert.ToBase64String(parsed.Wrapped), Convert.ToBase64String(otherWrapped));
    }

    private static string SwapNonce(string wire)
    {
        var parsed = SymmetricTestSupport.Parse(wire);

        return ReplaceOnce(
            wire,
            Convert.ToBase64String(parsed.NonceOf(parsed.SignatureToken)),
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(NonceLength)));
    }

    private static string ReplaceOnce(string wire, string target, string replacement)
    {
        var first = wire.IndexOf(target, StringComparison.Ordinal);

        if (first < 0 || first != wire.LastIndexOf(target, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"A negative control needs '{target}' to occur exactly once in the emitted wire so that one edit " +
                "changes one thing, but it occurred " + (first < 0 ? "never" : "more than once") + ".");
        }

        return wire.Substring(0, first) + replacement + wire.Substring(first + target.Length);
    }
}
