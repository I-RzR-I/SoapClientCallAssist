#region U S I N G

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

#endregion

namespace SoapClientCallAssistTests.Infrastructure
{
    public static class IisExpressPathResolver
    {
        public const string ExecutableOverrideVariable = "SOAPCLIENTCALLASSIST_IISEXPRESS_EXE";

        public const string ConfigOverrideVariable = "SOAPCLIENTCALLASSIST_IISEXPRESS_CONFIG";

        public const string ForceGeneratedConfigVariable = "SOAPCLIENTCALLASSIST_IISEXPRESS_GENERATE_CONFIG";

        private const string ExecutableRelativePath = @"IIS Express\iisexpress.exe";

        private const string SiteContentRelativePath = @"src\tests\TestSoapServiceN45";

        private const string InstallerUrl = "https://www.microsoft.com/download/details.aspx?id=48264";

        private const string ServiceProjectFileName = "TestSoapServiceN45.csproj";

        private const string VisualStudioRelativePath = @"Microsoft Visual Studio\2022";

        private const string MsBuildRelativePath = @"MSBuild\Current\Bin\MSBuild.exe";

        private static readonly string[] VisualStudioEditions =
        {
            "Enterprise", "Professional", "Community", "BuildTools", "Preview"
        };

        private static readonly string[] AffirmativeFlagValues = { "1", "true", "yes" };

        private static readonly string ServiceAssemblyRelativePath = Path.Combine(
            SiteContentRelativePath, @"bin\TestSoapServiceN45.dll");

        private static readonly string RepositoryMarkerRelativePath = Path.Combine(
            SiteContentRelativePath, "ServiceAsmx.asmx");

        private static readonly string ConfigRelativePath = Path.Combine(
            SiteContentRelativePath, @".vs\TestSoapServiceN45\config\applicationhost.config");

        private static readonly Lazy<string> RepositoryRoot = new Lazy<string>(WalkForRepositoryRoot);

        public static string ResolveExecutable()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new TestServiceUnavailableException(
                    "The SOAP integration tests host their service in IIS Express, which exists only on Windows. " +
                    "Current platform: " + RuntimeInformation.OSDescription + ". " +
                    "Run this project on a Windows agent, or point " + ExecutableOverrideVariable +
                    " at an equivalent host if one is available.");

            var attempts = new List<string>();

            var overridePath = ReadEnvironmentPath(ExecutableOverrideVariable);
            if (overridePath == null)
                attempts.Add(ExecutableOverrideVariable + " (not set)");
            else if (File.Exists(overridePath))
                return overridePath;
            else
                attempts.Add(ExecutableOverrideVariable + " points at " + Quote(overridePath) + " (file does not exist)");

            foreach (var programFilesVariable in new[] { "ProgramFiles", "ProgramFiles(x86)" })
            {
                var programFiles = ReadEnvironmentPath(programFilesVariable);
                if (programFiles == null)
                {
                    attempts.Add("%" + programFilesVariable + "%\\" + ExecutableRelativePath +
                                 " (%" + programFilesVariable + "% is not set)");

                    continue;
                }

                var candidate = Path.Combine(programFiles, ExecutableRelativePath);
                if (File.Exists(candidate))
                    return candidate;

                attempts.Add(Quote(candidate) + " (file does not exist)");
            }

            throw new TestServiceUnavailableException(
                "IIS Express was not found, so the SOAP test service these tests call cannot be started." +
                Environment.NewLine + "Probed, in order:" + Environment.NewLine +
                FormatAttempts(attempts) + Environment.NewLine +
                "Install IIS Express (it ships with the Visual Studio web workload, or standalone from " +
                InstallerUrl + "), or set " + ExecutableOverrideVariable +
                " to the full path of iisexpress.exe.");
        }

        public static string ResolveApplicationHostConfig(IisExpressHostingProfile profile, string executablePath, Action<string> log)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (log == null)
                throw new ArgumentNullException(nameof(log));

            var attempts = new List<string>();

            var overridePath = ReadEnvironmentPath(ConfigOverrideVariable);
            if (overridePath == null)
                attempts.Add(ConfigOverrideVariable + " (not set)");
            else if (File.Exists(overridePath))
                return WarnAboutGap(overridePath, profile, log);
            else
                attempts.Add(ConfigOverrideVariable + " points at " + Quote(overridePath) + " (file does not exist)");

            var repositoryRoot = RepositoryRoot.Value;
            var checkoutConfig = repositoryRoot == null ? null : Path.Combine(repositoryRoot, ConfigRelativePath);
            var forceGenerated = ReadEnvironmentFlag(ForceGeneratedConfigVariable);

            if (checkoutConfig == null)
                attempts.Add("the Visual Studio configuration under " + Quote(ConfigRelativePath) +
                             " (no ancestor of " + Quote(AppContext.BaseDirectory) + " contains " +
                             Quote(RepositoryMarkerRelativePath) + ", so the repository root is unknown)");
            else if (forceGenerated)
                attempts.Add(Quote(checkoutConfig) + " (skipped: " + ForceGeneratedConfigVariable + " is set)");
            else if (!File.Exists(checkoutConfig))
                attempts.Add(Quote(checkoutConfig) + " (file does not exist)");
            else
            {
                var gap = IisExpressConfigGenerator.DescribeGap(checkoutConfig, profile);
                if (gap == null)
                    return checkoutConfig;

                attempts.Add(Quote(checkoutConfig) + " (skipped: " + gap + ")");

                log("Not using " + Quote(checkoutConfig) + " because " + gap +
                    "; generating a configuration that declares the transport bindings instead.");
            }

