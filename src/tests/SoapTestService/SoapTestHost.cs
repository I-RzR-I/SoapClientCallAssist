using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace SoapTestService;

public static class SoapTestHost
{
    public static WebApplication Build(string[]? args = null) => Build(new SoapTestHostOptions(), args);

    public static WebApplication Build(SoapTestHostOptions options, string[]? args = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var builder = WebApplication.CreateBuilder(args ?? []);

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.Listen(IPAddress.Loopback, 0);
            kestrel.Limits.MaxRequestBodySize = 262144;

            foreach (var listener in options.TlsListeners)
                kestrel.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(https => Configure(https, listener)));
        });

        builder.Services.AddSingleton<RequestRecorder>();

        var app = builder.Build();

        app.MapGet("/health", () => Results.Ok());
        app.MapSoapService();

        return app;
    }

    private static void Configure(HttpsConnectionAdapterOptions https, SoapTlsListenerOptions listener)
    {
        https.ServerCertificate = listener.ServerCertificate;
        https.ClientCertificateMode = listener.ClientCertificateMode;
        https.SslProtocols = listener.SslProtocols;

        if (listener.TrustedClientRoot is null)
            return;

        var trustedRoot = listener.TrustedClientRoot;
        var pinned = listener.PinnedClientCertificateSha256;

        https.ClientCertificateValidation = (certificate, _, _) => ClientCertificatePin.Accepts(certificate, trustedRoot, pinned);
    }
}
