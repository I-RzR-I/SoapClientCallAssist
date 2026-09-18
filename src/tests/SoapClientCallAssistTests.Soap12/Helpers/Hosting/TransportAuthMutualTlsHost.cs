#nullable disable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Transport;
using SoapTestService;
using System;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Helpers.Hosting;

internal sealed class TransportAuthMutualTlsHost : IAsyncDisposable
{

    private const int TlsListenerCount = 3;

    private readonly WebApplication _app;

    private TransportAuthMutualTlsHost(WebApplication app, TransportAuthCertificates certificates)
    {
        _app = app;
        Certificates = certificates;
    }

    internal TransportAuthCertificates Certificates { get; }

    internal Uri PlainBase { get; private set; }

    internal Uri MutualTlsBase { get; private set; }

    internal Uri MutualTls13Base { get; private set; }

    internal Uri ImpostorServerBase { get; private set; }

    internal Uri MutualTlsService => new(MutualTlsBase, SoapEndpoints.ServicePath.TrimStart('/'));

    internal Uri MutualTls13Service => new(MutualTls13Base, SoapEndpoints.ServicePath.TrimStart('/'));

    internal Uri ImpostorServerService => new(ImpostorServerBase, SoapEndpoints.ServicePath.TrimStart('/'));

    internal static async Task<TransportAuthMutualTlsHost> StartAsync(TransportAuthCertificates certificates)
    {
        var pinned = new[]
        {
            TransportAuthTestSupport.Sha256Hex(certificates.ClientLeaf),
            TransportAuthTestSupport.Sha256Hex(certificates.ExpiredClient),
            TransportAuthTestSupport.Sha256Hex(certificates.ForeignCaClient)
        };

        var options = new SoapTestHostOptions
        {
            TlsListeners = new[]
            {
                new SoapTlsListenerOptions
                {
                    ServerCertificate = certificates.ServerLeaf,
                    ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                    TrustedClientRoot = certificates.Authority,
                    PinnedClientCertificateSha256 = pinned
                },
                new SoapTlsListenerOptions
                {
                    ServerCertificate = certificates.ServerLeaf,
                    ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                    TrustedClientRoot = certificates.Authority,
                    PinnedClientCertificateSha256 = pinned,
                    SslProtocols = SslProtocols.Tls13
                },
                new SoapTlsListenerOptions
                {
                    ServerCertificate = certificates.ImpostorServerLeaf
                }
            }
        };

        var app = SoapTestHost.Build(options);
        var host = new TransportAuthMutualTlsHost(app, certificates);

        try
        {
            await app.StartAsync();

            var addresses = TransportAuthTestSupport.BoundAddresses(app);
            var plain = addresses.Where(address => address.Scheme == Uri.UriSchemeHttp).ToList();
            var tls = addresses.Where(address => address.Scheme == Uri.UriSchemeHttps).ToList();

            if (plain.Count != 1 || tls.Count != TlsListenerCount)
                throw new InvalidOperationException(
                    $"Expected one plain and {TlsListenerCount} TLS listeners but the host bound [{string.Join(", ", addresses)}].");

            host.PlainBase = plain[0];
            host.MutualTlsBase = tls[0];
            host.MutualTls13Base = tls[1];
            host.ImpostorServerBase = tls[2];

            return host;
        }
        catch
        {
            await host.DisposeAsync();

            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _app.StopAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await _app.DisposeAsync();
        }
    }
}
