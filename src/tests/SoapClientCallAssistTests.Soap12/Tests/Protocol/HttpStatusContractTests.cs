#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssistTests.Soap12.Helpers.Hosting;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Transport;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

[TestClass]
public sealed class HttpStatusContractTests
{

    private const string HtmlMediaType = "text/html";

    private const string Soap12MediaType = "application/soap+xml";

    private const string Soap11MediaType = "text/xml";

    private const string Realm = "soap-tests";

    private const string Iis401Page =
        "<!DOCTYPE html><html><head><title>401 - Unauthorized: Access is denied due to invalid credentials.</title></head>"
        + "<body><h2>401 - Unauthorized</h2><h3>You do not have permission to view this directory or page using the "
        + "credentials that you supplied.</h3><pre>   at Microsoft.IIS.Fake.Authenticate() in Fake.cs:line 42</pre></body></html>";

    private const string Iis403Page =
        "<html><head><title>403 - Forbidden: Access is denied.</title></head><body><h2>403.7 - Forbidden: Client "
        + "certificate required.</h2><h3>The page you are attempting to access requires your browser to have a Secure "
        + "Sockets Layer (SSL) client certificate.</h3></body></html>";

    private const string Proxy502Page =
        "<html><head><title>502 Bad Gateway</title></head><body><center><h1>502 Bad Gateway</h1></center><hr>"
        + "<center>nginx/1.25.3</center></body></html>";

    private const string Server500Page =
        "<html><head><title>500 - Internal server error.</title></head><body><h2>500 - Internal server error.</h2>"
        + "<pre>System.NullReferenceException: Object reference not set   at Service.Handle() in Service.cs:line 7</pre></body></html>";

    private const string Soap12FaultEnvelope =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
        + "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\"><soap:Body><soap:Fault>"
        + "<soap:Code><soap:Value>soap:Sender</soap:Value></soap:Code>"
        + "<soap:Reason><soap:Text xml:lang=\"en\">The operation failed on purpose.</soap:Text></soap:Reason>"
        + "</soap:Fault></soap:Body></soap:Envelope>";

    private const string BearerToken = "Bearer redirect-canary-token-7f3a";

    private static readonly Uri Endpoint = new("https://status-contract.invalid/Service.svc");

    private static RedirectHostPair _redirects;

    [ClassInitialize]
    public static async Task StartRedirectHosts(TestContext _) => _redirects = await RedirectHostPair.StartAsync();

    [ClassCleanup]
    public static async Task StopRedirectHosts()
    {
        if (_redirects is not null)
            await _redirects.DisposeAsync();
    }

    [TestMethod]
    public async Task SendRequest_WhenTheServiceAnswers401WithAnHtmlPage_FailsUnderThe401CodeNamingTheChallengeOnly_Test()
    {
        var result = await Send(Canned(HttpStatusCode.Unauthorized, Iis401Page, HtmlMediaType, BasicChallenge()));

        var response = Soap12FunctionalSupport.UnwrapRejected(result, NegativeTestSupport.HttpUnauthorizedCode, "SendRequest");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        var info = NegativeTestSupport.FirstMessageInfo(result);

        StringAssert.Contains(info, $"Basic (realm \"{Realm}\")");

        AssertBodyIsWithheld(result, Iis401Page, "You do not have permission");

        await AssertBodyStillReadable(response, Iis401Page);
    }

    [TestMethod]
    public async Task SendRequest_WhenTheServiceAnswers401WithAnEmptyBody_FailsUnderTheSame401Code_Test()
    {
        var result = await Send(Canned(HttpStatusCode.Unauthorized, string.Empty, null, new AuthenticationHeaderValue("Negotiate")));

        var response = Soap12FunctionalSupport.UnwrapRejected(result, NegativeTestSupport.HttpUnauthorizedCode, "SendRequest");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        var info = NegativeTestSupport.FirstMessageInfo(result);

        StringAssert.Contains(info, "Negotiate");

        SecretLeakAssert.CarriesNoSecret(result, "SendRequest");

        await AssertBodyStillReadable(response, string.Empty);
    }

    [TestMethod]
    public async Task SendRequest_WhenTheServiceAnswers401WithoutAChallenge_StillFailsUnderThe401Code_Test()
    {
        var result = await Send(Canned(HttpStatusCode.Unauthorized, string.Empty, null));

        Soap12FunctionalSupport.UnwrapRejected(result, NegativeTestSupport.HttpUnauthorizedCode, "SendRequest");

        StringAssert.Contains(NegativeTestSupport.FirstMessageInfo(result), "no WWW-Authenticate challenge");

        SecretLeakAssert.CarriesNoSecret(result, "SendRequest");
    }

