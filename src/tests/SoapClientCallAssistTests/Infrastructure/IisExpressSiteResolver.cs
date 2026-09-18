#region U S I N G

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssistTests.Infrastructure
{
    public static class IisExpressSiteResolver
    {
        public const string SiteIdOverrideVariable = "SOAPCLIENTCALLASSIST_IISEXPRESS_SITEID";

        private const string HttpProtocol = "http";

        private const string RootPath = "/";

        public static IisExpressSite Resolve(string configPath, int port, string expectedContentRoot, Action<string> log)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));

            var normalizedExpected = IisExpressPathResolver.NormalizeForComparison(expectedContentRoot);
            var overriddenSiteId = ReadOverriddenSiteId();

            IReadOnlyList<XElement> siteElements;

            try
            {
                siteElements = ReadSiteElements(configPath);
            }
            catch (TestServiceUnavailableException exception) when (overriddenSiteId.HasValue)
            {
                log("WARNING: " + SiteIdOverrideVariable + " forces site id " + overriddenSiteId.Value +
                    " and " + IisExpressPathResolver.Quote(configPath) + " could not be read, so the check " +
                    "that the site serves this checkout was SKIPPED. This run may exercise a copy of the " +
                    "service from outside this repository. Reason: " + exception.Message);

                return new IisExpressSite(overriddenSiteId.Value, "set by " + SiteIdOverrideVariable, null);
            }

            if (overriddenSiteId.HasValue)
                return ResolveOverriddenSite(siteElements, overriddenSiteId.Value, normalizedExpected, configPath, log);

            var sites = siteElements
                .Where(element => BindsHttpOn(element, port))
                .Select(CreateSite)
                .Where(site => site != null)
                .ToList();

            if (sites.Count == 0)
                throw new TestServiceUnavailableException(
                    "No site in " + IisExpressPathResolver.Quote(configPath) + " binds " + HttpProtocol +
                    " on port " + port + ", so the SOAP test service cannot be started there." +
                    Environment.NewLine + "Set " + SiteIdOverrideVariable +
                    " to the id of the site that hosts the test service.");

            var candidates = normalizedExpected == null
                ? sites
                : sites
                    .Where(site => string.Equals(
                        site.PhysicalPath, normalizedExpected, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (candidates.Count == 1)
                return candidates[0];

            throw new TestServiceUnavailableException(
                BuildAmbiguityMessage(configPath, port, sites, DescribeSelection(normalizedExpected, candidates.Count)));
        }

        private static IisExpressSite ResolveOverriddenSite(
            IReadOnlyList<XElement> siteElements,
            int siteId,
            string normalizedExpected,
            string configPath,
            Action<string> log)
        {
            var site = siteElements
                .Select(CreateSite)
                .FirstOrDefault(candidate => candidate != null && candidate.Id == siteId);

            if (site == null)
                throw new InvalidOperationException(
                    SiteIdOverrideVariable + " is set to " + siteId + ", but " +
                    IisExpressPathResolver.Quote(configPath) + " declares no site with that id. Set it to " +
                    "an id that exists, or clear it to let the fixture select the site itself.");

            if (normalizedExpected == null)
            {
                log("WARNING: " + SiteIdOverrideVariable + " forces site " + site + " and the content root " +
                    "of the in-repository test service could not be resolved, so the check that the site " +
                    "serves this checkout was SKIPPED.");

                return site;
            }

            if (site.PhysicalPath == null)
            {
                log("WARNING: " + SiteIdOverrideVariable + " forces site " + site + ", which declares no " +
                    "readable root content path, so the check that the site serves this checkout was " +
                    "SKIPPED.");

                return site;
            }

            if (!string.Equals(site.PhysicalPath, normalizedExpected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    SiteIdOverrideVariable + " is set to " + siteId + ", but that site serves " +
                    IisExpressPathResolver.Quote(site.PhysicalPath) + " rather than this checkout at " +
                    IisExpressPathResolver.Quote(normalizedExpected) + "." + Environment.NewLine +
                    "Starting it would run every test against a copy of the service from outside this " +
                    "repository. A value left at user or machine scope while debugging another checkout is " +
                    "the usual cause. Clear " + SiteIdOverrideVariable + " to let the fixture select the " +
                    "site itself, or set it to the id of the site that serves this checkout.");

            return site;
        }

        private static string DescribeSelection(string normalizedExpected, int candidateCount)
        {
            if (normalizedExpected == null)
                return "The content root of the in-repository test service could not be resolved, and more " +
                       "than one site binds the port.";

            return candidateCount == 0
                ? "No site serves this checkout at " + IisExpressPathResolver.Quote(normalizedExpected) +
                  ". Starting any of the sites below would serve a copy of the service from outside this " +
                  "repository."
                : "Several sites serve " + IisExpressPathResolver.Quote(normalizedExpected) + ".";
        }

        private static IReadOnlyList<XElement> ReadSiteElements(string configPath)
        {
            XDocument document;

            try
            {
                document = XDocument.Load(configPath);
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException)
            {
                throw new TestServiceUnavailableException(
                    "The IIS Express hosting configuration " + IisExpressPathResolver.Quote(configPath) +
                    " could not be read: " + exception.Message + Environment.NewLine + "Set " +
                    IisExpressPathResolver.ConfigOverrideVariable +
                    " to the full path of an applicationhost.config that declares the test site.");
            }
            catch (XmlException exception)
            {
                throw new TestServiceUnavailableException(
                    "The IIS Express hosting configuration " + IisExpressPathResolver.Quote(configPath) +
                    " is not valid XML: " + exception.Message);
            }

            var siteElements = document.Root?
                .Element("system.applicationHost")?
                .Element("sites")?
                .Elements("site");

            if (siteElements == null)
                throw new TestServiceUnavailableException(
                    "The IIS Express hosting configuration " + IisExpressPathResolver.Quote(configPath) +
                    " declares no system.applicationHost/sites section.");

            return siteElements.ToList();
        }

        private static int? ReadOverriddenSiteId()
        {
            var rawValue = Environment.GetEnvironmentVariable(SiteIdOverrideVariable);
            if (string.IsNullOrWhiteSpace(rawValue))
                return null;

            if (!int.TryParse(rawValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var siteId))
                throw new TestServiceUnavailableException(
                    SiteIdOverrideVariable + " is set to " + IisExpressPathResolver.Quote(rawValue) +
                    ", which is not a whole number. Set it to a numeric IIS Express site id, or clear it to " +
                    "let the fixture select the site itself.");

            return siteId;
        }

        private static bool BindsHttpOn(XElement siteElement, int port)
        {
            return siteElement
                .Element("bindings")?
                .Elements("binding")
                .Any(binding =>
                    string.Equals((string)binding.Attribute("protocol"), HttpProtocol, StringComparison.OrdinalIgnoreCase)
                    && ReadBindingPort((string)binding.Attribute("bindingInformation")) == port) == true;
        }

        private static int? ReadBindingPort(string bindingInformation)
        {
            if (string.IsNullOrWhiteSpace(bindingInformation))
                return null;

            var parts = bindingInformation.Split(':');
            if (parts.Length < 2)
                return null;

            return int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
                ? port
                : (int?)null;
        }

        private static IisExpressSite CreateSite(XElement siteElement)
        {
            var rawId = (string)siteElement.Attribute("id");
            if (!int.TryParse(rawId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                return null;

            var physicalPath = siteElement
                .Elements("application")
                .Where(IsRootPath)
                .SelectMany(application => application.Elements("virtualDirectory"))
                .Where(IsRootPath)
                .Select(directory => IisExpressPathResolver.NormalizeForComparison((string)directory.Attribute("physicalPath")))
                .FirstOrDefault(path => path != null);

            return new IisExpressSite(id, (string)siteElement.Attribute("name"), physicalPath);
        }

        private static bool IsRootPath(XElement element)
        {
            return string.Equals((string)element.Attribute("path"), RootPath, StringComparison.Ordinal);
        }

        private static string BuildAmbiguityMessage(
            string configPath,
            int port,
            IEnumerable<IisExpressSite> candidates,
            string reason)
        {
            var builder = new StringBuilder();

            builder.Append("The IIS Express site hosting the SOAP test service could not be identified in ")
                .Append(IisExpressPathResolver.Quote(configPath))
                .Append('.')
                .Append(Environment.NewLine)
                .Append(reason)
                .Append(Environment.NewLine)
                .Append("Sites binding ")
                .Append(HttpProtocol)
                .Append(" on port ")
                .Append(port)
                .Append(':');

            foreach (var candidate in candidates)
                builder.Append(Environment.NewLine).Append("  - ").Append(candidate);

            builder.Append(Environment.NewLine)
                .Append("Set ")
                .Append(SiteIdOverrideVariable)
                .Append(" to the id of the site that serves this checkout.");

            return builder.ToString();
        }
    }
}
