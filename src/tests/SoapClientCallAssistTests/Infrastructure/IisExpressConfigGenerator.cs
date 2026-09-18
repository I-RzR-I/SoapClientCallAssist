#region U S I N G

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssistTests.Infrastructure
{
    public static class IisExpressConfigGenerator
    {
        private const string GeneratedSiteName = IisExpressHostingProfile.SiteName;

        private const int GeneratedSiteId = 1;

        private const string ApplicationPoolName = "Clr4IntegratedAppPool";

        private const string HttpProtocol = "http";

        private const string HttpsProtocol = "https";

        private const string RootPath = "/";

        private const string LocationElement = "location";

        private const string WindowsAuthenticationElement = "windowsAuthentication";

        private const string AnonymousAuthenticationElement = "anonymousAuthentication";

        private const string RequireClientCertificateSslFlags = "Ssl,SslNegotiateCert,SslRequireCert";

        private const string TemplateRelativeToInstall =
            @"config\templates\PersonalWebServer\applicationhost.config";

        private const string TemplateRelativePath = @"IIS Express\" + TemplateRelativeToInstall;

        private const string GeneratedRelativeDirectory = @"SoapClientCallAssist\IisExpress";

        private const string GeneratedFileName = "applicationhost.config";

        private const string XmlDeclaration = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";

        private const int CheckoutKeyLength = 16;

        private static readonly string[] ProgramFilesVariables = { "ProgramFiles", "ProgramFiles(x86)" };

        public static string ResolveTemplate(string executablePath)
        {
            return EnumerateTemplateCandidates(executablePath)
                .Where(candidate => candidate.Path != null)
                .Select(candidate => candidate.Path)
                .FirstOrDefault(File.Exists);
        }

        public static IReadOnlyList<string> DescribeTemplateProbes(string executablePath)
        {
            return EnumerateTemplateCandidates(executablePath)
                .Select(candidate => candidate.Path == null
                    ? "%" + candidate.Origin + "%\\" + TemplateRelativePath +
                      " (%" + candidate.Origin + "% is not set)"
                    : IisExpressPathResolver.Quote(candidate.Path) +
                      (File.Exists(candidate.Path) ? " (exists)" : " (file does not exist)"))
                .ToList();
        }

        public static string EnsureGenerated(string templatePath, IisExpressHostingProfile profile, Action<string> log)
        {
            if (templatePath == null)
                throw new ArgumentNullException(nameof(templatePath));

            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (log == null)
                throw new ArgumentNullException(nameof(log));

            var contentRoot = profile.ContentRoot;
            var generatedPath = ResolveGeneratedPath(contentRoot);
            var desired = Render(BuildConfiguration(templatePath, profile));

            if (IsCurrent(generatedPath, desired))
            {
                log("Reusing the generated IIS Express configuration " +
                    IisExpressPathResolver.Quote(generatedPath) + ".");

                return generatedPath;
            }

            Write(generatedPath, desired);

            log("Generated the IIS Express configuration " + IisExpressPathResolver.Quote(generatedPath) +
                " from " + IisExpressPathResolver.Quote(templatePath) + ", declaring site '" +
                GeneratedSiteName + "' on http port " + profile.HttpPort + " and https port " +
                profile.HttpsPort + " serving " + IisExpressPathResolver.Quote(contentRoot) + " with " +
                IisExpressHostingProfile.WindowsAuthApplicationPath + " and " +
                IisExpressHostingProfile.ClientCertificateApplicationPath + " serving the same root.");

            return generatedPath;
        }

        public static string DescribeGap(string configPath, IisExpressHostingProfile profile)
        {
            if (configPath == null)
                throw new ArgumentNullException(nameof(configPath));

            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            XDocument document;

            try
            {
                document = XDocument.Load(configPath);
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is XmlException)
            {
                return "it could not be read: " + exception.Message;
            }

            var normalizedRoot = IisExpressPathResolver.NormalizeForComparison(profile.ContentRoot);

            var site = (document.Root?
                    .Element("system.applicationHost")?
                    .Element("sites")?
                    .Elements("site") ?? Enumerable.Empty<XElement>())
                .FirstOrDefault(candidate =>
                    BindsOn(candidate, HttpProtocol, profile.HttpPort) &&
                    string.Equals(RootPhysicalPath(candidate), normalizedRoot, StringComparison.OrdinalIgnoreCase));

            if (site == null)
                return "no site binds http on port " + profile.HttpPort + " for " +
                       IisExpressPathResolver.Quote(profile.ContentRoot);

            if (!BindsOn(site, HttpsProtocol, profile.HttpsPort))
                return "its site does not bind https on port " + profile.HttpsPort;

            if (!DeclaresApplication(site, IisExpressHostingProfile.WindowsAuthApplicationPath))
                return "its site declares no " + IisExpressHostingProfile.WindowsAuthApplicationPath + " application";

            if (!DeclaresApplication(site, IisExpressHostingProfile.ClientCertificateApplicationPath))
                return "its site declares no " + IisExpressHostingProfile.ClientCertificateApplicationPath + " application";

            var siteName = (string)site.Attribute("name") ?? string.Empty;

            if (!HasLocation(document, siteName + IisExpressHostingProfile.WindowsAuthApplicationPath))
                return "it has no <location> switching Windows authentication on for " +
                       IisExpressHostingProfile.WindowsAuthApplicationPath;

            if (!HasLocation(document, siteName + IisExpressHostingProfile.ClientCertificateApplicationPath))
                return "it has no <location> requiring a client certificate for " +
                       IisExpressHostingProfile.ClientCertificateApplicationPath;

            return null;
        }

        private static string ResolveGeneratedPath(string contentRoot)
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root))
                root = Path.GetTempPath();

            return Path.Combine(
                root, GeneratedRelativeDirectory, DeriveCheckoutKey(contentRoot), GeneratedFileName);
        }

        private static IEnumerable<(string Origin, string Path)> EnumerateTemplateCandidates(string executablePath)
        {
            var installDirectory = string.IsNullOrWhiteSpace(executablePath)
                ? null
                : Path.GetDirectoryName(executablePath);

            if (!string.IsNullOrEmpty(installDirectory))
                yield return (null, Path.Combine(installDirectory, TemplateRelativeToInstall));

            foreach (var variable in ProgramFilesVariables)
            {
                var programFiles = Environment.GetEnvironmentVariable(variable);

                yield return string.IsNullOrWhiteSpace(programFiles)
                    ? (variable, null)
                    : (variable, Path.Combine(programFiles.Trim(), TemplateRelativePath));
            }
        }

        private static XDocument BuildConfiguration(string templatePath, IisExpressHostingProfile profile)
        {
            XDocument document;

            try
            {
                document = XDocument.Load(templatePath);
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is XmlException)
            {
                throw new TestServiceUnavailableException(
                    "The IIS Express configuration template " + IisExpressPathResolver.Quote(templatePath) +
                    " could not be read, so a hosting configuration for this checkout cannot be generated: " +
                    exception.Message + Environment.NewLine + "Repair the IIS Express installation, or set " +
                    IisExpressPathResolver.ConfigOverrideVariable +
                    " to the full path of an applicationhost.config that declares the test site.");
            }

            var sites = document.Root?
                .Element("system.applicationHost")?
                .Element("sites");

            if (sites == null)
                throw new TestServiceUnavailableException(
                    "The IIS Express configuration template " + IisExpressPathResolver.Quote(templatePath) +
                    " declares no system.applicationHost/sites section, so it is not a usable template.");

            sites.Elements("site").Remove();
            sites.AddFirst(BuildSite(profile));

            document.Root.Add(BuildWindowsAuthenticationLocation());
            document.Root.Add(BuildClientCertificateLocation());

            return document;
        }

        private static XElement BuildSite(IisExpressHostingProfile profile)
        {
            return new XElement(
                "site",
                new XAttribute("name", GeneratedSiteName),
                new XAttribute("id", GeneratedSiteId.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("serverAutoStart", "true"),
                BuildApplication(RootPath, profile.ContentRoot),
                BuildApplication(IisExpressHostingProfile.WindowsAuthApplicationPath, profile.ContentRoot),
                BuildApplication(IisExpressHostingProfile.ClientCertificateApplicationPath, profile.ContentRoot),
                new XElement(
                    "bindings",
                    BuildBinding(HttpProtocol, profile.HttpPort),
                    BuildBinding(HttpsProtocol, profile.HttpsPort)));
        }

        private static XElement BuildApplication(string virtualPath, string physicalPath)
        {
            return new XElement(
                "application",
                new XAttribute("path", virtualPath),
                new XAttribute("applicationPool", ApplicationPoolName),
                new XElement(
                    "virtualDirectory",
                    new XAttribute("path", RootPath),
                    new XAttribute("physicalPath", physicalPath)));
        }

        private static XElement BuildBinding(string protocol, int port)
        {
            return new XElement(
                "binding",
                new XAttribute("protocol", protocol),
                new XAttribute("bindingInformation", "*:" + port.ToString(CultureInfo.InvariantCulture) + ":localhost"));
        }

        private static XElement BuildWindowsAuthenticationLocation()
        {
            return new XElement(
                LocationElement,
                new XAttribute("path", GeneratedSiteName + IisExpressHostingProfile.WindowsAuthApplicationPath),
                new XElement(
                    "system.webServer",
                    new XElement(
                        "security",
                        new XElement(
                            "authentication",
                            new XElement(AnonymousAuthenticationElement, new XAttribute("enabled", "false")),
                            new XElement(WindowsAuthenticationElement, new XAttribute("enabled", "true"))))));
        }

        private static XElement BuildClientCertificateLocation()
        {
            return new XElement(
                LocationElement,
                new XAttribute("path", GeneratedSiteName + IisExpressHostingProfile.ClientCertificateApplicationPath),
                new XElement(
                    "system.webServer",
                    new XElement(
                        "security",
                        new XElement("access", new XAttribute("sslFlags", RequireClientCertificateSslFlags)))));
        }

        private static bool BindsOn(XElement site, string protocol, int port)
        {
            var suffix = ":" + port.ToString(CultureInfo.InvariantCulture) + ":";

            return site.Element("bindings")?
                .Elements("binding")
                .Any(binding =>
                    string.Equals((string)binding.Attribute("protocol"), protocol, StringComparison.OrdinalIgnoreCase) &&
                    ((string)binding.Attribute("bindingInformation") ?? string.Empty).Contains(suffix)) == true;
        }

        private static string RootPhysicalPath(XElement site)
        {
            return site.Elements("application")
                .Where(application => (string)application.Attribute("path") == RootPath)
                .SelectMany(application => application.Elements("virtualDirectory"))
                .Where(directory => (string)directory.Attribute("path") == RootPath)
                .Select(directory => IisExpressPathResolver.NormalizeForComparison((string)directory.Attribute("physicalPath")))
                .FirstOrDefault(path => path != null);
        }

        private static bool DeclaresApplication(XElement site, string virtualPath)
        {
            return site.Elements("application")
                .Any(application => string.Equals((string)application.Attribute("path"), virtualPath, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasLocation(XDocument document, string path)
        {
            return document.Root?
                .Elements(LocationElement)
                .Any(location => string.Equals((string)location.Attribute("path"), path, StringComparison.OrdinalIgnoreCase)) == true;
        }

        private static string Render(XDocument document)
        {
            var builder = new StringBuilder();

            var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };

            using (var writer = XmlWriter.Create(builder, settings))
                document.Save(writer);

            return XmlDeclaration + Environment.NewLine + builder;
        }

        private static bool IsCurrent(string generatedPath, string desired)
        {
            try
            {
                return File.Exists(generatedPath) &&
                       string.Equals(File.ReadAllText(generatedPath), desired, StringComparison.Ordinal);
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static void Write(string generatedPath, string content)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(generatedPath));

                File.WriteAllText(generatedPath, content, new UTF8Encoding(true));
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException)
            {
                throw new TestServiceUnavailableException(
                    "The generated IIS Express hosting configuration could not be written to " +
                    IisExpressPathResolver.Quote(generatedPath) + ": " + exception.Message +
                    Environment.NewLine + "Grant write access to that location, or set " +
                    IisExpressPathResolver.ConfigOverrideVariable +
                    " to the full path of an applicationhost.config that declares the test site.");
            }
        }

        private static string DeriveCheckoutKey(string contentRoot)
        {
            var normalized = (IisExpressPathResolver.NormalizeForComparison(contentRoot) ?? contentRoot)
                .ToLowerInvariant();

            using (var algorithm = SHA256.Create())
            {
                var digest = algorithm.ComputeHash(Encoding.UTF8.GetBytes(normalized));

                var key = new StringBuilder(CheckoutKeyLength);
                for (var index = 0; index * 2 < CheckoutKeyLength; index++)
                    key.Append(digest[index].ToString("x2", CultureInfo.InvariantCulture));

                return key.ToString();
            }
        }
    }
}