            var contentRoot = ResolveSiteContentRoot();
            var templatePath = contentRoot == null
                ? null
                : IisExpressConfigGenerator.ResolveTemplate(executablePath);

            if (templatePath != null)
                return IisExpressConfigGenerator.EnsureGenerated(templatePath, profile, log);

            attempts.AddRange(contentRoot == null
                ? new[] { "generating one from the IIS Express template (skipped: the content root of the " +
                          "in-repository test service is unknown, so no site could be declared)" }
                : IisExpressConfigGenerator.DescribeTemplateProbes(executablePath)
                    .Select(probe => "the IIS Express configuration template at " + probe)
                    .ToArray());

            throw new TestServiceUnavailableException(
                "No IIS Express hosting configuration for the SOAP test service could be found or generated." +
                Environment.NewLine + "Probed, in order:" + Environment.NewLine +
                FormatAttempts(attempts) + Environment.NewLine +
                "Install IIS Express (it ships with the Visual Studio web workload, or standalone from " +
                InstallerUrl + "), or set " + ConfigOverrideVariable +
                " to the full path of an applicationhost.config that declares the test site.");
        }

        private static string WarnAboutGap(string configPath, IisExpressHostingProfile profile, Action<string> log)
        {
            var gap = IisExpressConfigGenerator.DescribeGap(configPath, profile);
            if (gap != null)
                log("WARNING: " + ConfigOverrideVariable + " names " + Quote(configPath) + " and " + gap +
                    ", so the transport tests in this assembly will not be able to reach their endpoints.");

            return configPath;
        }

        public static void EnsureServiceAssemblyBuilt()
        {
            var repositoryRoot = RepositoryRoot.Value;

            if (repositoryRoot == null)
                return;

            var assemblyPath = Path.Combine(repositoryRoot, ServiceAssemblyRelativePath);
            if (File.Exists(assemblyPath))
                return;

            var projectPath = Path.Combine(repositoryRoot, SiteContentRelativePath, ServiceProjectFileName);
            var solutionDirectory = Path.Combine(repositoryRoot, SiteContentRelativePath) +
                                    Path.DirectorySeparatorChar;
            var msBuildPath = ResolveMsBuild();

            throw new InvalidOperationException(
                "The SOAP test service has not been compiled: " + Quote(assemblyPath) + " does not exist." +
                Environment.NewLine +
                "Its build output is excluded by .gitignore, so a fresh clone has to build it once. It is a " +
                ".NET Framework web project, so it needs MSBuild from a Visual Studio installation carrying " +
                "the web workload rather than the dotnet CLI. Run, in order, from a Developer Command " +
                "Prompt:" + Environment.NewLine +
                "  1. " + Quote(msBuildPath) + " " + Quote(projectPath) +
                " -t:Restore -p:RestorePackagesConfig=true -p:SolutionDir=" + Quote(solutionDirectory) +
                Environment.NewLine +
                "  2. " + Quote(msBuildPath) + " " + Quote(projectPath) +
                " -t:Build -p:Configuration=Debug -p:SolutionDir=" + Quote(solutionDirectory) +
                Environment.NewLine +
                "The SolutionDir argument is required: without it the restore fails with \"No solution found. " +
                "Restore against a solution or pass in /p:SolutionDir\".");
        }

        public static string ResolveSiteContentRoot()
        {
            var repositoryRoot = RepositoryRoot.Value;

            return repositoryRoot == null ? null : Path.Combine(repositoryRoot, SiteContentRelativePath);
        }

        public static string NormalizeForComparison(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                var expanded = Environment.ExpandEnvironmentVariables(path);

                return Path.GetFullPath(expanded)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException)
            {
                return null;
            }
        }

        public static string Quote(string value)
        {
            return "'" + value + "'";
        }

        private static string WalkForRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, RepositoryMarkerRelativePath)))
                    return directory.FullName;

                directory = directory.Parent;
            }

            return null;
        }

        public static bool ReadEnvironmentFlag(string name)
        {
            var value = ReadEnvironmentPath(name);

            return value != null && AffirmativeFlagValues.Contains(value, StringComparer.OrdinalIgnoreCase);
        }

        private static string ReadEnvironmentPath(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string ResolveMsBuild()
        {
            var candidates =
                from programFilesVariable in new[] { "ProgramFiles", "ProgramFiles(x86)" }
                let programFiles = ReadEnvironmentPath(programFilesVariable)
                where programFiles != null
                from edition in VisualStudioEditions
                select Path.Combine(programFiles, VisualStudioRelativePath, edition, MsBuildRelativePath);

            return candidates.FirstOrDefault(File.Exists) ?? "MSBuild.exe";
        }

        private static string FormatAttempts(IEnumerable<string> attempts)
        {
            return string.Join(
                Environment.NewLine,
                attempts.Select((attempt, index) => "  " + (index + 1) + ". " + attempt));
        }
    }
}