    [TestMethod]
    public async Task SendRequest_WhenIisRefusesTheClientCertificateWith403_FailsUnderThe403CodeWithoutThePage_Test()
    {
        var result = await Send(Canned(HttpStatusCode.Forbidden, Iis403Page, HtmlMediaType));

        var response = Soap12FunctionalSupport.UnwrapRejected(result, NegativeTestSupport.HttpForbiddenCode, "SendRequest");

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

        AssertBodyIsWithheld(result, Iis403Page, "Client certificate required");

        await AssertBodyStillReadable(response, Iis403Page);
    }

    [DataTestMethod]
    [DataRow(HttpStatusCode.InternalServerError, Soap12MediaType, DisplayName = "SOAP 1.2 fault over 500")]
    [DataRow(HttpStatusCode.InternalServerError, Soap11MediaType, DisplayName = "SOAP 1.1 media type over 500")]
    [DataRow(HttpStatusCode.BadRequest, Soap12MediaType, DisplayName = "SOAP 1.2 Sender fault over 400")]
    public async Task SendRequest_WhenTheServiceAnswersAFaultOverTheSoapHttpBinding_FailsUnderTheFaultCodeAndTheFaultStaysReadable_Test(
        HttpStatusCode status, string mediaType)
    {
        var result = await Send(Canned(status, Soap12FaultEnvelope, mediaType));

        var response = Soap12FunctionalSupport.UnwrapRejected(result, NegativeTestSupport.HttpSoapFaultCode, $"SendRequest {(int)status} | {mediaType}");

        Assert.AreEqual(status, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var fault = SoapAssert.GetBodyChild(body);

        Assert.AreEqual("Fault", fault.Name.LocalName, $"{body}");

        var check = SoapClientFactoryHelper.CreateSoap12Client().CheckBodyForFaultCode(body);

        Assert.IsFalse(check.IsSuccess);

        AssertBodyIsWithheld(result, Soap12FaultEnvelope, "The operation failed on purpose.");
    }

    [TestMethod]
    public async Task SendRequest_WhenTheServiceAnswers500WithAnHtmlPage_FailsUnderThe5xxCodeNotTheFaultCode_Test()
    {
        var result = await Send(Canned(HttpStatusCode.InternalServerError, Server500Page, HtmlMediaType));

        var response = Soap12FunctionalSupport.UnwrapRejected(result, NegativeTestSupport.HttpServerErrorCode, "SendRequest");

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        AssertBodyIsWithheld(result, Server500Page, "NullReferenceException");

        await AssertBodyStillReadable(response, Server500Page);
    }

    [TestMethod]
    public async Task SendRequest_WhenAProxyAnswers502WithAnHtmlPage_FailsUnderThe5xxCodeWithoutThePage_Test()
    {
        var result = await Send(Canned(HttpStatusCode.BadGateway, Proxy502Page, HtmlMediaType));

        var response = Soap12FunctionalSupport.UnwrapRejected(result, NegativeTestSupport.HttpServerErrorCode, "SendRequest");

        Assert.AreEqual(HttpStatusCode.BadGateway, response.StatusCode);

        StringAssert.Contains(NegativeTestSupport.FirstMessageInfo(result), "502");

        AssertBodyIsWithheld(result, Proxy502Page, "nginx");

        await AssertBodyStillReadable(response, Proxy502Page);
    }

    [DataTestMethod]
    [DataRow(HttpStatusCode.NotFound, DisplayName = "404")]
    [DataRow(HttpStatusCode.MethodNotAllowed, DisplayName = "405")]
    [DataRow(HttpStatusCode.UnsupportedMediaType, DisplayName = "415")]
    public async Task SendRequest_WhenTheServiceAnswersAnotherClientError_FailsUnderThe4xxCode_Test(HttpStatusCode status)
    {
        var result = await Send(Canned(status, Iis403Page, HtmlMediaType));

        var response = Soap12FunctionalSupport.UnwrapRejected(result, NegativeTestSupport.HttpClientErrorCode, $"SendRequest {(int)status}");

        Assert.AreEqual(status, response.StatusCode);

        StringAssert.Contains(NegativeTestSupport.FirstMessageInfo(result), ((int)status).ToString());

        AssertBodyIsWithheld(result, Iis403Page, "Client certificate required");
    }

    [DataTestMethod]
    [DataRow(HttpStatusCode.Found, DisplayName = "302 not followed")]
    [DataRow(HttpStatusCode.TemporaryRedirect, DisplayName = "307 not followed")]
    public async Task SendRequest_WhenARedirectReachesTheClientUnfollowed_FailsUnderThe3xxCode_Test(HttpStatusCode status)
    {
        var result = await Send(Canned(status, string.Empty, null, location: "https://elsewhere.invalid/Service.svc?token=leak-canary"));

        var response = Soap12FunctionalSupport.UnwrapRejected(result, NegativeTestSupport.HttpRedirectCode, $"SendRequest {(int)status}");

        Assert.AreEqual(status, response.StatusCode);

        Assert.IsFalse(NegativeTestSupport.FirstMessageInfo(result).Contains("elsewhere.invalid", StringComparison.Ordinal));

        SecretLeakAssert.CarriesNoSecret(result, $"SendRequest {(int)status}");
    }

    [TestMethod]
    public async Task SendRequest_WhenTheServiceAnswers302_PinsThatTheClientFollowsItAsAGetWithoutTheBody_Test()
    {
        _redirects.Reset();

        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = BuildRedirectProbe(client, HttpStatusCode.Found, out var sentEnvelope);

        var result = await client.SendRequestAsync(request);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));

