using System.Net;

namespace SoapTestService;

public static class SoapTestHost
{
    public static WebApplication Build(string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
            options.Limits.MaxRequestBodySize = 262144;
        });

        builder.Services.AddSingleton<RequestRecorder>();

        var app = builder.Build();

        app.MapGet("/health", () => Results.Ok());
        app.MapSoapService();

        return app;
    }
}
