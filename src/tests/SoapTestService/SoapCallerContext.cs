using System.Security.Cryptography;

namespace SoapTestService;

public sealed record SoapCallerContext(
    string Scheme,
    string? ClientCertificateSha256,
    string? ClientCertificateSubject,
    string? IdentityName,
    string? AuthenticationType,
    bool IsAuthenticated)
{
    public static SoapCallerContext From(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var certificate = context.Connection.ClientCertificate;
        var identity = context.User.Identity;

        return new SoapCallerContext(
            context.Request.Scheme,
            certificate is null ? null : Convert.ToHexString(SHA256.HashData(certificate.RawData)),
            certificate?.Subject,
            identity?.Name,
            identity?.AuthenticationType,
            identity?.IsAuthenticated ?? false);
    }
}
