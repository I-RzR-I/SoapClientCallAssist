#nullable disable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Helpers.Hosting;

internal sealed class RedirectHostPair : IAsyncDisposable
{

    internal const string LandingPath = "/landing";

    private const string RedirectPathPrefix = "/redirect/";

    private readonly WebApplication _redirector;

    private readonly WebApplication _target;

    private readonly ConcurrentQueue<RedirectTargetRecord> _received = new();

    private RedirectHostPair(WebApplication redirector, WebApplication target)
    {
        _redirector = redirector;
        _target = target;
    }

    internal Uri RedirectorBase { get; private set; }

    internal Uri TargetBase { get; private set; }

    internal IReadOnlyList<RedirectTargetRecord> Received => _received.ToArray();

    internal Uri RedirectingTo(HttpStatusCode status) => new(RedirectorBase, $"{RedirectPathPrefix}{(int)status}");

    internal void Reset() => _received.Clear();

    internal static async Task<RedirectHostPair> StartAsync()
    {
        var target = Listener();
        var redirector = Listener();

        var pair = new RedirectHostPair(redirector, target);

        target.MapMethods(LandingPath, new[] { "GET", "POST" }, pair.RecordAndAnswer);

        redirector.MapPost($"{RedirectPathPrefix}{{status:int}}", (int status, HttpContext context) =>
        {
            context.Response.StatusCode = status;
            context.Response.Headers.Location = new Uri(pair.TargetBase, LandingPath).ToString();

            return Task.CompletedTask;
        });

        await target.StartAsync();
        pair.TargetBase = BoundAddress(target);

        await redirector.StartAsync();
        pair.RedirectorBase = BoundAddress(redirector);

        return pair;
    }

    public async ValueTask DisposeAsync()
    {
        await Stop(_redirector);
        await Stop(_target);
    }

    private async Task RecordAndAnswer(HttpContext context)
    {
        using var reader = new StreamReader(context.Request.Body);

        _received.Enqueue(new RedirectTargetRecord
        {
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            Body = await reader.ReadToEndAsync(),
            Headers = context.Request.Headers.ToDictionary(
                header => header.Key,
                header => header.Value.ToString(),
                StringComparer.OrdinalIgnoreCase)
        });

        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.ContentType = "application/soap+xml; charset=utf-8";

        await context.Response.WriteAsync(NegativeTestSupport.UnindentedSoap12Envelope);
    }

    private static WebApplication Listener()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

        return builder.Build();
    }

    private static Uri BoundAddress(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault()
                      ?? throw new InvalidOperationException("The redirect host reported no bound address.");

        var bound = new Uri(address);

        return new UriBuilder(bound.Scheme, bound.Host, bound.Port).Uri;
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
