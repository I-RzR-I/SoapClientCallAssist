#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal static class WsSecurityTestSupport
{

    internal const string WsuNamespace =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    internal const string WsseNamespace =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

    internal const string DsNamespace = "http://www.w3.org/2000/09/xmldsig#";

    internal const string X509TokenValueType =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3";

    internal const string ExclusiveC14N = "http://www.w3.org/2001/10/xml-exc-c14n#";

    internal const string InclusiveC14N = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";

    internal const string ExclusiveC14NWithComments = "http://www.w3.org/2001/10/xml-exc-c14n#WithComments";

    internal const string Sha256Digest = "http://www.w3.org/2001/04/xmlenc#sha256";

    internal const string Md5Digest = "http://www.w3.org/2001/04/xmldsig-more#md5";

    internal const string RsaSha256Signature = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

    internal const string HmacSha256Signature = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256";

    internal const string XsltTransform = "http://www.w3.org/TR/1999/REC-xslt-19991116";

    internal const string XPathTransform = "http://www.w3.org/TR/1999/REC-xpath-19991116";

    internal const string EnvelopedSignatureTransform = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";

    internal const string MissingSigningKeyMessage =
        "A signing certificate with an accessible RSA private key is required when message security is enabled.";

    internal const string InsufficientSigningInputMessage =
        "A SOAP envelope, and a valid, unambiguous set of elements to sign (body, timestamp, or additional element ids that resolve to exactly one element), are required.";

    internal const string SelfEnclosingSignatureMessage =
        "The additional signed element id 'env-1' resolves to an element that contains the WS-Security header the signature is placed in; the digest would be taken before the signature is inserted into the very subtree it covers, so no receiver could verify the message.";

    internal const string PostOnlySigningMessage =
        "Signing a SOAP message is only supported for HTTP POST requests.";

    internal const string BodyNotSignedCode = "V-SEC-009";

    internal const string TimestampNotSignedCode = "V-SEC-010";

    internal const string TimestampWindowCode = "V-SEC-012";

    internal const string SignatureCountCode = "V-SEC-006";

    internal const string ReferenceUriCode = "V-SEC-007";

    internal const string AlgorithmCode = "V-SEC-008";

    internal const string SignatureVerificationCode = "ER-SEC-VER";

    internal const string SigningKeyCode = "V-SEC-002";

    internal const string MissingExpectedCertificateCode = "V-SEC-004";

    internal const string MissingSecurityOptionsCode = "V-SEC-013";

    internal const string VerifierFailureCode = "ER-BEC-VRS";

    internal const string Action = "urn:ws-security-tests";

    internal static readonly XNamespace Service = SoapAssert.ServiceNs;

    internal static readonly Uri Endpoint = new("https://ws-security.invalid/Service.svc");

    internal static readonly X509Certificate2 SigningCertificate = CreateCertificate("CN=SoapClientCallAssist.Tests");

    internal static readonly X509Certificate2 OtherCertificate = CreateCertificate("CN=SoapClientCallAssist.Tests.Other");

    internal static readonly X509Certificate2 PublicOnlyCertificate =
        X509CertificateLoader.LoadCertificate(SigningCertificate.Export(X509ContentType.Cert));

    private static X509Certificate2 CreateCertificate(string subject)
        => TestCertificates.SelfSignedRsa(subject);

    internal static XElement DefaultBody() => Body("s1");

    internal static XElement Body(string payload)
        => new(Service + "IsValid", new XElement(Service + "id", payload));

    internal static SoapSecurityDto Security(Action<SoapSecurityDto> configure = null)
    {
        var security = new SoapSecurityDto { Enabled = true, SigningCertificate = SigningCertificate };

        configure?.Invoke(security);

        return security;
    }

    internal static ISoapClientEndpoint Client(SoapProtocolType protocol)
        => protocol == SoapProtocolType.SOAP_1_1
            ? SoapClientFactoryHelper.CreateSoap11Client()
            : SoapClientFactoryHelper.CreateSoap12Client();

    internal static IResult<HttpRequestMessage> Build(
        SoapProtocolType protocol,
        HttpMethod method,
        SoapSecurityDto security,
        params XElement[] bodies)
        => BuildWithHeaders(protocol, method, security, null, bodies);

    internal static IResult<HttpRequestMessage> BuildWithHeaders(
        SoapProtocolType protocol,
        HttpMethod method,
        SoapSecurityDto security,
        IEnumerable<XElement> headers,
        params XElement[] bodies)
        => Client(protocol).BuildRequest(
            method,
            new BuildSoapRequestDto
            {
                Client = new HttpClientDto(Endpoint),
                Envelope = new SoapEnvelopeDto(
                    bodies is { Length: > 0 } ? bodies : new[] { DefaultBody() },
                    headers,
                    Action),
                Security = security
            });

    internal static IResult<HttpRequestMessage> BuildPost(SoapSecurityDto security, params XElement[] bodies)
        => Build(SoapProtocolType.SOAP_1_2, HttpMethod.Post, security, bodies);

    internal static string Wire(IResult<HttpRequestMessage> built, string what)
    {
        Assert.IsTrue(built.IsSuccess, $"{what} | {NegativeTestSupport.Describe(built)}");

        Assert.IsNotNull(built.Response, what);

        using var request = built.Response;

        Assert.IsNotNull(request.Content, what);

        return request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    internal static byte[] WireBytes(IResult<HttpRequestMessage> built, string what)
    {
        Assert.IsTrue(built.IsSuccess, $"{what} | {NegativeTestSupport.Describe(built)}");

        Assert.IsNotNull(built.Response, what);

        using var request = built.Response;

        Assert.IsNotNull(request.Content, what);

        return request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
    }

    internal static string SignedWire(SoapSecurityDto security = null, params XElement[] bodies)
        => Wire(BuildPost(security ?? Security(), bodies), "Build");

    internal static XmlDocument ParseWire(string wire)
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            ConformanceLevel = ConformanceLevel.Document
        };

        using var textReader = new StringReader(wire);
        using var xmlReader = XmlReader.Create(textReader, settings);

        document.Load(xmlReader);

        return document;
    }

    internal static XmlNamespaceManager Namespaces(XmlDocument document)
    {
        var manager = new XmlNamespaceManager(document.NameTable);

        manager.AddNamespace("ds", DsNamespace);
        manager.AddNamespace("wsu", WsuNamespace);
        manager.AddNamespace("wsse", WsseNamespace);

        return manager;
    }

    internal static XmlElement RequireNode(
        XmlNode context, string xpath, XmlNamespaceManager namespaces, string what)
    {
        var node = context.SelectSingleNode(xpath, namespaces) as XmlElement;

        Assert.IsNotNull(node, $"{what} | {xpath}");

        return node;
    }

    internal static byte[] ExclusiveC14NBytes(XmlElement element)
    {
        var transform = new XmlDsigExcC14NTransform();

        transform.LoadInput(element.SelectNodes("descendant-or-self::node() | descendant-or-self::*/@*"));

        using var output = (Stream)transform.GetOutput(typeof(Stream));
        using var buffer = new MemoryStream();

        output.CopyTo(buffer);

        return buffer.ToArray();
    }

    internal static string IndependentExclusiveC14NDigest(XmlElement element)
    {
        using var sha256 = SHA256.Create();

        return Convert.ToBase64String(sha256.ComputeHash(ExclusiveC14NBytes(element)));
    }

    internal static string TimestampInstant(DateTimeOffset instant)
        => instant.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    internal static string WsuId(XmlElement element) => element.GetAttribute("Id", WsuNamespace);

    internal static string SignedBodyId(string wire)
    {
        var document = ParseWire(wire);

        return WsuId(RequireNode(document, "/*/*[local-name()='Body']", Namespaces(document), "signed Body"));
    }
}