        using var response = result.Response;

        var landed = SingleLanding();

        Assert.AreEqual("GET", landed.Method);
        Assert.AreEqual(string.Empty, landed.Body);
        Assert.AreEqual(_redirects.TargetBase.Authority, landed.Header("Host"));

        Assert.IsNull(landed.Header("Authorization"));

        Assert.AreEqual(_redirects.TargetBase.Authority, response.RequestMessage?.RequestUri?.Authority);

        Assert.IsTrue(sentEnvelope.Length > 0);
    }

    [TestMethod]
    public async Task SendRequest_WhenTheServiceAnswers307_PinsThatTheSignedBodyIsRePostedToTheNewHost_Test()
    {
        _redirects.Reset();

        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = BuildRedirectProbe(client, HttpStatusCode.TemporaryRedirect, out var sentEnvelope);

        var result = await client.SendRequestAsync(request);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));

        using var response = result.Response;

        var landed = SingleLanding();

        Assert.AreEqual("POST", landed.Method);

        Assert.AreEqual(sentEnvelope, landed.Body);

        Assert.AreEqual(_redirects.TargetBase.Authority, landed.Header("Host"));

        Assert.IsNull(landed.Header("Authorization"));

        Assert.IsNotNull(landed.Header(Soap12FunctionalSupport.CorrelationHeader));
    }

    private static HttpRequestMessage BuildRedirectProbe(ISoapClientEndpoint client, HttpStatusCode status, out string sentEnvelope)
    {
        var headers = Soap12FunctionalSupport.CorrelationHeaders(Soap12FunctionalSupport.NewCorrelationId());
        headers.Add("Authorization", new[] { BearerToken });

        var built = client.BuildRequest(
            HttpMethod.Post,
            _redirects.RedirectingTo(status),
            NegativeTestSupport.Bodies("HelloWorld"),
            httpClientHeaders: headers);

        var request = Soap12FunctionalSupport.Unwrap(built, $"BuildRequest {(int)status}");

        sentEnvelope = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        return request;
    }

    private static RedirectTargetRecord SingleLanding()
    {
        var received = _redirects.Received;

        Assert.AreEqual(1, received.Count);
        Assert.AreEqual(RedirectHostPair.LandingPath, received[0].Path);

        return received[0];
    }

    private static async Task<IResult<HttpResponseMessage>> Send(CannedResponseHttpMessageHandler handler)
    {
        var client = SoapClientFactoryHelper.CreateSoap12ClientSendingThrough(handler);

        var built = client.BuildRequest(HttpMethod.Post, Endpoint, NegativeTestSupport.Bodies("HelloWorld"));

        using var request = Soap12FunctionalSupport.Unwrap(built, "BuildRequest");

        var result = await client.SendRequestAsync(request);

        Assert.AreEqual(1, handler.Invocations);

        return result;
    }

    private static CannedResponseHttpMessageHandler Canned(
        HttpStatusCode status, string body, string mediaType, AuthenticationHeaderValue challenge = null, string location = null)
        => new(_ =>
        {
            var response = new HttpResponseMessage(status)
            {
                Content = mediaType is null
                    ? new StringContent(body)
                    : new StringContent(body, Encoding.UTF8, mediaType)
            };

            if (mediaType is null)
                response.Content.Headers.ContentType = null;

            if (challenge is not null)
                response.Headers.WwwAuthenticate.Add(challenge);

            if (location is not null)
                response.Headers.Location = new Uri(location);

            return response;
        });

    private static AuthenticationHeaderValue BasicChallenge() => new("Basic", $"realm=\"{Realm}\", charset=\"UTF-8\"");

    private static void AssertBodyIsWithheld(IResult result, string body, string distinctiveText)
    {
        var swept = MessageLeakSweep.Render(result);

        Assert.IsTrue(body.Contains(distinctiveText, StringComparison.Ordinal));

        Assert.IsFalse(
            swept.Contains(distinctiveText, StringComparison.Ordinal) || swept.Contains("<html", StringComparison.OrdinalIgnoreCase),
            swept);

        SecretLeakAssert.CarriesNoSecret(result, "send");
    }

    private static async Task AssertBodyStillReadable(HttpResponseMessage response, string expectedBody)
    {
        Assert.AreEqual(expectedBody, await response.Content.ReadAsStringAsync());
    }
}
