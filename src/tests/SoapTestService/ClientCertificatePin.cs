using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SoapTestService;

public static class ClientCertificatePin
{
    private const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";

    public static bool Accepts(
        X509Certificate2? certificate,
        X509Certificate2 trustedRoot,
        IReadOnlyCollection<string> pinnedSha256)
    {
        ArgumentNullException.ThrowIfNull(trustedRoot);
        ArgumentNullException.ThrowIfNull(pinnedSha256);

        if (certificate is null)
            return false;

        var fingerprint = Convert.ToHexString(SHA256.HashData(certificate.RawData));

        if (!pinnedSha256.Contains(fingerprint, StringComparer.OrdinalIgnoreCase))
            return false;

        using var chain = new X509Chain();

        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(trustedRoot);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.DisableCertificateDownloads = true;
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid(ClientAuthenticationOid));

        return chain.Build(certificate);
    }
}
