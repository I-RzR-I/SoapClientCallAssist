#nullable disable

using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoapClientCallAssistTests.Soap12.Helpers.Transport;
using SoapTestService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Helpers.Hosting;

internal sealed class TransportAuthNegotiateHosts : IAsyncDisposable
{

    internal const string RedirectToOtherPath = "/redirect-to-other";

    private readonly WebApplication _hostA;

    private readonly WebApplication _hostB;

    private readonly ConcurrentQueue<TransportAuthWireRecord> _wireA = new();

    private readonly ConcurrentQueue<TransportAuthWireRecord> _wireB = new();

    private TransportAuthNegotiateHosts(WebApplication hostA, WebApplication hostB)
    {
        _hostA = hostA;
        _hostB = hostB;
    }

    internal Uri BaseA { get; private set; }

    internal Uri BaseB { get; private set; }

    internal Uri ServiceA => new(BaseA, SoapEndpoints.ServicePath.TrimStart('/'));

    internal Uri ServiceB => new(BaseB, SoapEndpoints.ServicePath.TrimStart('/'));

    internal Uri RedirectOnAToB => new(BaseA, RedirectToOtherPath.TrimStart('/'));

    internal IReadOnlyList<TransportAuthWireRecord> WireA => _wireA.ToArray();

    internal IReadOnlyList<TransportAuthWireRecord> WireB => _wireB.ToArray();

    internal void Reset()
    {
        _wireA.Clear();
        _wireB.Clear();
    }

    internal static async Task<TransportAuthNegotiateHosts> StartAsync()
    {
        var hostA = ChallengingSoapHost();
        var hostB = ChallengingSoapHost();
        var pair = new TransportAuthNegotiateHosts(hostA, hostB);

        pair.Wire(hostA, pair._wireA);
        pair.Wire(hostB, pair._wireB);

        hostA.MapPost(RedirectToOtherPath, (HttpContext context) =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.TemporaryRedirect;
            context.Response.Headers.Location = pair.ServiceB.ToString();

            return Task.CompletedTask;
        });

        try
        {
            await hostB.StartAsync();
            pair.BaseB = TransportAuthTestSupport.BoundAddresses(hostB).Single();

            await hostA.StartAsync();
            pair.BaseA = TransportAuthTestSupport.BoundAddresses(hostA).Single();

            return pair;
        }
        catch
        {
            await pair.DisposeAsync();

            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Stop(_hostA);
        await Stop(_hostB);
    }

    private void Wire(WebApplication app, ConcurrentQueue<TransportAuthWireRecord> sink)
    {
        app.Use(async (context, next) =>
        {
            sink.Enqueue(new TransportAuthWireRecord
            {
                Path = context.Request.Path.Value,
                Host = context.Request.Headers.Host.ToString(),
                Authorization = context.Request.Headers.Authorization.ToString()
            });

            await next(context);
        });

        app.UseAuthentication();
        app.UseAuthorization();

        var protectedGroup = app.MapGroup(string.Empty).RequireAuthorization();

        protectedGroup.MapSoapService();
    }

    private static WebApplication ChallengingSoapHost()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

        builder.Services.AddSingleton<RequestRecorder>();
        builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
        builder.Services.AddAuthorization();

        return builder.Build();
    }

    private static async Task Stop(WebApplication app)
    {
        try
        {
            await app.StopAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await app.DisposeAsync();
        }
    }
}
