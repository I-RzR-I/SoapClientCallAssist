using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Service.Common;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;

namespace SoapClientCallAssistTests.Wcf.Service.Symmetric;

public sealed class SymmetricProbeHost : IDisposable
{

    public const string CertificateCredential = "cert";

    public const string UserNameCredential = "user";

    public const string KnownUserName = "bob";

    public const string KnownPassword = "symmetric-secret";

    private static readonly IReadOnlyDictionary<SoapProtocolType, (string Suffix, MessageVersion Version)> Protocols = ProbeProtocols.Addressing10;

    private static readonly IReadOnlyDictionary<SoapSecureConversationVersionType, (string Suffix, MessageSecurityVersion Version)> SecureConversationVersions = ProbeProtocols.SecureConversationVersions;

    private static readonly IReadOnlyDictionary<MessageProtectionOrder, string> ProtectionOrders = ProbeProtocols.ProtectionOrders;

    private readonly ServiceHost _host;

    private SymmetricProbeHost(ServiceHost host, Uri baseAddress, TestCertificate serviceCertificate, TestCertificate clientCertificate, TestCertificate untrustedCertificate)
    {
        _host = host;
        BaseAddress = baseAddress;
        ServiceCertificate = serviceCertificate;
        ClientCertificate = clientCertificate;
        UntrustedCertificate = untrustedCertificate;
    }

    public Uri BaseAddress { get; }

    public TestCertificate ServiceCertificate { get; }

    public TestCertificate ClientCertificate { get; }

    public TestCertificate UntrustedCertificate { get; }

    public static SymmetricProbeHost Open()
    {
        var serviceCertificate = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.SymmetricService");
        var clientCertificate = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.SymmetricClient");
        var untrustedCertificate = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.SymmetricUntrusted");

        var baseAddress = new Uri(WcfHostFixture.ListenPrefix + Guid.NewGuid().ToString("N") + "/");
        var host = new ServiceHost(new SymmetricProbeService(), baseAddress);

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
            untrustedCertificate.Dispose();

            throw;
        }

        return new SymmetricProbeHost(host, baseAddress, serviceCertificate, clientCertificate, untrustedCertificate);
    }

    public Uri Address(string credential, SoapSecureConversationVersionType secureConversation, MessageProtectionOrder protectionOrder, SoapProtocolType protocol)
        => new(BaseAddress, RelativePath(credential, secureConversation, protectionOrder, protocol));

    public static CustomBinding Binding(string credential, SoapSecureConversationVersionType secureConversation, MessageProtectionOrder protectionOrder, SoapProtocolType protocol)
        => SymmetricProbeBindings.For(credential, secureConversation, protectionOrder, protocol);

    public IEnumerable<string> Endpoints()
    {
        foreach (var endpoint in _host.Description.Endpoints)
            yield return $"{endpoint.Address.Uri} [{endpoint.Binding.MessageVersion}]";
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
        UntrustedCertificate.Dispose();
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
                typeof(ISymmetricProbeService),
                Binding(credential, secureConversation, protectionOrder, protocol),
                RelativePath(credential, secureConversation, protectionOrder, protocol));
        }
    }
}
