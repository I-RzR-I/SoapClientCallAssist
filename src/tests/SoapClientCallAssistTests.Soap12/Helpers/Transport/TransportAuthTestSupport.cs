#nullable disable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Transport;

internal static class TransportAuthTestSupport
{

    internal const string SendFailureCode = "ER-BEC-BSRM-SR";

    internal const string PfxPassword = "Transport-Pfx-Pwd!41c9";

    internal const string WhoAmIOperation = "WhoAmI";

    private const string CngKeyDirectory = @"Microsoft\Crypto\Keys";

    private const string CapiKeyDirectory = @"Microsoft\Crypto\RSA";

    private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(15);

    private static readonly TimeSpan KeyReleaseTimeout = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan KeyReleasePollInterval = TimeSpan.FromMilliseconds(100);

    private static readonly string[] ForbiddenInRefusal =
    {
        "   at ",
        ".cs:line",
        "AuthenticationException",
        "System.Security.Cryptography",
        "System.Security.Authentication",
        "RemoteCertificateValidationCallback",
        "remote certificate",
        "handshake"
    };

    internal static X509Certificate2 Persist(X509Certificate2 ephemeral)
    {
        using (ephemeral)
        {
            var pfx = ephemeral.Export(X509ContentType.Pfx, PfxPassword);

            try
            {
                return X509CertificateLoader.LoadPkcs12(pfx, PfxPassword, X509KeyStorageFlags.UserKeySet);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pfx);
            }
        }
    }

    internal static string Sha256Hex(X509Certificate2 certificate)
        => Convert.ToHexString(SHA256.HashData(certificate.RawData));

    internal static int CountPersistedKeyFiles()
        => KeyDirectories().Sum(CountFiles);

    internal static int WaitForPersistedKeyFiles(int expected)
    {
        var deadline = DateTime.UtcNow + KeyReleaseTimeout;
        var count = CountPersistedKeyFiles();

        while (count != expected && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(KeyReleasePollInterval);
            count = CountPersistedKeyFiles();
        }

        return count;
    }

    internal static string DescribeKeyFilesWrittenSince(DateTime sinceUtc)
    {
        var recent = KeyDirectories()
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            .Where(file => File.GetLastWriteTimeUtc(file) >= sinceUtc)
            .Select(file => $"{Path.GetFileName(file)} ({File.GetLastWriteTimeUtc(file):O})")
            .ToList();

        return recent.Count == 0 ? "<none>" : string.Join(", ", recent);
    }

    internal static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerPin(X509Certificate2 expected)
    {
        var pinned = Sha256Hex(expected);

        return (_, presented, _, _) => presented is not null && string.Equals(Sha256Hex(presented), pinned, StringComparison.Ordinal);
    }

    internal static ISoapClientEndpoint ClientSendingThrough(HttpClientHandler handler, bool registerHandlerFirst = true)
    {
        var services = new ServiceCollection();

        if (registerHandlerFirst)
        {
            services.AddHttpClient(SoapClientEndpointExtensions.SoapHttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
            services.RegisterSoapClientsEndpoint();
        }
        else
        {
            services.RegisterSoapClientsEndpoint();
            services.AddHttpClient(SoapClientEndpointExtensions.SoapHttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        }

        var client = services.BuildServiceProvider().GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>()(SoapProtocolType.SOAP_1_2);

        client.SetClientTimeout(NetworkTimeout);

        return client;
    }

    internal static HttpClientHandler MutualTlsHandler(X509Certificate2 clientCertificate, X509Certificate2 pinnedServer)
    {
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = ServerPin(pinnedServer),
            AllowAutoRedirect = false,
            UseProxy = false
        };

        if (clientCertificate is not null)
            handler.ClientCertificates.Add(clientCertificate);

        return handler;
    }

    internal static IResult<HttpResponseMessage> SendWhoAmI(ISoapClientEndpoint client, Uri endpoint)
    {
        var built = client.BuildRequest(
            HttpMethod.Post,
            endpoint,
            new[] { new XElement(Soap12FunctionalSupport.Service + WhoAmIOperation) });

        using var request = Soap12FunctionalSupport.Unwrap(built, $"BuildRequest {WhoAmIOperation} | {endpoint}");

        return client.SendRequest(request);
    }

    internal static TransportAuthWhoAmI WhoAmI(ISoapClientEndpoint client, Uri endpoint, string what)
    {
        using var response = Soap12FunctionalSupport.Unwrap(SendWhoAmI(client, endpoint), what);

        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var payload = SoapAssert.GetBodyChild(envelope);

        Assert.AreEqual((Soap12FunctionalSupport.Service + $"{WhoAmIOperation}Response").ToString(), payload.Name.ToString(), $"{what} | {envelope}");

        var result = payload.Element(Soap12FunctionalSupport.Service + $"{WhoAmIOperation}Result");

        Assert.IsNotNull(result, $"{what} | {envelope}");

        return new TransportAuthWhoAmI
        {
            Scheme = Text(result, "Scheme"),
            ClientCertificateSha256 = Text(result, "ClientCertificateSha256"),
            ClientCertificateSubject = Text(result, "ClientCertificateSubject"),
            IdentityName = Text(result, "IdentityName"),
            AuthenticationType = Text(result, "AuthenticationType"),
            IsAuthenticated = string.Equals(Text(result, "IsAuthenticated"), "true", StringComparison.OrdinalIgnoreCase)
        };
    }

    internal static void AssertRefusedAtTransport(IResult<HttpResponseMessage> result, string what, params ForbiddenSecret[] secrets)
    {
        Assert.IsFalse(result.IsSuccess, $"{what} | {NegativeTestSupport.Describe(result)}");

        Assert.AreEqual(SendFailureCode, NegativeTestSupport.Messages(result)[0].Key, $"{what} | {NegativeTestSupport.Describe(result)}");

        Assert.IsNull(result.Response, what);

        var swept = MessageLeakSweep.Render(result);

        var forbidden = ForbiddenInRefusal
            .Select(marker => (Name: $"the marker [{marker}]", Value: marker))
            .Append(("the test binary directory", AppContext.BaseDirectory))
            .Append(("the host temporary directory", Path.GetTempPath()))
            .Concat(secrets.SelectMany(secret => secret.Renderings()));

        foreach (var (name, value) in forbidden)
        {
            Assert.IsFalse(swept.Contains(value, StringComparison.OrdinalIgnoreCase), $"{what} | {name} | {swept}");
        }
    }

    internal static ForbiddenSecret[] CertificateSecrets(string name, X509Certificate2 certificate)
        => new[]
        {
            ForbiddenSecret.OfText($"{name} subject", certificate.Subject),
            ForbiddenSecret.OfText($"{name} SHA-1 thumbprint", certificate.Thumbprint),
            ForbiddenSecret.OfText($"{name} SHA-256 fingerprint", Sha256Hex(certificate)),
            ForbiddenSecret.OfBytes($"{name} DER bytes", certificate.RawData),
            ForbiddenSecret.OfText("the PFX password", PfxPassword)
        };

    internal static IReadOnlyList<Uri> BoundAddresses(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()
                      ?? throw new InvalidOperationException("The started host exposes no IServerAddressesFeature.");

        return feature.Addresses
            .Select(address => new Uri(address))
            .Select(bound => new UriBuilder(bound.Scheme, bound.Host, bound.Port).Uri)
            .ToList();
    }

    private static string Text(XElement scope, string localName)
        => scope.Element(Soap12FunctionalSupport.Service + localName)?.Value ?? string.Empty;

    private static IEnumerable<string> KeyDirectories()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        yield return Path.Combine(roaming, CngKeyDirectory);
        yield return Path.Combine(roaming, CapiKeyDirectory);
    }

    private static int CountFiles(string directory)
        => Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Count() : 0;
}
