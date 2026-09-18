#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Service.Common;
using SoapClientCallAssistTests.Wcf.Service.Symmetric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Helpers.Symmetric;

internal static class SymmetricCallSupport
{

    internal static readonly XNamespace Service = SymmetricProbeContract.Namespace;

    internal static XElement WhoAmIBody() => ProbeBodies.WhoAmI(Service);

    internal static XElement EchoBody(string value) => ProbeBodies.Echo(Service, value);

    internal static X509Certificate2 PublicOnly(TestCertificate certificate)
        => new(certificate.Certificate.Export(X509ContentType.Cert));

    internal static SoapSecurityDto CertificateSecurity(SymmetricProbeHost host, TestCertificate clientCertificate, SoapSecureConversationVersionType version, Action<SoapSecurityDto> configure = null)
    {
        var security = new SoapSecurityDto
        {
            SigningCertificate = clientCertificate.Certificate,
            SymmetricBinding = new SoapSymmetricBindingDto
            {
                ServiceCertificate = PublicOnly(host.ServiceCertificate),
                Version = version,
                EndorseWithSigningCertificate = true
            },
            Addressing = new SoapAddressingDto()
        };

        configure?.Invoke(security);

        return security;
    }

    internal static SoapSecurityDto UserNameSecurity(SymmetricProbeHost host, string password, SoapSecureConversationVersionType version, Action<SoapSecurityDto> configure = null)
    {
        var security = new SoapSecurityDto
        {
            UsernameToken = new SoapUsernameTokenDto { Username = SymmetricProbeHost.KnownUserName, Password = password },
            SymmetricBinding = new SoapSymmetricBindingDto
            {
                ServiceCertificate = PublicOnly(host.ServiceCertificate),
                Version = version
            },
            Addressing = new SoapAddressingDto(),
            ResponseSecurity = new SoapResponseSecurityDto { RequireSignatureConfirmation = false }
        };

        configure?.Invoke(security);

        return security;
    }

    internal static HttpRequestMessage Build(ISoapClientEndpoint client, Uri endpoint, XElement body, string action, SoapSecurityDto security)
        => WcfCallSupport.BuildPost(client, endpoint, body, action, security: security);

    internal static async Task<SymmetricExchange> SendAcceptedAsync(ISoapClientEndpoint client, HttpRequestMessage request, string what)
    {
        var requestWire = await request.Content.ReadAsStringAsync();

        var sent = await client.SendRequestAsync(request);

        using var response = WcfCallSupport.Unwrap(sent, what);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, what);

        var responseWire = await response.Content.ReadAsStringAsync();

        return new SymmetricExchange(request, requestWire, responseWire);
    }

    internal static async Task<RejectedCall> SendRejectedAsync(ISoapClientEndpoint client, HttpRequestMessage request, string what)
        => await WcfCallSupport.SendRejectedAsync(client, request, what);

    internal static SoapSignatureVerificationResult VerifyAccepted(SymmetricExchange exchange, string what)
    {
        var verified = new WsSecurityResponseSecurity().Verify(exchange.Request, exchange.ResponseWire);

        Assert.IsTrue(verified.IsSuccess, $"{what} | {ResultLeakSweep.Render(verified)}");

        return verified.Response;
    }

    internal static WcfCallerIdentity ReadWhoAmI(ISoapClientEndpoint client, string responseBody)
        => ProbeReaders.ReadWhoAmI(client, responseBody, Service);

    internal static string ReadEcho(ISoapClientEndpoint client, string responseBody)
        => ProbeReaders.ReadEcho(client, responseBody, Service);

    internal static IEnumerable<string> Secrets(SymmetricProbeHost host, params string[] extra)
    {
        yield return host.ServiceCertificate.Thumbprint;
        yield return host.ClientCertificate.Thumbprint;
        yield return host.UntrustedCertificate.Thumbprint;
        yield return SymmetricProbeHost.KnownPassword;

        foreach (var value in extra)
            yield return value;
    }

    internal static XElement[] UnsignedAddressingHeaders(Uri endpoint, string action)
    {
        XNamespace wsa = "http://www.w3.org/2005/08/addressing";

        return new[]
        {
            new XElement(wsa + "Action", action),
            new XElement(wsa + "MessageID", "urn:uuid:" + Guid.NewGuid().ToString("D")),
            new XElement(wsa + "ReplyTo", new XElement(wsa + "Address", "http://www.w3.org/2005/08/addressing/anonymous")),
            new XElement(wsa + "To", endpoint.AbsoluteUri)
        };
    }

    internal static string SecureConversationNamespace(SoapSecureConversationVersionType version)
        => version == SoapSecureConversationVersionType.December2005
            ? "http://docs.oasis-open.org/ws-sx/ws-secureconversation/200512"
            : "http://schemas.xmlsoap.org/ws/2005/02/sc";

    internal static MessageProtectionOrder DefaultOrder => MessageProtectionOrder.SignBeforeEncryptAndEncryptSignature;
}
