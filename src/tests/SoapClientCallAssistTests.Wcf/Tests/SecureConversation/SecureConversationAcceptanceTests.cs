using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Helpers.Symmetric;
using SoapClientCallAssistTests.Wcf.Service.Common;
using SoapClientCallAssistTests.Wcf.Service.SecureConversation;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Tests.SecureConversation;

[TestClass]
public sealed class SecureConversationAcceptanceTests
{

    private static SecureConversationProbeHost? _host;

    public TestContext TestContext { get; set; } = null!;

    private static SecureConversationProbeHost Host => _host ?? throw new InvalidOperationException("The secure conversation host did not start.");

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _host = SecureConversationProbeHost.Open();

        foreach (var endpoint in _host.Endpoints())
            context.WriteLine(endpoint);
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _host?.Dispose();
        _host = null;
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, SoapSecureConversationVersionType.February2005, DisplayName = "SOAP 1.1, SC Feb 2005")]
    [DataRow(SoapProtocolType.SOAP_1_2, SoapSecureConversationVersionType.December2005, DisplayName = "SOAP 1.2, SC Dec 2005")]
    public async Task FullSequence_IssueThenCallsThenCancel_UnderTheSecurityContext_Test(
        SoapProtocolType protocol, SoapSecureConversationVersionType version)
    {
        var client = WcfCallSupport.CreateClient(protocol);
        var sc = new SoapSecureConversationClient();
        var endpoint = Host.Address(SecureConversationProbeHost.CertificateCredential, version, protocol);
        var bootstrap = Bootstrap(version);

        var issued = await sc.IssueAsync(endpoint, bootstrap, default, protocol);
        Assert.IsTrue(issued.IsSuccess, ResultLeakSweep.Render(issued));

        using var session = issued.Response;
        TestContext.WriteLine($"session context={session.ContextIdentifier} expires={session.ExpiresUtc:o}");

        for (var call = 0; call < 3; call++)
        {
            var identity = await WhoAmIAsync(client, endpoint, session, protocol, $"call #{call}");
            Assert.AreEqual(Host.ClientCertificate.Thumbprint, identity.Name, $"call #{call}");
        }

        var cancelled = await sc.CancelAsync(endpoint, session, default, protocol);
        Assert.IsTrue(cancelled.IsSuccess, ResultLeakSweep.Render(cancelled));
        Assert.IsTrue(session.IsDisposed);

        var afterCancel = client.BuildRequest(
            HttpMethod.Post,
            RequestDto(endpoint, ProbeBodies.WhoAmI(SecureConversationProbeContract.Namespace), SecureConversationProbeContract.WhoAmIAction, SessionSecurity(session)));

        Assert.IsFalse(afterCancel.IsSuccess);
        Assert.AreEqual("V-SEC-063", ResultRendering.FirstKey(afterCancel));
    }

    [TestMethod]
    public async Task IssueAsync_CapturesTheRstAndRstrPairToBin_Test()
    {
        var factory = new CapturingHttpClientFactory();
        var sc = new SoapSecureConversationClient(factory, null);
        var endpoint = Host.Address(SecureConversationProbeHost.CertificateCredential, SoapSecureConversationVersionType.February2005, SoapProtocolType.SOAP_1_1);

        var issued = await sc.IssueAsync(endpoint, Bootstrap(SoapSecureConversationVersionType.February2005), default, SoapProtocolType.SOAP_1_1);

        Assert.IsTrue(issued.IsSuccess, ResultLeakSweep.Render(issued));
        issued.Response.Dispose();

        var directory = AppContext.BaseDirectory;
        Assert.AreEqual(1, factory.Requests.Count);
        File.WriteAllText(Path.Combine(directory, "sc-issue-rst.xml"), factory.Requests[0]);
        File.WriteAllText(Path.Combine(directory, "sc-issue-rstr.xml"), factory.Responses[0]);

        Assert.IsTrue(factory.Responses[0].Contains("RSTR/SCT"));
    }

    [TestMethod]
    public async Task CallOnTheWireAfterCancel_IsRejectedByWcf_Test()
    {
        var client = WcfCallSupport.CreateClient(SoapProtocolType.SOAP_1_1);
        var sc = new SoapSecureConversationClient();
        var endpoint = Host.Address(SecureConversationProbeHost.CertificateCredential, SoapSecureConversationVersionType.February2005, SoapProtocolType.SOAP_1_1);

        var issued = await sc.IssueAsync(endpoint, Bootstrap(SoapSecureConversationVersionType.February2005), default, SoapProtocolType.SOAP_1_1);
        Assert.IsTrue(issued.IsSuccess, ResultLeakSweep.Render(issued));

        var session = issued.Response;

        var keptSecret = (byte[])typeof(SoapSecureConversationSession)
            .GetMethod("CopySecret", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(session, null);

        using var clone = new SoapSecureConversationSession(session.ContextIdentifier, keptSecret, DateTimeOffset.UtcNow.AddMinutes(30), SoapSecureConversationVersionType.February2005);

        var cancelled = await sc.CancelAsync(endpoint, session, default, SoapProtocolType.SOAP_1_1);
        Assert.IsTrue(cancelled.IsSuccess, ResultLeakSweep.Render(cancelled));

        var request = WcfCallSupport.Unwrap(
            client.BuildRequest(HttpMethod.Post, RequestDto(endpoint, ProbeBodies.WhoAmI(SecureConversationProbeContract.Namespace), SecureConversationProbeContract.WhoAmIAction, SessionSecurity(clone))),
            "BuildRequest (cloned cancelled context)");

        var sent = await client.SendRequestAsync(request);
        Assert.IsFalse(sent.IsSuccess);

        Assert.IsNotNull(sent.Response);
        var fault = SoapFaultReader.Read(await sent.Response.Content.ReadAsStringAsync());
        sent.Response.Dispose();

        TestContext.WriteLine($"WCF rejected the reused context with {fault.NamespaceName}#{fault.LocalName}");
        Assert.AreEqual("BadContextToken", fault.LocalName);
    }

    [TestMethod]
    public async Task ExpiredSession_IsRefusedLocallyWithoutTouchingTheWire_Test()
    {
        var client = WcfCallSupport.CreateClient(SoapProtocolType.SOAP_1_1);

        using var expired = new SoapSecureConversationSession(
            "urn:uuid:" + Guid.NewGuid().ToString("D"), new byte[32], DateTimeOffset.UtcNow.AddMinutes(-1), SoapSecureConversationVersionType.February2005);

        var endpoint = Host.Address(SecureConversationProbeHost.CertificateCredential, SoapSecureConversationVersionType.February2005, SoapProtocolType.SOAP_1_1);

        var built = client.BuildRequest(HttpMethod.Post, RequestDto(endpoint, ProbeBodies.WhoAmI(SecureConversationProbeContract.Namespace), SecureConversationProbeContract.WhoAmIAction, SessionSecurity(expired)));

        Assert.IsFalse(built.IsSuccess);
        Assert.AreEqual("V-SEC-062", ResultRendering.FirstKey(built));
    }

    private static async Task<WcfCallerIdentity> WhoAmIAsync(ISoapClientEndpoint client, Uri endpoint, SoapSecureConversationSession session, SoapProtocolType protocol, string what)
    {
        var request = WcfCallSupport.Unwrap(
            client.BuildRequest(HttpMethod.Post, RequestDto(endpoint, ProbeBodies.WhoAmI(SecureConversationProbeContract.Namespace), SecureConversationProbeContract.WhoAmIAction, SessionSecurity(session))),
            $"BuildRequest ({what})");

        var requestWire = await request.Content.ReadAsStringAsync();
        var sent = await client.SendRequestAsync(request);
        using var response = WcfCallSupport.Unwrap(sent, what);
        var responseWire = await response.Content.ReadAsStringAsync();

        CaptureFirstExchange(requestWire, responseWire);

        var plaintext = new WsSecurityResponseSecurity().DecryptAndVerify(request, responseWire);
        Assert.IsTrue(plaintext.IsSuccess, $"{what} | {ResultLeakSweep.Render(plaintext)}");

        return ProbeReaders.ReadWhoAmI(client, plaintext.Response, SecureConversationProbeContract.Namespace);
    }

    private static void CaptureFirstExchange(string requestWire, string responseWire)
    {
        var directory = AppContext.BaseDirectory;
        var requestPath = Path.Combine(directory, "sc-app-request.xml");

        if (File.Exists(requestPath))
            return;

        File.WriteAllText(requestPath, requestWire);
        File.WriteAllText(Path.Combine(directory, "sc-app-response.xml"), responseWire);
    }

    private static SoapSecurityDto Bootstrap(SoapSecureConversationVersionType version)
        => new()
        {
            SigningCertificate = Host.ClientCertificate.Certificate,
            SymmetricBinding = new SoapSymmetricBindingDto
            {
                ServiceCertificate = SymmetricCallSupport.PublicOnly(Host.ServiceCertificate),
                Version = version,
                EndorseWithSigningCertificate = true
            }
        };

    private static SoapSecurityDto SessionSecurity(SoapSecureConversationSession session)
        => new()
        {
            SecureConversation = new SoapSecureConversationDto { Session = session },
            Addressing = new SoapAddressingDto(),
            Encryption = new SoapEncryptionDto { EncryptBody = true, EncryptSignature = true },
            ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true, RequireSignatureConfirmation = false }
        };

    private static BuildSoapRequestDto RequestDto(Uri endpoint, XElement body, string action, SoapSecurityDto security)
        => new()
        {
            Client = new HttpClientDto(endpoint),
            Envelope = new SoapEnvelopeDto(new[] { body }, null, action),
            Security = security
        };
}
