using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapTestService;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12;

[TestClass]
public sealed class SoapServiceFixture
{

    private const string ServiceRelativePath = "ServiceSvc.svc";

    private const string Service11RelativePath = "ServiceAsmx.asmx";

    private static WebApplication? _app;

    private static Uri? _baseAddress;

    private static Uri? _serviceUri;

    private static Uri? _service11Uri;

    private static RequestRecorder? _recorder;

    public static Uri BaseAddress => Required(_baseAddress, nameof(BaseAddress));

    public static Uri ServiceUri => Required(_serviceUri, nameof(ServiceUri));

    public static Uri Service11Uri => Required(_service11Uri, nameof(Service11Uri));

    public static RequestRecorder Recorder => Required(_recorder, nameof(Recorder));

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext testContext)
    {
        var app = SoapTestHost.Build();
        _app = app;

        try
        {
            await app.StartAsync();

            var baseAddress = ReadBoundAddress(app);

            _baseAddress = baseAddress;
            _serviceUri = new Uri(baseAddress, ServiceRelativePath);
            _service11Uri = new Uri(baseAddress, Service11RelativePath);
            _recorder = app.Services.GetRequiredService<RequestRecorder>();

            testContext.WriteLine(
                $"SOAP test service listening on {baseAddress}, endpoints {_serviceUri} and {_service11Uri}");
        }
        catch
        {

            await ShutdownAsync();

            throw;
        }
    }

    [AssemblyCleanup]
    public static Task CleanupAsync() => ShutdownAsync();

    private static async Task ShutdownAsync()
    {
        var app = _app;
        if (app is null)
            return;

        _app = null;
        _baseAddress = null;
        _serviceUri = null;
        _service11Uri = null;
        _recorder = null;

        try
        {
            await app.StopAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    private static Uri ReadBoundAddress(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()
                        ?? throw new InvalidOperationException(
                            "The started test host exposes no IServerAddressesFeature, so the bound port cannot be discovered.");

        var address = addresses.Addresses.FirstOrDefault()
                      ?? throw new InvalidOperationException(
                          "The started test host reported no bound addresses, so the bound port cannot be discovered.");

        var bound = new Uri(address);

        if (bound.Port == 0)
            throw new InvalidOperationException(
                $"The test host reported the unbound placeholder port in '{address}'; the listener is not ready.");

        return new UriBuilder(bound.Scheme, bound.Host, bound.Port).Uri;
    }

    private static T Required<T>(T? value, string name) where T : class
        => value ?? throw new InvalidOperationException(
            $"{name} is unavailable because the SOAP test service is not running. " +
            "Check the failure reported by SoapServiceFixture.InitializeAsync.");
}
