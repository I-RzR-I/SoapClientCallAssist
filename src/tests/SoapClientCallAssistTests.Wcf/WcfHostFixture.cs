using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Service.Common;
using SoapClientCallAssistTests.Wcf.Service.Plain;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;

namespace SoapClientCallAssistTests.Wcf;

[TestClass]
public sealed class WcfHostFixture
{

    public const string PlainRelativePath = "plain";

    public const string StrictActionRelativePath = "strict-action";

    public const string CertificateTransportRelativePath = "cert-transport";

    public const string UserNameTransportRelativePath = "user-transport";

    public const string KnownUserName = "alice";

    public const string KnownPassword = "secret";

    public const string ListenPrefix = "http://localhost:80/Temporary_Listen_Addresses/";

    private static readonly IReadOnlyDictionary<SoapProtocolType, (string Suffix, MessageVersion Version)> Protocols = ProbeProtocols.Plain;

    private static ServiceHost? _host;

    private static Uri? _baseAddress;

    private static TestCertificate? _trustedCertificate;

    private static TestCertificate? _untrustedCertificate;

    public static Uri BaseAddress => Required(_baseAddress, nameof(BaseAddress));

    public static TestCertificate TrustedCertificate => Required(_trustedCertificate, nameof(TrustedCertificate));

    public static TestCertificate UntrustedCertificate => Required(_untrustedCertificate, nameof(UntrustedCertificate));

    public static int KeyFilesAtStart { get; private set; }

    public static Uri Address(string relativePath, SoapProtocolType protocol)
        => new(BaseAddress, $"{relativePath}/{Protocols[protocol].Suffix}");

    [AssemblyInitialize]
    public static void Initialize(TestContext testContext)
    {
        KeyFilesAtStart = CngKeyStoreLitter.CountKeyFiles();
        _trustedCertificate = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.Trusted");
        _untrustedCertificate = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.Untrusted");

        var baseAddress = new Uri(ListenPrefix + Guid.NewGuid().ToString("N") + "/");
        var host = new ServiceHost(new WcfProbeService(), baseAddress);

        try
        {
            Configure(host, _trustedCertificate.Thumbprint);
            host.Open();
        }
        catch (Exception ex)
        {
            host.Abort();
            DisposeCertificates();

            var reason =
                $"The WCF test host could not open {baseAddress} ({ex.GetType().Name}: {ex.Message}). "
                + $"Best effort: {Port80Diagnostics.DescribeHolder()}. "
                + $"Set {EnvironmentFlag.AllowEnvironmentSkipVariable}=true to report this suite as skipped instead of failed.";

            if (EnvironmentFlag.AllowsEnvironmentSkip())
                Assert.Inconclusive(reason);

            Assert.Fail(reason);
        }

        _host = host;
        _baseAddress = baseAddress;

        testContext.WriteLine($"WCF test host listening on {baseAddress}");

        foreach (var endpoint in host.Description.Endpoints)
            testContext.WriteLine($"  {endpoint.Address.Uri} [{endpoint.Binding.MessageVersion}]");
    }

    [AssemblyCleanup]
    public static void Cleanup()
    {
        var host = _host;
        _host = null;
        _baseAddress = null;

        if (host is not null)
        {
            try
            {
                host.Close();
            }
            catch (Exception)
            {
                host.Abort();
            }
        }

        DisposeCertificates();

        var keyFilesAtEnd = CngKeyStoreLitter.CountKeyFiles();

        if (keyFilesAtEnd != KeyFilesAtStart)
            throw new InvalidOperationException(
                $"The run left key files behind under {CngKeyStoreLitter.KeyDirectory}: {KeyFilesAtStart} before, {keyFilesAtEnd} after.");
    }

    private static void Configure(ServiceHost host, string trustedThumbprint)
    {
        var clientCertificates = host.Credentials.ClientCertificate.Authentication;
        clientCertificates.CertificateValidationMode = X509CertificateValidationMode.Custom;
        clientCertificates.RevocationMode = X509RevocationMode.NoCheck;
        clientCertificates.CustomCertificateValidator = new ThumbprintCertificateValidator(new[] { trustedThumbprint });

        var userNames = host.Credentials.UserNameAuthentication;
        userNames.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
        userNames.CustomUserNamePasswordValidator = new InMemoryUserNamePasswordValidator(
            new Dictionary<string, string> { { KnownUserName, KnownPassword } });

        foreach (var protocol in Protocols)
        {
            AddEndpoint(host, WcfProbeBindings.Plain(protocol.Value.Version), PlainRelativePath, protocol.Key);
            AddEndpoint(host, WcfProbeBindings.CertificateOverTransport(protocol.Value.Version), CertificateTransportRelativePath, protocol.Key);
            AddEndpoint(host, WcfProbeBindings.UserNameOverTransport(protocol.Value.Version), UserNameTransportRelativePath, protocol.Key);
        }

        host.AddServiceEndpoint(
            typeof(IWcfProbeService),
            WcfProbeBindings.Plain(MessageVersion.Soap12),
            $"{StrictActionRelativePath}/{Protocols[SoapProtocolType.SOAP_1_2].Suffix}");
    }

    private static void AddEndpoint(ServiceHost host, Binding binding, string relativePath, SoapProtocolType protocol)
    {
        var endpoint = host.AddServiceEndpoint(
            typeof(IWcfProbeService),
            binding,
            $"{relativePath}/{Protocols[protocol].Suffix}");

        if (protocol == SoapProtocolType.SOAP_1_2)
            endpoint.EndpointBehaviors.Add(new BodyElementDispatchBehavior());
    }

    private static void DisposeCertificates()
    {
        _trustedCertificate?.Dispose();
        _untrustedCertificate?.Dispose();
        _trustedCertificate = null;
        _untrustedCertificate = null;
    }

    private static T Required<T>(T? value, string name) where T : class
        => value ?? throw new InvalidOperationException($"{name} is not available because the WCF host fixture did not start.");
}
