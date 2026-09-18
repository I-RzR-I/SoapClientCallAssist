#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Helpers.Symmetric;
using SoapClientCallAssistTests.Wcf.Service.Common;
using SoapClientCallAssistTests.Wcf.Service.TierB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Security;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Helpers.TierB;

internal static class TierBCallSupport
{

    internal const string DecryptionCode = "ER-SEC-DEC";

    internal const string CipherCapCode = "V-SEC-054";

    internal const string DecryptionNotAllowedCode = "V-SEC-058";

    internal const string EncryptedShapeCode = "V-SEC-059";

    internal const string ConsumedCode = "V-SEC-031";

    internal static readonly XNamespace Service = TierBProbeContract.Namespace;

    internal static MessageProtectionOrder DefaultOrder => MessageProtectionOrder.SignBeforeEncryptAndEncryptSignature;

    internal static XElement WhoAmIBody() => ProbeBodies.WhoAmI(Service);

    internal static XElement EchoBody(string value) => ProbeBodies.Echo(Service, value);

    internal static SoapSecurityDto CertificateSecurity(TierBProbeHost host, SoapSecureConversationVersionType version, Action<SoapSecurityDto> configure = null)
    {
        var security = new SoapSecurityDto
        {
            SigningCertificate = host.ClientCertificate.Certificate,
            SymmetricBinding = new SoapSymmetricBindingDto
            {
                ServiceCertificate = SymmetricCallSupport.PublicOnly(host.ServiceCertificate),
                Version = version,
                EndorseWithSigningCertificate = true
            },
            Addressing = new SoapAddressingDto(),
            Encryption = new SoapEncryptionDto { EncryptBody = true, EncryptSignature = true },
            ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true }
        };

        configure?.Invoke(security);

        return security;
    }

    internal static SoapSecurityDto UserNameSecurity(TierBProbeHost host, string password, SoapSecureConversationVersionType version, Action<SoapSecurityDto> configure = null)
    {
        var security = new SoapSecurityDto
        {
            UsernameToken = new SoapUsernameTokenDto { Username = TierBProbeHost.KnownUserName, Password = password },
            SymmetricBinding = new SoapSymmetricBindingDto
            {
                ServiceCertificate = SymmetricCallSupport.PublicOnly(host.ServiceCertificate),
                Version = version
            },
            Addressing = new SoapAddressingDto(),
            Encryption = new SoapEncryptionDto { EncryptBody = true, EncryptSignature = true },
            ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true, RequireSignatureConfirmation = false }
        };

        configure?.Invoke(security);

        return security;
    }

    internal static HttpRequestMessage Build(ISoapClientEndpoint client, Uri endpoint, XElement body, string action, SoapSecurityDto security)
        => WcfCallSupport.BuildPost(client, endpoint, body, action, security: security);

    internal static string DecryptAccepted(SymmetricExchange exchange, string what)
        => DecryptAccepted(exchange.Request, exchange.ResponseWire, what);

    internal static string DecryptAccepted(HttpRequestMessage request, string responseWire, string what)
    {
        var decrypted = new WsSecurityResponseSecurity().DecryptAndVerify(request, responseWire);

        Assert.IsTrue(decrypted.IsSuccess, $"{what} | {ResultLeakSweep.Render(decrypted)}");
        Assert.IsFalse(string.IsNullOrEmpty(decrypted.Response), what);

        return decrypted.Response;
    }

    internal static void DecryptRefused(HttpRequestMessage request, string responseWire, string expectedCode, IEnumerable<string> secrets, string what)
    {
        var decrypted = new WsSecurityResponseSecurity().DecryptAndVerify(request, responseWire);

        Assert.IsFalse(decrypted.IsSuccess, what);
        Assert.IsNull(decrypted.Response, what);
        Assert.AreEqual(expectedCode, ResultRendering.FirstKey(decrypted), $"{what} | {ResultLeakSweep.Render(decrypted)}");

        ResultLeakSweep.AssertNoSecret(decrypted, secrets, what);
    }

    internal static WcfCallerIdentity ReadWhoAmI(ISoapClientEndpoint client, string plaintextEnvelope)
        => ProbeReaders.ReadWhoAmI(client, plaintextEnvelope, Service);

    internal static string ReadEcho(ISoapClientEndpoint client, string plaintextEnvelope)
        => ProbeReaders.ReadEcho(client, plaintextEnvelope, Service);

    internal static IEnumerable<string> Secrets(TierBProbeHost host, TierBExchangeMaterial material, params string[] extra)
    {
        yield return host.ServiceCertificate.Thumbprint;
        yield return host.ClientCertificate.Thumbprint;
        yield return TierBProbeHost.KnownPassword;

        if (material is not null)
        {
            yield return Convert.ToBase64String(material.Secret);
            yield return Convert.ToBase64String(material.EncryptionKey);

            if (material.SignatureKey is not null)
                yield return Convert.ToBase64String(material.SignatureKey);

            yield return material.BodyCipherValuePrefix;
        }

        foreach (var value in extra)
            yield return value;
    }

    internal static int CountElements(string envelope, string localName)
        => XDocument.Parse(envelope).Descendants().Count(element => element.Name.LocalName == localName);
}
