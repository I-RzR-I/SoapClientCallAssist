using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Service.Common;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Text;

namespace SoapClientCallAssistTests.Wcf.Service.SecureConversation;

public sealed class SecureConversationProbeHost : IDisposable
{

    public const string CertificateCredential = "cert";

    public const string UserNameCredential = "user";

    public const string KnownUserName = "dave";

    public const string KnownPassword = "sc-secret";

    private static readonly IReadOnlyDictionary<SoapProtocolType, (string Suffix, MessageVersion Version)> Protocols = ProbeProtocols.Addressing10;

    private static readonly IReadOnlyDictionary<SoapSecureConversationVersionType, (string Suffix, MessageSecurityVersion Version)> SecureConversationVersions = ProbeProtocols.SecureConversationVersions;

    private readonly ServiceHost _host;

    private SecureConversationProbeHost(ServiceHost host, Uri baseAddress, TestCertificate serviceCertificate, TestCertificate clientCertificate)
    {
        _host = host;
        BaseAddress = baseAddress;
        ServiceCertificate = serviceCertificate;
        ClientCertificate = clientCertificate;
    }

    public Uri BaseAddress { get; }

    public TestCertificate ServiceCertificate { get; }

    public TestCertificate ClientCertificate { get; }

    public static SecureConversationProbeHost Open()
    {
        var serviceCertificate = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.ScService");
        var clientCertificate = TestCertificate.CreateEphemeral("CN=SoapClientCallAssist.Wcf.ScClient");

        var baseAddress = new Uri(WcfHostFixture.ListenPrefix + Guid.NewGuid().ToString("N") + "/");
        var host = new ServiceHost(new SecureConversationProbeService(), baseAddress);

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

        return new SecureConversationProbeHost(host, baseAddress, serviceCertificate, clientCertificate);
    }

    public Uri Address(string credential, SoapSecureConversationVersionType version, SoapProtocolType protocol)
        => new(BaseAddress, RelativePath(credential, version, protocol));

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
    }

    private static string RelativePath(string credential, SoapSecureConversationVersionType version, SoapProtocolType protocol)
        => $"{credential}-{SecureConversationVersions[version].Suffix}/{Protocols[protocol].Suffix}";

    private static CustomBinding Binding(string credential, SoapSecureConversationVersionType version, SoapProtocolType protocol)
    {
        var messageVersion = Protocols[protocol].Version;
        var securityVersion = SecureConversationVersions[version].Version;

        var bootstrap = credential == CertificateCredential
            ? SecurityBindingElement.CreateMutualCertificateBindingElement(securityVersion, false)
            : SecurityBindingElement.CreateUserNameForCertificateBindingElement();

        Tune(bootstrap, securityVersion);

        var secureConversation = SecurityBindingElement.CreateSecureConversationBindingElement(bootstrap, true);
        Tune(secureConversation, securityVersion);

        return new CustomBinding(secureConversation, new TextMessageEncodingBindingElement(messageVersion, Encoding.UTF8), new HttpTransportBindingElement());
    }

    private static void Tune(SecurityBindingElement security, MessageSecurityVersion securityVersion)
    {
        security.MessageSecurityVersion = securityVersion;
        security.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Basic256Sha256;
        security.IncludeTimestamp = true;
        security.SetKeyDerivation(true);

        if (security is SymmetricSecurityBindingElement symmetric)
            symmetric.MessageProtectionOrder = MessageProtectionOrder.SignBeforeEncryptAndEncryptSignature;
    }

    private static void Configure(ServiceHost host, X509Certificate2 serviceCertificate, string trustedClientThumbprint)
    {
        ProbeCredentials.Configure(host, serviceCertificate, trustedClientThumbprint, KnownUserName, KnownPassword);

        foreach (var credential in new[] { CertificateCredential, UserNameCredential })
        foreach (var version in SecureConversationVersions.Keys)
        foreach (var protocol in Protocols.Keys)
        {
            host.AddServiceEndpoint(
                typeof(ISecureConversationProbeService),
                Binding(credential, version, protocol),
                RelativePath(credential, version, protocol));
        }
    }
}
