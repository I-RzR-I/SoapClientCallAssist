#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

[TestClass]
public sealed class ClientHeaderDeliveryTests
{
    private const string AuthorizationName = "Authorization";

    private const string AuthorizationValue = "Bearer client-header-delivery";

    private const string UserAgentName = "User-Agent";

    private const string UserAgentValue = "SoapClientCallAssist-Tests/1.0";

    private const string AcceptName = "Accept";

    private const string AcceptValue = "application/soap+xml";

    private const string CustomName = "X-Custom";

    private const string CustomValue = "custom-value";

    private const string RejectedHeaderText = "could not be applied to the request";

    private const string Action = SoapAssert.ServiceNs + "EchoValue";

    private static readonly Uri Endpoint = new("https://client-headers.invalid/Service.svc");

    private static readonly Dictionary<string, string> CallerHeaders = new()
    {
        { AuthorizationName, AuthorizationValue },
        { UserAgentName, UserAgentValue },
        { AcceptName, AcceptValue },
        { CustomName, CustomValue }
    };

    [DataTestMethod]
    [DataRow(AuthorizationName, AuthorizationValue, DisplayName = "Authorization")]
    [DataRow(UserAgentName, UserAgentValue, DisplayName = "User-Agent")]
    [DataRow(AcceptName, AcceptValue, DisplayName = "Accept")]
    [DataRow(CustomName, CustomValue, DisplayName = "X-Custom")]
    public void BuildRequest_WithACallerSuppliedHeader_PutsItOnTheRequestHeaderCollection_Test(string name, string value)
    {
        using var request = BuildOrFail(Header(name, value), name);

        Assert.IsTrue(request.Headers.TryGetValues(name, out var carried), name);

        CollectionAssert.Contains(carried.ToList(), value, name);
    }

    [TestMethod]
    public void BuildRequest_WithAContentHeader_PutsItOnTheContentHeaderCollection_Test()
    {
        using var request = BuildOrFail(Header("Content-Language", "en-GB"), "BuildRequest");

        Assert.IsFalse(request.Headers.TryGetValues("Content-Language", out _));

        Assert.IsTrue(request.Content.Headers.TryGetValues("Content-Language", out var languages));

        CollectionAssert.Contains(languages.ToList(), "en-GB");
    }

    [TestMethod]
    public void BuildRequest_WithACallerHeader_LeavesTheActionHeadersOnTheContent_Test()
    {
        using var request = BuildOrFail(Header(AuthorizationName, AuthorizationValue), "BuildRequest");

        Assert.IsTrue(request.Content.Headers.TryGetValues("SOAPAction", out _));

        Assert.IsTrue(request.Content.Headers.TryGetValues("Action", out _), "DEFECT-BARE-ACTION-HEADER");
    }

    [TestMethod]
    public void BuildRequest_WithAHeaderNeitherCollectionAccepts_ReportsFailure_Test()
    {
        var built = Build(Header("Invalid Header Name", "value"));

        Assert.IsFalse(built.IsSuccess);

        Soap12FunctionalSupport.AssertFailureMessageContains(built, RejectedHeaderText);
        Soap12FunctionalSupport.AssertFailureMessageContains(built, "Invalid Header Name");
    }

    [TestMethod]
    public void BuildRequest_WithAHeaderCarryingNoValues_ReportsFailure_Test()
    {
        var built = Build(new Dictionary<string, IEnumerable<string>> { { "X-Empty", null } });

        Assert.IsFalse(built.IsSuccess);

        Soap12FunctionalSupport.AssertFailureMessageContains(built, RejectedHeaderText);
        Soap12FunctionalSupport.AssertFailureMessageContains(built, "X-Empty");
    }

    [TestMethod]
    public void BuildRequest_WithAHeaderNeitherCollectionAccepts_DoesNotLeakTheHeaderValue_Test()
    {
        const string secret = "super-secret-credential";

        var built = Build(Header("Invalid Header Name", secret));

        Assert.IsFalse(built.IsSuccess);

        var swept = MessageLeakSweep.Render(built);

        Assert.IsFalse(swept.Contains(secret, StringComparison.OrdinalIgnoreCase), $"{secret} | {swept}");

        SecretLeakAssert.CarriesNoSecret(built, "build");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    public async Task SendRequest_WithCallerSuppliedHeaders_DeliversThemToTheService_Test(SoapProtocolType protocol)
    {
        var client = CrossProtocolSupport.CreateClient(protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            protocol,
            client,
            Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
            correlationId,
            action: Action,
            extraHeaders: CallerHeaders);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync {protocol}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{protocol}");

        var recorded = Soap12FunctionalSupport.FindRecorded(correlationId);

        foreach (var header in CallerHeaders)
        {
            Soap12FunctionalSupport.AssertHeaderReceived(
                recorded,
                header.Key,
                header.Value,
                $"{protocol}");
        }
    }

    private static Dictionary<string, IEnumerable<string>> Header(string name, string value)
        => new() { { name, new[] { value } } };

    private static IResult<HttpRequestMessage> Build(Dictionary<string, IEnumerable<string>> headers)
        => SoapClientFactoryHelper.CreateDirectSoap12Client().BuildRequest(
            HttpMethod.Post,
            Endpoint,
            new[] { WsSecurityTestSupport.DefaultBody() },
            action: Action,
            httpClientHeaders: headers);

    private static HttpRequestMessage BuildOrFail(Dictionary<string, IEnumerable<string>> headers, string what)
        => Soap12FunctionalSupport.Unwrap(Build(headers), what);
}
