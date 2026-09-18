using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace SoapTestService;

public sealed class SoapTlsListenerOptions
{
    public required X509Certificate2 ServerCertificate { get; init; }

    public ClientCertificateMode ClientCertificateMode { get; init; } = ClientCertificateMode.NoCertificate;

    public X509Certificate2? TrustedClientRoot { get; init; }

    public IReadOnlyCollection<string> PinnedClientCertificateSha256 { get; init; } = Array.Empty<string>();

    public SslProtocols SslProtocols { get; init; } = SslProtocols.None;
}
