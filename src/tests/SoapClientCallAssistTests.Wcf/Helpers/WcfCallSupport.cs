#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Client;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Wcf.Service.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Helpers;

internal static class WcfCallSupport
{

    internal const string HttpSoapFaultCode = "ER-BEC-HTTP-FLT";

    internal const string BodyFaultCode = "ER-BEC-FLT";

    internal static readonly XNamespace Service = WcfProbeContract.Namespace;

    private static readonly XNamespace Soap11 = "http://schemas.xmlsoap.org/soap/envelope/";

    private static readonly XNamespace Soap12 = "http://www.w3.org/2003/05/soap-envelope";

    private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(20);

    internal static ISoapClientEndpoint CreateClient(SoapProtocolType protocol)
    {
        ISoapClientEndpoint client = protocol == SoapProtocolType.SOAP_1_1 ? new Soap11Client() : new Soap12Client();

        Unwrap(client.SetClientTimeout(NetworkTimeout), "SetClientTimeout");

        return client;
    }

    internal static XNamespace SoapNamespace(SoapProtocolType protocol)
        => protocol == SoapProtocolType.SOAP_1_1 ? Soap11 : Soap12;

    internal static XElement EchoBody(string value) => ProbeBodies.Echo(Service, value);

    internal static XElement WhoAmIBody() => ProbeBodies.WhoAmI(Service);

    internal static HttpRequestMessage BuildPost(
        ISoapClientEndpoint client,
        Uri endpoint,
        XElement body,
        string action,
        IEnumerable<XElement> headers = null,
        SoapSecurityDto security = null)
        => Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                new BuildSoapRequestDto
                {
                    Client = new HttpClientDto(endpoint),
                    Envelope = new SoapEnvelopeDto(new[] { body }, headers, action),
                    Security = security
                }),
            $"BuildRequest({endpoint.AbsolutePath})");

    internal static async Task<string> SendAcceptedAsync(ISoapClientEndpoint client, HttpRequestMessage request, string what)
    {
        using (request)
        {
            var sent = await client.SendRequestAsync(request);

            using var response = Unwrap(sent, what);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, what);

            return await response.Content.ReadAsStringAsync();
        }
    }

    internal static async Task<RejectedCall> SendRejectedAsync(ISoapClientEndpoint client, HttpRequestMessage request, string what)
    {
        using (request)
        {
            var sent = await client.SendRequestAsync(request);

            Assert.IsFalse(sent.IsSuccess, what);

            Assert.AreEqual(
                HttpSoapFaultCode,
                ResultRendering.FirstKey(sent),
                $"{what} | {ResultLeakSweep.Render(sent)}");

            Assert.IsNotNull(sent.Response, what);

            using var response = sent.Response;

            var body = await response.Content.ReadAsStringAsync();
            var checkedBody = client.CheckBodyForFaultCode(body);

            Assert.IsFalse(checkedBody.IsSuccess, what);
            Assert.AreEqual(BodyFaultCode, ResultRendering.FirstKey(checkedBody), what);

            return new RejectedCall(sent, checkedBody, SoapFaultReader.Read(body));
        }
    }

    internal static string ReadEcho(ISoapClientEndpoint client, string responseBody)
        => ProbeReaders.ReadEcho(client, responseBody, Service);

    internal static WcfCallerIdentity ReadWhoAmI(ISoapClientEndpoint client, string responseBody)
        => ProbeReaders.ReadWhoAmI(client, responseBody, Service);

    internal static T Unwrap<T>(IResult<T> result, string what)
    {
        Assert.IsTrue(result.IsSuccess, $"{what} | {ResultLeakSweep.Render(result)}");

        return result.Response;
    }

    internal static void Unwrap(IResult result, string what)
        => Assert.IsTrue(result.IsSuccess, $"{what} | {ResultLeakSweep.Render(result)}");
}
