#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Hosting;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Transport;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests.Transport;

[TestClass]
public sealed class MutualTlsTransportTests
{

    private const int Tls13CapableWindowsBuild = 20348;

    private static TransportAuthCertificates _certificates;

    private static TransportAuthMutualTlsHost _host;

    private static int _keyFilesBeforeClass;

    private static DateTime _classStartedUtc;

    public TestContext TestContext { get; set; }

    [ClassInitialize]
    public static async Task StartHost(TestContext context)
    {
        _classStartedUtc = DateTime.UtcNow;
        _keyFilesBeforeClass = TransportAuthTestSupport.CountPersistedKeyFiles();

        context.WriteLine($"persisted key files before the class: {_keyFilesBeforeClass}");

        _certificates = TransportAuthCertificates.Create();

        try
        {
            _host = await TransportAuthMutualTlsHost.StartAsync(_certificates);
        }
        catch
        {
            _certificates.Dispose();
            _certificates = null;

            throw;
        }

        context.WriteLine(
            $"mTLS host: plain={_host.PlainBase} mtls={_host.MutualTlsBase} mtls13={_host.MutualTls13Base} impostor={_host.ImpostorServerBase}");
    }

    [ClassCleanup]
    public static async Task StopHost()
    {
        if (_host is not null)
            await _host.DisposeAsync();

        _certificates?.Dispose();

        _host = null;
        _certificates = null;

        var after = TransportAuthTestSupport.WaitForPersistedKeyFiles(_keyFilesBeforeClass);

        Console.WriteLine($"[MutualTlsTransportTests] persisted key files before={_keyFilesBeforeClass} after={after}");

        if (after != _keyFilesBeforeClass)
            Assert.Fail(
                $"Expected {_keyFilesBeforeClass} persisted key files after the class but found {after}. Every certificate this class "
                + "persisted under UserKeySet must take its key file with it when disposed, otherwise each run litters the user's key "
                + "store with one file per certificate. SChannel releases a server key a moment after the listener stops, which the wait "
                + $"allows for. Files written since the class started: {TransportAuthTestSupport.DescribeKeyFilesWrittenSince(_classStartedUtc)}");
    }

    [TestMethod]
    public void SendRequest_OverMutualTlsWithThePinnedClientCertificate_SucceedsAndTheServiceEchoesTheFingerprint_Test()
    {
        var handler = TransportAuthTestSupport.MutualTlsHandler(_certificates.ClientLeaf, _certificates.ServerLeaf);
        var client = TransportAuthTestSupport.ClientSendingThrough(handler);

        var answer = TransportAuthTestSupport.WhoAmI(client, _host.MutualTlsService, "SendRequest");

        TestContext.WriteLine(answer.ToString());

        Assert.AreEqual(Uri.UriSchemeHttps, answer.Scheme);

        Assert.AreEqual(TransportAuthTestSupport.Sha256Hex(_certificates.ClientLeaf), answer.ClientCertificateSha256);

        Assert.AreEqual(TransportAuthCertificates.ClientSubject, answer.ClientCertificateSubject);

        Assert.AreEqual(DecompressionMethods.GZip | DecompressionMethods.Deflate, handler.AutomaticDecompression);
    }

    [TestMethod]
    public void SendRequest_WithASelfSignedCloneOfTheClientSubject_IsRefusedAtTheHandshake_Test()
    {
        Assert.AreEqual(_certificates.ClientLeaf.Subject, _certificates.SubjectCloneClient.Subject);

        Assert.AreNotEqual(
            TransportAuthTestSupport.Sha256Hex(_certificates.ClientLeaf),
            TransportAuthTestSupport.Sha256Hex(_certificates.SubjectCloneClient));

        var client = TransportAuthTestSupport.ClientSendingThrough(
            TransportAuthTestSupport.MutualTlsHandler(_certificates.SubjectCloneClient, _certificates.ServerLeaf));

        var result = TransportAuthTestSupport.SendWhoAmI(client, _host.MutualTlsService);

        TransportAuthTestSupport.AssertRefusedAtTransport(
            result,
            "SendRequest",
            TransportAuthTestSupport.CertificateSecrets("the clone client certificate", _certificates.SubjectCloneClient));
    }

