using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SoapClientCallAssistTests.Wcf.Helpers;

public sealed class TestCertificate : IDisposable
{

    private readonly RSACng _key;

    private TestCertificate(RSACng key, X509Certificate2 certificate)
    {
        _key = key;
        Certificate = certificate;
    }

    public X509Certificate2 Certificate { get; }

    public string Thumbprint => Certificate.Thumbprint;

    public static TestCertificate CreateEphemeral(string subject)
    {
        var key = new RSACng(2048);

        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(1));

        return new TestCertificate(key, certificate);
    }

    public void Dispose()
    {
        Certificate.Dispose();
        _key.Dispose();
    }
}
