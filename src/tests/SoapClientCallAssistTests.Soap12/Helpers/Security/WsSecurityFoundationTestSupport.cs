#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal static class WsSecurityFoundationTestSupport
{

    internal const string Wsse11Namespace = "http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd";

    internal const string WsAddressing10Namespace = "http://www.w3.org/2005/08/addressing";

    internal const string WsAddressingAugust2004Namespace = "http://schemas.xmlsoap.org/ws/2004/08/addressing";

    internal const string PasswordTextType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText";

    internal const string PasswordDigestType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest";

    internal const string Base64BinaryEncodingType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";

    internal const string Username = "alice";

    internal const string Password = "s3cr3t-P@ss";

    internal static readonly X509Certificate2 ServiceCertificate = CertificateScenarioSupport.CreateRsaCertificate("CN=SoapClientCallAssist.Tests.Service", 2048);

    internal static readonly Assembly LibraryAssembly = typeof(WsSecurityMessageSigner).Assembly;

    internal static SoapUsernameTokenDto UsernameToken(Action<SoapUsernameTokenDto> configure = null)
    {
        var token = new SoapUsernameTokenDto { Username = Username, Password = Password };

        configure?.Invoke(token);

        return token;
    }

    internal static SoapAddressingDto Addressing(Action<SoapAddressingDto> configure = null)
    {
        var addressing = new SoapAddressingDto();

        configure?.Invoke(addressing);

        return addressing;
    }

    internal static SoapSymmetricBindingDto SymmetricBinding(Action<SoapSymmetricBindingDto> configure = null)
    {
        var binding = new SoapSymmetricBindingDto { ServiceCertificate = ServiceCertificate };

        configure?.Invoke(binding);

        return binding;
    }

    internal static SoapSecurityDto SymmetricSecurity(Action<SoapSecurityDto> configure = null)
    {
        var security = new SoapSecurityDto
        {
            Enabled = true,
            SymmetricBinding = SymmetricBinding(),
            Addressing = Addressing()
        };

        configure?.Invoke(security);

        return security;
    }

    internal static SoapSecureConversationSession Session(TimeSpan lifetime)
        => new("urn:uuid:" + Guid.NewGuid().ToString("D"), new byte[32], DateTimeOffset.UtcNow.Add(lifetime), SoapSecureConversationVersionType.February2005);

    internal static SoapSecurityDto SecureConversationSecurity(SoapSecureConversationSession session, Action<SoapSecurityDto> configure = null)
    {
        var security = new SoapSecurityDto
        {
            Enabled = true,
            SecureConversation = new SoapSecureConversationDto { Session = session },
            Addressing = Addressing()
        };

        configure?.Invoke(security);

        return security;
    }

    internal static SoapSecurityDto TokenOnlySecurity(Action<SoapSecurityDto> configure = null)
    {
        var security = new SoapSecurityDto { Enabled = true, UsernameToken = UsernameToken() };

        configure?.Invoke(security);

        return security;
    }

    internal static XmlElement SamlAssertion()
    {
        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml("<saml:Assertion xmlns:saml=\"urn:oasis:names:tc:SAML:2.0:assertion\" ID=\"_a1\" Version=\"2.0\"><saml:Issuer>issuer</saml:Issuer></saml:Assertion>");

        return document.DocumentElement;
    }

    internal static string FirstCode(IResult result)
    {
        var messages = NegativeTestSupport.Messages(result);

        Assert.IsTrue(messages.Count > 0);

        return messages[0].Key;
    }

    internal static void RefusedWith(IResult built, string expectedCode, string because, params ForbiddenSecret[] extraForbidden)
    {
        Assert.IsFalse(built.IsSuccess, $"{because} | {NegativeTestSupport.Describe(built)}");

        Assert.AreEqual(expectedCode, FirstCode(built), $"{because} | {NegativeTestSupport.Describe(built)}");

        SecretLeakAssert.CarriesNoSecret(built, expectedCode, extraForbidden);

        (built as IResult<HttpRequestMessage>)?.Response?.Dispose();
    }

    internal static XmlElement SecurityHeader(XmlDocument document)
        => WsSecurityTestSupport.RequireNode(document, "/*/*[local-name()='Header']/wsse:Security", WsSecurityTestSupport.Namespaces(document), "wsse:Security");

    internal static XmlElement UsernameTokenElement(XmlDocument document)
        => WsSecurityTestSupport.RequireNode(document, "//wsse:Security/wsse:UsernameToken", WsSecurityTestSupport.Namespaces(document), "wsse:UsernameToken");

    internal static string ChildText(XmlElement parent, string localName)
    {
        var child = parent.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().SingleOrDefault(element => element.LocalName == localName);

        return child?.InnerText;
    }

    internal static XmlElement Child(XmlElement parent, string localName)
        => parent.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().SingleOrDefault(element => element.LocalName == localName);

    internal static IReadOnlyList<XmlElement> HeaderChildren(XmlDocument document)
    {
        var header = WsSecurityTestSupport.RequireNode(document, "/*/*[local-name()='Header']", WsSecurityTestSupport.Namespaces(document), "Header");

        return header.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().ToList();
    }

    internal static IReadOnlyList<string> SignedReferenceUris(XmlDocument document)
    {
        var references = document.SelectNodes("//ds:Signature/ds:SignedInfo/ds:Reference", WsSecurityTestSupport.Namespaces(document));

        return references.Cast<XmlElement>().Select(reference => reference.GetAttribute("URI")).ToList();
    }

    internal static object KeyMaterialOf(HttpRequestMessage request)
        => request.Options.TryGetValue(new HttpRequestOptionsKey<object>(SoapClientEndpointExtensions.RequestSecurityStateKey), out var material) ? material : null;

    internal static Type LibraryType(string fullName)
    {
        var type = LibraryAssembly.GetType(fullName, false);

        Assert.IsNotNull(type, fullName);

        return type;
    }

    internal static XElement RelatesToHeader(string messageId, string wsuId)
        => new(
            XName.Get("RelatesTo", WsAddressing10Namespace),
            new XAttribute(XNamespace.Xmlns + "wsu", WsSecurityTestSupport.WsuNamespace),
            new XAttribute(XNamespace.Get(WsSecurityTestSupport.WsuNamespace) + "Id", wsuId),
            messageId);

    internal static XElement SignatureConfirmationHeader(string base64Value, string wsuId)
        => new(
            XName.Get("Security", WsSecurityTestSupport.WsseNamespace),
            new XAttribute(XNamespace.Xmlns + "wsse", WsSecurityTestSupport.WsseNamespace),
            new XAttribute(XNamespace.Xmlns + "wsu", WsSecurityTestSupport.WsuNamespace),
            new XElement(
                XName.Get("SignatureConfirmation", Wsse11Namespace),
                new XAttribute(XNamespace.Xmlns + "wsse11", Wsse11Namespace),
                new XAttribute(XNamespace.Get(WsSecurityTestSupport.WsuNamespace) + "Id", wsuId),
                new XAttribute("Value", base64Value)));

    internal static string SignedResponse(X509Certificate2 signingCertificate, IEnumerable<XElement> headers, params string[] additionalSignedIds)
    {
        var security = new SoapSecurityDto
        {
            Enabled = true,
            SigningCertificate = signingCertificate,
            AdditionalSignedElementIds = additionalSignedIds.Length == 0 ? null : additionalSignedIds
        };

        return WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildWithHeaders(SoapProtocolType.SOAP_1_2, HttpMethod.Post, security, headers),
            "Build");
    }
}
