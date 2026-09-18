#nullable disable

using SoapClientCallAssistTests.Soap12.Helpers.Transport;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SoapClientCallAssistTests.Soap12.Helpers.Certificates;

internal sealed class TransportAuthCertificates : IDisposable
{

    internal const string ClientSubject = "CN=Scca.Tests.Transport.Client, O=SoapClientCallAssist";

    internal const string ServerSubject = "CN=localhost, O=SoapClientCallAssist";

    internal const string AuthoritySubject = "CN=Scca.Tests.Transport.CA, O=SoapClientCallAssist";

    internal const string ForeignAuthoritySubject = "CN=Scca.Tests.Transport.ForeignCA, O=SoapClientCallAssist";

    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

    private const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";

    private const int SerialNumberLength = 16;

    private static readonly TimeSpan ValidityBefore = TimeSpan.FromDays(1);

    private static readonly TimeSpan ValidityAfter = TimeSpan.FromDays(2);

    private static readonly TimeSpan AuthorityValidityBefore = TimeSpan.FromDays(500);

    private static readonly TimeSpan ExpiredNotBefore = TimeSpan.FromDays(400);

    private static readonly TimeSpan ExpiredNotAfter = TimeSpan.FromDays(35);

    private TransportAuthCertificates()
    {
    }

    internal X509Certificate2 Authority { get; private set; }

    internal X509Certificate2 ForeignAuthority { get; private set; }

    internal X509Certificate2 ServerLeaf { get; private set; }

    internal X509Certificate2 ImpostorServerLeaf { get; private set; }

    internal X509Certificate2 ClientLeaf { get; private set; }

    internal X509Certificate2 SubjectCloneClient { get; private set; }

    internal X509Certificate2 ForeignCaClient { get; private set; }

    internal X509Certificate2 ExpiredClient { get; private set; }

    internal X509Certificate2 ClientPublicOnly { get; private set; }

    internal static TransportAuthCertificates Create()
    {
        var now = DateTimeOffset.UtcNow;
        var set = new TransportAuthCertificates();

        set.Authority = CreateAuthority(AuthoritySubject, now);
        set.ForeignAuthority = CreateAuthority(ForeignAuthoritySubject, now);

        set.ServerLeaf = TransportAuthTestSupport.Persist(
            Issue(set.Authority, ServerSubject, ServerAuthenticationOid, now - ValidityBefore, now + ValidityAfter, serverNames: true));
        set.ImpostorServerLeaf = TransportAuthTestSupport.Persist(
            Issue(set.Authority, ServerSubject, ServerAuthenticationOid, now - ValidityBefore, now + ValidityAfter, serverNames: true));
        set.ClientLeaf = TransportAuthTestSupport.Persist(
            Issue(set.Authority, ClientSubject, ClientAuthenticationOid, now - ValidityBefore, now + ValidityAfter));
        set.SubjectCloneClient = TransportAuthTestSupport.Persist(
            SelfSigned(ClientSubject, ClientAuthenticationOid, now - ValidityBefore, now + ValidityAfter));
        set.ForeignCaClient = TransportAuthTestSupport.Persist(
            Issue(set.ForeignAuthority, ClientSubject, ClientAuthenticationOid, now - ValidityBefore, now + ValidityAfter));
        set.ExpiredClient = TransportAuthTestSupport.Persist(
            Issue(set.Authority, ClientSubject, ClientAuthenticationOid, now - ExpiredNotBefore, now - ExpiredNotAfter));
        set.ClientPublicOnly = X509CertificateLoader.LoadCertificate(set.ClientLeaf.RawData);

        return set;
    }

    public void Dispose()
    {
        ClientPublicOnly?.Dispose();
        ExpiredClient?.Dispose();
        ForeignCaClient?.Dispose();
        SubjectCloneClient?.Dispose();
        ClientLeaf?.Dispose();
        ImpostorServerLeaf?.Dispose();
        ServerLeaf?.Dispose();
        ForeignAuthority?.Dispose();
        Authority?.Dispose();
    }

    private static X509Certificate2 CreateAuthority(string subject, DateTimeOffset now)
    {
        using var key = RSA.Create(2048);

        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        return request.CreateSelfSigned(now - AuthorityValidityBefore, now + ValidityAfter);
    }

    private static X509Certificate2 Issue(
        X509Certificate2 issuer,
        string subject,
        string enhancedKeyUsageOid,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter,
        bool serverNames = false)
    {
        using var key = RSA.Create(2048);

        var request = Leaf(subject, key, enhancedKeyUsageOid, serverNames);

        var serial = RandomNumberGenerator.GetBytes(SerialNumberLength);
        serial[0] &= 0x7F;

        using var issued = request.Create(issuer, notBefore, notAfter, serial);

        return issued.CopyWithPrivateKey(key);
    }

    private static X509Certificate2 SelfSigned(
        string subject, string enhancedKeyUsageOid, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var key = RSA.Create(2048);

        return Leaf(subject, key, enhancedKeyUsageOid, false).CreateSelfSigned(notBefore, notAfter);
    }

    private static CertificateRequest Leaf(string subject, RSA key, string enhancedKeyUsageOid, bool serverNames)
    {
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid(enhancedKeyUsageOid) }, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        if (!serverNames)
            return request;

        var names = new SubjectAlternativeNameBuilder();

        names.AddDnsName("localhost");
        names.AddIpAddress(IPAddress.Loopback);

        request.CertificateExtensions.Add(names.Build());

        return request;
    }
}
