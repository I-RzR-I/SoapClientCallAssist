#nullable disable

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SoapClientCallAssistTests.Soap12.Helpers.Certificates;

internal static class TestCertificates
{

    internal static X509Certificate2 SelfSignedRsa(string subject, int keySize = 2048, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null, X509KeyUsageFlags? keyUsage = null)
    {
        using var key = RSA.Create(keySize);

        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        if (keyUsage.HasValue)
            request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage.Value, critical: true));

        return request.CreateSelfSigned(notBefore ?? DateTimeOffset.UtcNow.AddMinutes(-5), notAfter ?? DateTimeOffset.UtcNow.AddDays(1));
    }

    internal static X509Certificate2 Ecdsa(string subject)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        return new CertificateRequest(subject, key, HashAlgorithmName.SHA256)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
    }
}
