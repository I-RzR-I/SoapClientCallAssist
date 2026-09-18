#nullable disable

using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal static class JavaInteropSupport
{

    internal const string DefaultsCase = "A_defaults";

    internal const string TamperedCase = "Z_tampered_body";

    internal const string Saml20HolderOfKeyCase = "L_saml20_hok";

    internal const string Saml11HolderOfKeyCase = "M_saml11_hok";

    private const string VerifierSourceName = "JavaVerify.java";

    private const string VerifierClassName = "JavaVerify";

    private const string TamperTarget = ">s1<";

    private const int ToolTimeoutMilliseconds = 180000;

    private static readonly XNamespace Service = WsSecurityTestSupport.Service;

    private static readonly XNamespace Extra = SoapAssert.ServiceNs + "extra";

    private static readonly XNamespace Wsu = WsSecurityTestSupport.WsuNamespace;

    private static readonly Lazy<JavaInteropRun> LazyRun =
        new(Execute, LazyThreadSafetyMode.ExecutionAndPublication);

    private static string _workDirectory;

    internal static JavaInteropRun Run() => LazyRun.Value;

    internal static void Cleanup()
    {
        var directory = Interlocked.Exchange(ref _workDirectory, null);

        if (directory is null || !Directory.Exists(directory))
            return;

        try
        {
            Directory.Delete(directory, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static JavaInteropRun Execute()
    {
        var toolchain = JavaInteropToolchain.Resolve();

        if (!toolchain.IsAvailable)
            return JavaInteropRun.Skipped(toolchain.MissingReason);

        var verifierSource = Path.Combine(AppContext.BaseDirectory, "Interop", VerifierSourceName);

        if (!File.Exists(verifierSource))
        {
            return JavaInteropRun.Failed(
                $"The Java verifier source was not found at '{verifierSource}'. The project file must copy " +
                "it to the test output directory. This is a repository defect, not a missing toolchain, so " +
                "it fails rather than skipping: a silent skip here would disable every cross-stack " +
                "interop assertion while the build stayed green.",
                null,
                toolchain);
        }

        var directory = Path.Combine(
            Path.GetTempPath(), "SoapClientCallAssist.JavaInterop", Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);
        Interlocked.Exchange(ref _workDirectory, directory);

        try
        {
            var bodyReferenceUris = Emit(directory);
            var symmetric = SymmetricJavaInteropSupport.Emit(directory);
            var localSource = Path.Combine(directory, VerifierSourceName);

            File.Copy(verifierSource, localSource, true);

            var compileExitCode = Execute(
                toolchain.JavacPath,
                $"-encoding UTF-8 -d \"{directory}\" \"{localSource}\"",
                directory,
                out var compileOutput);

            if (compileExitCode != 0)
            {
                return JavaInteropRun.Failed(
                    $"Compiling '{VerifierSourceName}' with '{toolchain.JavacPath}' failed with exit code " +
                    $"{compileExitCode}. A JDK was found and the source was found, so the remaining cause is " +
                    "a defect in the verifier source this repository owns. It fails rather than skipping.",
                    compileOutput,
                    toolchain);
            }

            var verifyExitCode = Execute(
                toolchain.JavaPath,
                $"-Dfile.encoding=UTF-8 -cp \"{directory}\" {VerifierClassName} \"{directory}\"",
                directory,
                out var verifyOutput);

            if (verifyExitCode != 0)
            {
                return JavaInteropRun.Failed(
                    $"The Java verifier '{toolchain.JavaPath}' compiled successfully but exited with code " +
                    $"{verifyExitCode}, so its verdict cannot be trusted.",
                    verifyOutput,
                    toolchain);
            }

            return JavaInteropRun.Completed(
                verifyOutput, JavaInteropOutputParser.Parse(verifyOutput), bodyReferenceUris, symmetric, toolchain);
        }
        finally
        {
            SymmetricJavaInteropSupport.Scrub(directory);
        }
    }

    private static IReadOnlyDictionary<string, string> Emit(string directory)
    {
        File.WriteAllBytes(
            Path.Combine(directory, "signer.cer"),
            WsSecurityTestSupport.SigningCertificate.Export(X509ContentType.Cert));

        var bodyReferenceUris = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var interopCase in Cases())
        {
            var envelope = WsSecurityTestSupport.WireBytes(
                WsSecurityTestSupport.BuildPost(
                    WsSecurityTestSupport.Security(interopCase.Tune), interopCase.Body()),
                $"Build {interopCase.Name}");

            File.WriteAllBytes(Path.Combine(directory, interopCase.Name + ".xml"), envelope);

            bodyReferenceUris[interopCase.Name] = BodyReferenceUri(envelope);

            if (interopCase.Name != DefaultsCase)
                continue;

            File.WriteAllBytes(Path.Combine(directory, TamperedCase + ".xml"), Tamper(envelope));

            bodyReferenceUris[TamperedCase] = bodyReferenceUris[DefaultsCase];
        }

        return bodyReferenceUris;
    }

    private static (string Name, Func<XElement> Body, Action<SoapSecurityDto> Tune)[] Cases()
        => new (string, Func<XElement>, Action<SoapSecurityDto>)[]
        {
            (DefaultsCase, WsSecurityTestSupport.DefaultBody, _ => { }),
            ("B_sha512", WsSecurityTestSupport.DefaultBody, security =>
            {
                security.SignatureAlgorithm = SoapSignatureAlgorithmType.RsaSha512;
                security.DigestAlgorithm = SoapDigestAlgorithmType.Sha512;
            }),
            ("C_inclusive", WsSecurityTestSupport.DefaultBody, security =>
                security.Canonicalization = SoapCanonicalizationType.InclusiveC14N),
            ("D_namespaces", NamespacedBody, _ => { }),
            ("D2_unusedns", UnusedNamespaceDeclarationBody, _ => { }),
            ("E_whitespace", WhitespaceBody, _ => { }),
            ("F_bodyonly", WsSecurityTestSupport.DefaultBody, security => security.IncludeTimestamp = false),
            ("G_unicode", UnicodeBody, _ => { }),
            ("H_incl_nomu", WsSecurityTestSupport.DefaultBody, security =>
            {
                security.Canonicalization = SoapCanonicalizationType.InclusiveC14N;
                security.MustUnderstand = false;
            }),
            ("I_actor", WsSecurityTestSupport.DefaultBody, security =>
                security.SecurityActor = "http://java-interop.invalid/role/gateway"),
            ("K_qname", QNameBody, _ => { }),
            (Saml20HolderOfKeyCase, WsSecurityTestSupport.DefaultBody, security =>
                security.SamlToken = SamlTestSupport.HolderOfKeyToken(WsSecurityTestSupport.SigningCertificate, saml20: true)),
            (Saml11HolderOfKeyCase, WsSecurityTestSupport.DefaultBody, security =>
                security.SamlToken = SamlTestSupport.HolderOfKeyToken(WsSecurityTestSupport.SigningCertificate, saml20: false))
        };

    private static XElement NamespacedBody()
        => new(Service + "Compute",
            new XElement(Service + "left",
                new XElement(Extra + "value", "10"),
                new XElement(Extra + "unit", "kg")),
            new XElement(Extra + "right",
                new XAttribute("mode", "strict"),
                new XElement(Service + "value", "20")));

    private static XElement UnusedNamespaceDeclarationBody()
        => new(Service + "Ping",
            new XAttribute(XNamespace.Xmlns + "unused", "urn:never-referenced"),
            new XAttribute(XNamespace.Xmlns + "alsounused", "urn:also-never-referenced"),
            new XElement(Service + "seq", "1"),
            new XElement(Service + "note", "ancestor declares two prefixes no descendant uses"));

    private static XElement WhitespaceBody()
        => new(Service + "Echo",
            new XElement(Service + "text", "  leading and trailing  "),
            new XElement(Service + "multiline", "line one\r\nline two\r\n\tindented\r\n"),
            new XElement(Service + "tabs", "\ta\tb\t"));

    private static XElement UnicodeBody()
        => new(Service + "Unicode",
            new XElement(Service + "romanian", "șțăâî ŞTA"),
            new XElement(Service + "euro", "12,50 €"),
            new XElement(Service + "cyrillic", "Привет мир"),
            new XElement(Service + "cjk", "日本語 中文"));

    private static XElement QNameBody()
        => new(Service + "Query",
            new XElement(Service + "type", "xsi:string"),
            new XElement(Service + "other", "i:decimal"));

    private static string BodyReferenceUri(byte[] envelope)
    {
        using var stream = new MemoryStream(envelope);

        var body = XDocument.Load(stream).Root?
            .Elements()
            .FirstOrDefault(element => element.Name.LocalName == "Body");

        var id = body?.Attribute(Wsu + "Id")?.Value;

        return string.IsNullOrEmpty(id) ? null : "#" + id;
    }

    private static byte[] Tamper(byte[] envelope)
    {
        var index = envelope.AsSpan().IndexOf(Encoding.ASCII.GetBytes(TamperTarget));

        if (index < 0)
        {
            throw new InvalidOperationException(
                $"The negative control needs the signed body to carry '{TamperTarget}' so a single character can " +
                "be flipped, but the emitted envelope did not contain it.");
        }

        var tampered = (byte[])envelope.Clone();

        tampered[index + 2] = (byte)'2';

        return tampered;
    }

    private static int Execute(string fileName, string arguments, string workingDirectory, out string output)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(startInfo);

        if (process is null)
            throw new InvalidOperationException($"Starting '{fileName}' produced no process to read a verdict from.");

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(ToolTimeoutMilliseconds))
        {
            process.Kill(true);

            output = $"'{fileName}' did not finish within {ToolTimeoutMilliseconds} ms and was killed.";

            return -1;
        }

        output = standardOutput.GetAwaiter().GetResult() + standardError.GetAwaiter().GetResult();

        return process.ExitCode;
    }
}