    [TestMethod]
    public void SendRequest_WithoutAClientCertificate_IsRefusedAtTheHandshakeWithoutLeakingAnything_Test()
    {
        var client = TransportAuthTestSupport.ClientSendingThrough(
            TransportAuthTestSupport.MutualTlsHandler(null, _certificates.ServerLeaf));

        var result = TransportAuthTestSupport.SendWhoAmI(client, _host.MutualTlsService);

        TransportAuthTestSupport.AssertRefusedAtTransport(
            result,
            "SendRequest",
            TransportAuthTestSupport.CertificateSecrets("the server certificate", _certificates.ServerLeaf));
    }

    [TestMethod]
    public void SendRequest_WithAClientCertificateFromAForeignAuthority_IsRefusedEvenThoughItsFingerprintIsPinned_Test()
    {
        Assert.AreEqual(_certificates.ClientLeaf.Subject, _certificates.ForeignCaClient.Subject);

        var client = TransportAuthTestSupport.ClientSendingThrough(
            TransportAuthTestSupport.MutualTlsHandler(_certificates.ForeignCaClient, _certificates.ServerLeaf));

        var result = TransportAuthTestSupport.SendWhoAmI(client, _host.MutualTlsService);

        TransportAuthTestSupport.AssertRefusedAtTransport(
            result,
            "SendRequest",
            TransportAuthTestSupport.CertificateSecrets("the foreign client certificate", _certificates.ForeignCaClient));
    }

    [TestMethod]
    public void SendRequest_WithAnExpiredClientCertificate_IsRefusedEvenThoughItsFingerprintIsPinned_Test()
    {
        Assert.IsTrue(_certificates.ExpiredClient.NotAfter < DateTime.UtcNow);

        var client = TransportAuthTestSupport.ClientSendingThrough(
            TransportAuthTestSupport.MutualTlsHandler(_certificates.ExpiredClient, _certificates.ServerLeaf));

        var result = TransportAuthTestSupport.SendWhoAmI(client, _host.MutualTlsService);

        TransportAuthTestSupport.AssertRefusedAtTransport(
            result,
            "SendRequest",
            TransportAuthTestSupport.CertificateSecrets("the expired client certificate", _certificates.ExpiredClient));
    }

    [TestMethod]
    public void SendRequest_WithAClientCertificateThatHasNoPrivateKey_IsRefusedAsASendFailureWithoutLeaking_Test()
    {
        Assert.IsFalse(_certificates.ClientPublicOnly.HasPrivateKey);

        var client = TransportAuthTestSupport.ClientSendingThrough(
            TransportAuthTestSupport.MutualTlsHandler(_certificates.ClientPublicOnly, _certificates.ServerLeaf));

        var result = TransportAuthTestSupport.SendWhoAmI(client, _host.MutualTlsService);

        TransportAuthTestSupport.AssertRefusedAtTransport(
            result,
            "SendRequest",
            TransportAuthTestSupport.CertificateSecrets("the public-only client certificate", _certificates.ClientPublicOnly));
    }

    [TestMethod]
    public void SendRequest_WhenTheServerPresentsTheSameSubjectUnderADifferentKey_TheClientPinRefusesIt_Test()
    {
        Assert.AreEqual(_certificates.ServerLeaf.Subject, _certificates.ImpostorServerLeaf.Subject);

        var pinnedToTheRealServer = TransportAuthTestSupport.ClientSendingThrough(
            TransportAuthTestSupport.MutualTlsHandler(_certificates.ClientLeaf, _certificates.ServerLeaf));

        var refused = TransportAuthTestSupport.SendWhoAmI(pinnedToTheRealServer, _host.ImpostorServerService);

        TransportAuthTestSupport.AssertRefusedAtTransport(
            refused,
            "SendRequest",
            TransportAuthTestSupport.CertificateSecrets("the impostor server certificate", _certificates.ImpostorServerLeaf));

        var pinnedToTheImpostor = TransportAuthTestSupport.ClientSendingThrough(
            TransportAuthTestSupport.MutualTlsHandler(_certificates.ClientLeaf, _certificates.ImpostorServerLeaf));

        var answer = TransportAuthTestSupport.WhoAmI(
            pinnedToTheImpostor, _host.ImpostorServerService, "SendRequest");

        Assert.AreEqual(Uri.UriSchemeHttps, answer.Scheme);
    }

