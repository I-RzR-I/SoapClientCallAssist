#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal sealed class JavaInteropToolchain
{

    internal const string JavaHomeVariable = "JAVA_HOME";

    internal const string OverrideVariable = "SOAPCLIENTCALLASSIST_TESTS_JAVA_HOME";

    internal const int SymmetricMinimumMajorVersion = 11;

    internal const int UnknownMajorVersion = -1;

    private const string ReleaseFileName = "release";

    private const string ReleaseVersionKey = "JAVA_VERSION=";

    private const int VersionProbeTimeoutMilliseconds = 30000;

    private static readonly Regex QuotedVersion = new("version \"([^\"]+)\"", RegexOptions.Compiled);

    private JavaInteropToolchain(
        string javaPath,
        string javacPath,
        int majorVersion,
        string source,
        string selectionReport,
        string missingReason)
    {
        JavaPath = javaPath;
        JavacPath = javacPath;
        MajorVersion = majorVersion;
        Source = source;
        SelectionReport = selectionReport;
        MissingReason = missingReason;
    }

    internal string JavaPath { get; }

    internal string JavacPath { get; }

    internal int MajorVersion { get; }

    internal string Source { get; }

    internal string SelectionReport { get; }

    internal string MissingReason { get; }

    internal bool IsAvailable => MissingReason is null;

    internal string SymmetricLimitation
        => !IsAvailable || MajorVersion >= SymmetricMinimumMajorVersion
            ? null
            : $"The JDK the harness selected ('{JavaPath}', from {Source}) reports major version " +
              $"{DescribeMajor(MajorVersion)}, below {SymmetricMinimumMajorVersion}. Validating an HMAC ds:Signature " +
              "through DOMValidateContext with a SecretKeySpec is only relied upon from JDK 11, where the " +
              "hmac-sha256 SignatureMethod is part of the public JSR-105 contract, so the symmetric WS-Security " +
              "cross-stack verdicts are not trusted from this runtime. Put a JDK 11 or newer on PATH, or point " +
              $"{OverrideVariable} at one. {SelectionReport}";

    internal static JavaInteropToolchain Resolve()
    {
        var javaHome = Environment.GetEnvironmentVariable(JavaHomeVariable);
        var overrideHome = Environment.GetEnvironmentVariable(OverrideVariable);

        var candidates = new List<(string Java, string Javac, int Major, string Source)>();
        var probed = new List<string>();

        foreach (var (directory, source) in ProbeDirectories(javaHome, overrideHome))
        {
            var java = Executable(directory, "java");
            var javac = Executable(directory, "javac");

            if (java is null || javac is null)
                continue;

            var major = MajorVersionOf(directory, java);

            candidates.Add((java, javac, major, source));
            probed.Add($"{source} -> '{java}' (major {DescribeMajor(major)})");
        }

        if (candidates.Count == 0)
        {
            var reason =
                "No JDK was found, so the Java cross-stack interop verifier could not be run. " +
                $"{OverrideVariable} was {Describe(overrideHome)}, {JavaHomeVariable} was {Describe(javaHome)}, " +
                "and no PATH entry held both a 'java' and a 'javac' executable. Install a JDK and put it on PATH, " +
                $"or set {JavaHomeVariable} to one, to run this test.";

            return new JavaInteropToolchain(null, null, UnknownMajorVersion, null, null, reason);
        }

        var chosenIndex = candidates.FindIndex(candidate => candidate.Major >= SymmetricMinimumMajorVersion);

        string why;

        if (chosenIndex < 0)
        {
            chosenIndex = 0;
            why = $"no probed JDK reached major {SymmetricMinimumMajorVersion}, so the first usable one in precedence " +
                  "order was kept and the symmetric HMAC verdicts are reported as a toolchain limitation";
        }
        else if (chosenIndex == 0)
        {
            why = $"it is the first usable JDK in precedence order and already meets major {SymmetricMinimumMajorVersion}";
        }
        else
        {
            var skipped = candidates.Take(chosenIndex).Select(candidate => $"{candidate.Source} (major {DescribeMajor(candidate.Major)})");

            why = $"it is the first JDK at major {SymmetricMinimumMajorVersion} or newer, chosen ahead of " +
                  $"{string.Join(", ", skipped)} because HMAC validation through DOMValidateContext(SecretKeySpec) " +
                  $"is only relied upon from JDK {SymmetricMinimumMajorVersion}";
        }

        var chosen = candidates[chosenIndex];

        var report =
            $"JDK selection: '{chosen.Java}' (major {DescribeMajor(chosen.Major)}, from {chosen.Source}) because {why}. " +
            $"Precedence is {OverrideVariable}, then {JavaHomeVariable}, then PATH. Probed: {string.Join("; ", probed)}.";

        return new JavaInteropToolchain(chosen.Java, chosen.Javac, chosen.Major, chosen.Source, report, null);
    }

    internal static int ParseMajorVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return UnknownMajorVersion;

        var trimmed = version.Trim();

        if (trimmed.StartsWith("1.", StringComparison.Ordinal))
            trimmed = trimmed.Substring(2);

        var digits = new string(trimmed.TakeWhile(char.IsDigit).ToArray());

        return int.TryParse(digits, out var major) && major > 0 ? major : UnknownMajorVersion;
    }

    private static IEnumerable<(string Directory, string Source)> ProbeDirectories(string javaHome, string overrideHome)
    {
        if (!string.IsNullOrWhiteSpace(overrideHome))
            yield return (Path.Combine(overrideHome.Trim().Trim('"'), "bin"), OverrideVariable);

        if (!string.IsNullOrWhiteSpace(javaHome))
            yield return (Path.Combine(javaHome.Trim().Trim('"'), "bin"), JavaHomeVariable);

        var path = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrWhiteSpace(path))
            yield break;

        foreach (var entry in path.Split(Path.PathSeparator))
        {
            var candidate = entry.Trim().Trim('"');

            if (candidate.Length > 0)
                yield return (candidate, "PATH");
        }
    }

    private static string Executable(string directory, string name)
    {
        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? name + ".exe" : name;

        try
        {
            var full = Path.Combine(directory, fileName);

            return File.Exists(full) ? full : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static int MajorVersionOf(string binDirectory, string javaPath)
    {
        var fromRelease = MajorVersionFromReleaseFile(binDirectory);

        return fromRelease != UnknownMajorVersion ? fromRelease : MajorVersionFromJavaVersion(javaPath);
    }

    private static int MajorVersionFromReleaseFile(string binDirectory)
    {
        try
        {
            var release = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(binDirectory)) ?? binDirectory, ReleaseFileName);

            if (!File.Exists(release))
                return UnknownMajorVersion;

            var line = File.ReadLines(release).FirstOrDefault(candidate => candidate.StartsWith(ReleaseVersionKey, StringComparison.Ordinal));

            return line is null ? UnknownMajorVersion : ParseMajorVersion(line.Substring(ReleaseVersionKey.Length).Trim().Trim('"'));
        }
        catch (IOException)
        {
            return UnknownMajorVersion;
        }
        catch (UnauthorizedAccessException)
        {
            return UnknownMajorVersion;
        }
    }

    private static int MajorVersionFromJavaVersion(string javaPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo(javaPath, "-version")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);

            if (process is null)
                return UnknownMajorVersion;

            var standardError = process.StandardError.ReadToEndAsync();
            var standardOutput = process.StandardOutput.ReadToEndAsync();

            if (!process.WaitForExit(VersionProbeTimeoutMilliseconds))
            {
                process.Kill(true);

                return UnknownMajorVersion;
            }

            var match = QuotedVersion.Match(standardError.GetAwaiter().GetResult() + standardOutput.GetAwaiter().GetResult());

            return match.Success ? ParseMajorVersion(match.Groups[1].Value) : UnknownMajorVersion;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return UnknownMajorVersion;
        }
    }

    private static string DescribeMajor(int major) => major == UnknownMajorVersion ? "unknown" : major.ToString();

    private static string Describe(string javaHome)
        => string.IsNullOrWhiteSpace(javaHome) ? "unset" : $"'{javaHome}'";
}
