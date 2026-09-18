#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using SoapClientCallAssistTests.Soap12.Helpers.Wire;
using SoapTestService;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class WsSecuritySignedTransportTests
{

    private const string Action = SoapAssert.ServiceNs + "EchoValue";

    private const string AuthorizationName = "Authorization";

    private const string AuthorizationValue = "Bearer signed-transport";

    private const string UserAgentName = "User-Agent";

    private const string UserAgentValue = "SoapClientCallAssist-Tests/1.0";

    private const string CustomName = "X-Signed-Transport";

    private const string CustomValue = "signed-transport-value";

    private static readonly Dictionary<string, string> CallerHeaders = new()
    {
        { AuthorizationName, AuthorizationValue },
        { UserAgentName, UserAgentValue },
        { CustomName, CustomValue }
    };

    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task SendRequest_WithASignedEnvelope_DeliversBytesWhoseSignatureStillVerifies_Test()
    {
        var recorded = await SendSignedAsync();

        TestContext.WriteLine($"service recorded {recorded.Body.Length} characters of signed envelope");

        WsSecurityAssert.Accepted(
            new WsSecurityMessageVerifier().Verify(recorded.Body, WsSecurityTestSupport.SigningCertificate),
            "");

        var tampered = WsSecurityWireMutator.ReplaceOnce(recorded.Body, ">abc<", ">xyz<");

        WsSecurityAssert.Rejected(
            new WsSecurityMessageVerifier().Verify(tampered, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.SignatureVerificationCode,
            "");
    }

    [TestMethod]
    public async Task SendRequest_WithASignedEnvelope_StillDeliversTheCallerHeaders_Test()
    {
        var recorded = await SendSignedAsync();

        foreach (var header in CallerHeaders)
            Soap12FunctionalSupport.AssertHeaderReceived(
                recorded,
                header.Key,
                header.Value,
                "");
    }

    private static async Task<RecordedRequest> SendSignedAsync()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.Unwrap(
            BuildSigned(client, correlationId), "BuildRequest");

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request), "SendRequestAsync");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{(int)response.StatusCode}");

        return Soap12FunctionalSupport.FindRecorded(correlationId);
    }

    private static IResult<HttpRequestMessage> BuildSigned(ISoapClientEndpoint client, string correlationId)
        => client.BuildRequest(
            HttpMethod.Post,
            new BuildSoapRequestDto
            {
                Client = new HttpClientDto(
                    SoapServiceFixture.ServiceUri,
                    null,
                    false,
                    Soap12FunctionalSupport.CorrelationHeaders(correlationId, CallerHeaders)),
                Envelope = new SoapEnvelopeDto(
                    Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service), null, Action),
                Security = WsSecurityTestSupport.Security()
            });
}
