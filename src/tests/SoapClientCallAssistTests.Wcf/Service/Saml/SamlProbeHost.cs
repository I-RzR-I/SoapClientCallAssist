using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Service.Common;
using System;
using System.Collections.Generic;
using System.IdentityModel.Configuration;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;

namespace SoapClientCallAssistTests.Wcf.Service.Saml;

public sealed class SamlProbeHost : IDisposable
{

    private static readonly IReadOnlyDictionary<SoapProtocolType, (string Suffix, MessageVersion Version)> Protocols = ProbeProtocols.Plain;

    private readonly ServiceHost _host;

    private SamlProbeHost(ServiceHost host, Uri baseAddress)
    {
        _host = host;
        BaseAddress = baseAddress;
    }

    public Uri BaseAddress { get; }

    public Uri Address(string relativePath, SoapProtocolType protocol)
        => new(BaseAddress, $"{relativePath}/{Protocols[protocol].Suffix}");

    public static SamlProbeHost Open(string trustedIssuerThumbprint, TimeSpan maxClockSkew, TestContext testContext)
    {
        var baseAddress = new Uri(WcfHostFixture.ListenPrefix + Guid.NewGuid().ToString("N") + "/");
        var host = new ServiceHost(new SamlProbeService(), baseAddress);

        try
        {
            Configure(host, baseAddress, trustedIssuerThumbprint, maxClockSkew);
            host.Open();
        }
        catch (Exception ex)
        {
            host.Abort();

            var reason =
                $"The SAML WCF test host could not open {baseAddress} ({ex.GetType().Name}: {ex.Message}). "
                + $"Set {EnvironmentFlag.AllowEnvironmentSkipVariable}=true to report this class as skipped instead of failed.";

            if (EnvironmentFlag.AllowsEnvironmentSkip())
                Assert.Inconclusive(reason);

            Assert.Fail(reason);
        }

        testContext.WriteLine($"SAML WCF test host listening on {baseAddress}");

        foreach (var endpoint in host.Description.Endpoints)
            testContext.WriteLine($"  {endpoint.Address.Uri} [{endpoint.Binding.MessageVersion}]");

        return new SamlProbeHost(host, baseAddress);
    }

    public void Dispose()
    {
        try
        {
            _host.Close();
        }
        catch (Exception)
        {
            _host.Abort();
        }
    }

    private static void Configure(ServiceHost host, Uri baseAddress, string trustedIssuerThumbprint, TimeSpan maxClockSkew)
    {
        var registry = new ConfigurationBasedIssuerNameRegistry();
        registry.AddTrustedIssuer(trustedIssuerThumbprint, SamlProbeContract.IssuerName);

        var identity = new IdentityConfiguration(false)
        {
            IssuerNameRegistry = registry,
            CertificateValidationMode = X509CertificateValidationMode.None,
            RevocationMode = X509RevocationMode.NoCheck,
            MaxClockSkew = maxClockSkew,
            SaveBootstrapContext = false
        };

        identity.AudienceRestriction.AudienceMode = AudienceUriMode.Always;

        foreach (var protocol in Protocols)
        {
            foreach (var relativePath in new[] { SamlProbeContract.BearerRelativePath, SamlProbeContract.HolderOfKeyRelativePath })
                identity.AudienceRestriction.AllowedAudienceUris.Add(new Uri(baseAddress, $"{relativePath}/{protocol.Value.Suffix}"));
        }

        host.Credentials.IdentityConfiguration = identity;
        host.Credentials.UseIdentityConfiguration = true;

        foreach (var protocol in Protocols)
        {
            AddEndpoint(host, SamlProbeBindings.IssuedTokenOverTransport(protocol.Value.Version, SecurityKeyType.BearerKey), SamlProbeContract.BearerRelativePath, protocol.Key);
            AddEndpoint(host, SamlProbeBindings.IssuedTokenOverTransport(protocol.Value.Version, SecurityKeyType.AsymmetricKey), SamlProbeContract.HolderOfKeyRelativePath, protocol.Key);
        }
    }

    private static void AddEndpoint(ServiceHost host, Binding binding, string relativePath, SoapProtocolType protocol)
    {
        var endpoint = host.AddServiceEndpoint(typeof(ISamlProbeService), binding, $"{relativePath}/{Protocols[protocol].Suffix}");

        if (protocol == SoapProtocolType.SOAP_1_2)
            endpoint.EndpointBehaviors.Add(new BodyElementDispatchBehavior());
    }
}
