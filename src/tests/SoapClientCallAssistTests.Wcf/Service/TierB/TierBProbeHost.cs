using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Service.Common;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;

namespace SoapClientCallAssistTests.Wcf.Service.TierB;

public sealed class TierBProbeHost : IDisposable
{

    public const string CertificateCredential = "cert";

    public const string UserNameCredential = "user";

    public const string KnownUserName = "carol";

    public const string KnownPassword = "tier-b-secret";

    private static readonly IReadOnlyDictionary<SoapProtocolType, (string Suffix, MessageVersion Version)> Protocols = ProbeProtocols.Addressing10;

    private static readonly IReadOnlyDictionary<SoapSecureConversationVersionType, (string Suffix, MessageSecurityVersion Version)> SecureConversationVersions = ProbeProtocols.SecureConversationVersions;

    private static readonly IReadOnlyDictionary<MessageProtectionOrder, string> ProtectionOrders = ProbeProtocols.ProtectionOrders;

    private readonly ServiceHost _host;

    private TierBProbeHost(ServiceHost host, Uri baseAddress, TestCertificate serviceCertificate, TestCertificate clientCertificate)
    {
        _host = host;
        BaseAddress = baseAddress;
        ServiceCertificate = serviceCertificate;
        ClientCertificate = clientCertificate;
    }

    public Uri BaseAddress { get; }

    public TestCertificate ServiceCertificate { get; }

    public TestCertificate ClientCertificate { get; }

    public static TierBProbeHost Open()
    {
        var serviceCertificate = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.TierBService");
        var clientCertificate = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.TierBClient");

        var baseAddress = new Uri(WcfHostFixture.ListenPrefix + Guid.NewGuid().ToString("N") + "/");
        var host = new ServiceHost(new TierBProbeService(), baseAddress);

        try
        {
            Configure(host, serviceCertificate.Certificate, clientCertificate.Thumbprint);
            host.Open();
        }
        catch (Exception)
        {
            host.Abort();
            serviceCertificate.Dispose();
            clientCertificate.Dispose();

            throw;
        }

        return new TierBProbeHost(host, baseAddress, serviceCertificate, clientCertificate);
    }

    public Uri Address(string credential, SoapSecureConversationVersionType secureConversation, MessageProtectionOrder protectionOrder, SoapProtocolType protocol)
        => new(BaseAddress, RelativePath(credential, secureConversation, protectionOrder, protocol));

    public IEnumerable<string> Endpoints()
    {
        foreach (var endpoint in _host.Description.Endpoints)
            yield return $"{endpoint.Address.Uri} [{endpoint.Binding.MessageVersion}] contract protection {(endpoint.Contract.HasProtectionLevel ? endpoint.Contract.ProtectionLevel.ToString() : "default (EncryptAndSign)")}";
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

        ServiceCertificate.Dispose();
        ClientCertificate.Dispose();
    }

    private static string RelativePath(string credential, SoapSecureConversationVersionType secureConversation, MessageProtectionOrder protectionOrder, SoapProtocolType protocol)
        => $"{credential}-{SecureConversationVersions[secureConversation].Suffix}-{ProtectionOrders[protectionOrder]}/{Protocols[protocol].Suffix}";

    private static void Configure(ServiceHost host, X509Certificate2 serviceCertificate, string trustedClientThumbprint)
    {
        ProbeCredentials.Configure(host, serviceCertificate, trustedClientThumbprint, KnownUserName, KnownPassword);

        foreach (var credential in new[] { CertificateCredential, UserNameCredential })
        foreach (var secureConversation in SecureConversationVersions.Keys)
        foreach (var protectionOrder in ProtectionOrders.Keys)
        foreach (var protocol in Protocols.Keys)
        {
            host.AddServiceEndpoint(
                typeof(ITierBProbeService),
                SymmetricProbeBindings.For(credential, secureConversation, protectionOrder, protocol),
                RelativePath(credential, secureConversation, protectionOrder, protocol));
        }
    }
}