    [TestMethod]
    public void ServerPin_WithNoCertificateOrADifferentCertificate_Refuses_Test()
    {
        var pin = TransportAuthTestSupport.ServerPin(_certificates.ServerLeaf);

        using var request = new HttpRequestMessage(HttpMethod.Post, _host.MutualTlsService);

        Assert.IsFalse(pin(request, null, null, SslPolicyErrors.RemoteCertificateNotAvailable));
        Assert.IsFalse(pin(request, _certificates.ImpostorServerLeaf, null, SslPolicyErrors.None));
        Assert.IsTrue(pin(request, _certificates.ServerLeaf, null, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [TestMethod]
    public void SendRequest_OverATls13OnlyListener_StillRefusesAnAnonymousConnection_Test()
    {
        var control = TransportAuthTestSupport.SendWhoAmI(
            TransportAuthTestSupport.ClientSendingThrough(
                TransportAuthTestSupport.MutualTlsHandler(_certificates.ClientLeaf, _certificates.ServerLeaf)),
            _host.MutualTls13Service);

        if (!control.IsSuccess)
        {
            var description = NegativeTestSupport.Describe(control);

            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, Tls13CapableWindowsBuild))
                Assert.Inconclusive(
                    $"The control call with the pinned client certificate failed on the TLS 1.3-only listener, and this host is Windows build "
                    + $"{Environment.OSVersion.Version.Build}, below {Tls13CapableWindowsBuild} where SChannel first supports TLS 1.3. The anonymous refusal "
                    + $"cannot be proven here because nothing can complete a TLS 1.3 handshake at all. Control: {description}");

            Assert.Fail($"The control call with the pinned client certificate must succeed on the TLS 1.3-only listener. Got: {description}");
        }

        control.Response.Dispose();

        var anonymous = TransportAuthTestSupport.SendWhoAmI(
            TransportAuthTestSupport.ClientSendingThrough(TransportAuthTestSupport.MutualTlsHandler(null, _certificates.ServerLeaf)),
            _host.MutualTls13Service);

        TransportAuthTestSupport.AssertRefusedAtTransport(
            anonymous,
            "SendRequest",
            TransportAuthTestSupport.CertificateSecrets("the server certificate", _certificates.ServerLeaf));
    }

    [TestMethod]
    public void SendRequest_WhenThePrimaryHandlerIsRegisteredAfterTheLibrary_PinsThatTheCertificateStillAppliesWithoutTheLibraryAugmentation_Test()
    {
        var handler = TransportAuthTestSupport.MutualTlsHandler(_certificates.ClientLeaf, _certificates.ServerLeaf);
        var client = TransportAuthTestSupport.ClientSendingThrough(handler, registerHandlerFirst: false);

        var answer = TransportAuthTestSupport.WhoAmI(client, _host.MutualTlsService, "SendRequest");

        Assert.AreEqual(TransportAuthTestSupport.Sha256Hex(_certificates.ClientLeaf), answer.ClientCertificateSha256);

        Assert.AreEqual(DecompressionMethods.None, handler.AutomaticDecompression);
    }

    [TestMethod]
    public void TransportCertificates_WhenCreatedAndDisposed_LeaveNoPersistedKeyFilesBehind_Test()
    {
        var before = TransportAuthTestSupport.CountPersistedKeyFiles();

        int during;

        using (var set = TransportAuthCertificates.Create())
        {
            during = TransportAuthTestSupport.CountPersistedKeyFiles();

            Assert.IsTrue(set.ClientLeaf.HasPrivateKey && set.ServerLeaf.HasPrivateKey);
        }

        var after = TransportAuthTestSupport.WaitForPersistedKeyFiles(before);

        TestContext.WriteLine($"persisted key files before={before} during={during} after={after}");

        Assert.IsTrue(during > before, $"{before} | {during}");

        Assert.AreEqual(before, after, $"{during}");
    }
}
