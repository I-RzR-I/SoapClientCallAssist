#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers.Hosting;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Transport;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Transport;

[TestClass]
public sealed class NegotiateTransportTests
{

    private const string NegotiateScheme = "Negotiate";

    private const string NtlmAuthenticationType = "NTLM";

    private static TransportAuthNegotiateHosts _hosts;

    public TestContext TestContext { get; set; }

    [ClassInitialize]
    public static async Task StartHosts(TestContext context)
    {
        _hosts = await TransportAuthNegotiateHosts.StartAsync();

        context.WriteLine($"Negotiate hosts: A={_hosts.BaseA} B={_hosts.BaseB}");
    }

    [ClassCleanup]
    public static async Task StopHosts()
    {
        if (_hosts is not null)
            await _hosts.DisposeAsync();

        _hosts = null;
    }

    [TestInitialize]
    public void ResetWire() => _hosts.Reset();

    [TestMethod]
    public void SendRequest_WithDefaultCredentialsCachedForTheExactHost_AuthenticatesAsTheCurrentWindowsUserOverNtlm_Test()
    {
        var client = TransportAuthTestSupport.ClientSendingThrough(HandlerWithCredentialsFor(_hosts.BaseA));

        var answer = TransportAuthTestSupport.WhoAmI(client, _hosts.ServiceA, "SendRequest");

        TestContext.WriteLine(answer.ToString());
        TestContext.WriteLine($"wire A: {string.Join(" | ", _hosts.WireA)}");

        Assert.IsTrue(answer.IsAuthenticated, $"{answer}");

        Assert.AreEqual(WindowsIdentity.GetCurrent().Name, answer.IdentityName, true, $"{answer}");

        Assert.IsTrue(
            string.Equals(answer.AuthenticationType, NtlmAuthenticationType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer.AuthenticationType, NegotiateScheme, StringComparison.OrdinalIgnoreCase),
            $"{answer}");

        Assert.AreEqual(Uri.UriSchemeHttp, answer.Scheme);

        var authorized = _hosts.WireA.Where(record => record.CarriesAuthorization).ToList();

        Assert.IsTrue(authorized.Count > 0, string.Join(" | ", _hosts.WireA));

        Assert.IsTrue(
            authorized.All(record => string.Equals(record.AuthorizationScheme, NegotiateScheme, StringComparison.OrdinalIgnoreCase)),
            string.Join(" | ", _hosts.WireA));
    }

    [TestMethod]
    public void SendRequest_AnonymouslyToAChallengingHost_FailsUnderThe401CodeNamingNegotiate_Test()
    {
        var client = TransportAuthTestSupport.ClientSendingThrough(AnonymousHandler());

        var result = TransportAuthTestSupport.SendWhoAmI(client, _hosts.ServiceA);

        using var response = Soap12FunctionalSupport.UnwrapRejected(result, NegativeTestSupport.HttpUnauthorizedCode, "SendRequest");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        StringAssert.Contains(NegativeTestSupport.FirstMessageInfo(result), NegotiateScheme);

        SecretLeakAssert.CarriesNoSecret(result, "SendRequest");

        Assert.IsFalse(_hosts.WireA.Any(record => record.CarriesAuthorization), string.Join(" | ", _hosts.WireA));
    }

    [TestMethod]
    public void SendRequest_WithCredentialsCachedForHostAOnly_SendsNoAuthorizationToHostBOnTheWire_Test()
    {
        var client = TransportAuthTestSupport.ClientSendingThrough(HandlerWithCredentialsFor(_hosts.BaseA));

        TransportAuthTestSupport.WhoAmI(client, _hosts.ServiceA, "SendRequest");

        Assert.IsTrue(_hosts.WireA.Any(record => record.CarriesAuthorization), string.Join(" | ", _hosts.WireA));

        var toB = TransportAuthTestSupport.SendWhoAmI(client, _hosts.ServiceB);

        using var response = Soap12FunctionalSupport.UnwrapRejected(toB, NegativeTestSupport.HttpUnauthorizedCode, "SendRequest");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        TestContext.WriteLine($"wire B: {string.Join(" | ", _hosts.WireB)}");

        Assert.IsTrue(_hosts.WireB.Count > 0);

        Assert.IsFalse(_hosts.WireB.Any(record => record.CarriesAuthorization), string.Join(" | ", _hosts.WireB));
    }

    [TestMethod]
    public void SendRequest_WhenHostARedirectsToHostB_TheRedirectIsNotFollowedAndHostBSeesNothing_Test()
    {
        var client = TransportAuthTestSupport.ClientSendingThrough(HandlerWithCredentialsFor(_hosts.BaseA));

        var built = client.BuildRequest(
            HttpMethod.Post,
            _hosts.RedirectOnAToB,
            new[] { new XElement(Soap12FunctionalSupport.Service + TransportAuthTestSupport.WhoAmIOperation) });

        using var request = Soap12FunctionalSupport.Unwrap(built, "BuildRequest");

        var result = client.SendRequest(request);

        using var response = Soap12FunctionalSupport.UnwrapRejected(result, NegativeTestSupport.HttpRedirectCode, "SendRequest");

        Assert.AreEqual(HttpStatusCode.TemporaryRedirect, response.StatusCode);

        Assert.AreEqual(0, _hosts.WireB.Count, string.Join(" | ", _hosts.WireB));
    }

    private static HttpClientHandler HandlerWithCredentialsFor(Uri exactBase)
    {
        var credentials = new CredentialCache { { exactBase, NegotiateScheme, CredentialCache.DefaultNetworkCredentials } };

        return new HttpClientHandler
        {
            Credentials = credentials,
            UseDefaultCredentials = false,
            AllowAutoRedirect = false,
            PreAuthenticate = false,
            UseProxy = false
        };
    }

    private static HttpClientHandler AnonymousHandler()
        => new()
        {
            UseDefaultCredentials = false,
            AllowAutoRedirect = false,
            UseProxy = false
        };
}
