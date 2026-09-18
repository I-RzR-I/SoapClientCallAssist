#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Wcf.Service.Saml;
using System;
using System.Collections.Generic;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Helpers.Saml;

internal static class SamlCallSupport
{

    internal const string Saml11Namespace = "urn:oasis:names:tc:SAML:1.0:assertion";

    internal const string Saml20Namespace = "urn:oasis:names:tc:SAML:2.0:assertion";

    internal const string DsNamespace = "http://www.w3.org/2000/09/xmldsig#";

    internal const string SubjectName = "alice.saml";

    internal const string SubjectRole = "soap-callers";

    internal static readonly XNamespace Service = SamlProbeContract.Namespace;

    internal static XElement WhoAmIBody() => ProbeBodies.WhoAmI(Service);

    internal static XmlElement Assertion(Action<SamlIssuanceOptions> tune)
    {
        var options = new SamlIssuanceOptions();

        tune(options);

        if (options.IssuerCertificate is null)
            throw new InvalidOperationException("An issuance needs the certificate the assertion is signed with.");

        if (options.Audience is null)
            throw new InvalidOperationException("An issuance needs the audience the assertion is restricted to.");

        var document = Load(Write(options), options.PrettyPrint);

        if (options.Sign)
            SignAssertion(document, options.IssuerCertificate, options.Saml20);

        return document.DocumentElement;
    }

    internal static SoapSecurityDto BearerSecurity(XmlElement assertion, bool includeTimestamp = true)
        => new()
        {
            Enabled = true,
            IncludeTimestamp = includeTimestamp,
            SamlToken = new SoapSamlTokenDto
            {
                Assertion = assertion,
                Confirmation = SoapSamlConfirmationType.Bearer,
                AllowBearerOverInsecureTransport = true
            }
        };

    internal static SoapSecurityDto HolderOfKeySecurity(XmlElement assertion, X509Certificate2 signingCertificate)
        => new()
        {
            Enabled = true,
            SigningCertificate = signingCertificate,
            SignBody = false,
            IncludeTimestamp = true,
            SignTimestamp = true,
            SamlToken = new SoapSamlTokenDto
            {
                Assertion = assertion,
                Confirmation = SoapSamlConfirmationType.HolderOfKey,
                SignAssertion = false
            }
        };

    internal static SamlCallerClaims ReadWhoAmI(ISoapClientEndpoint client, string responseBody)
    {
        var result = ProbeReaders.ReadPayload(client, responseBody).Element(Service + "WhoAmIResult")
            ?? throw new AssertFailedException("The WhoAmI response carries no WhoAmIResult.");

        return new SamlCallerClaims
        {
            AuthenticationType = result.Element(Service + "AuthenticationType")?.Value ?? string.Empty,
            Name = result.Element(Service + "Name")?.Value ?? string.Empty,
            Role = result.Element(Service + "Role")?.Value ?? string.Empty,
            Issuer = result.Element(Service + "Issuer")?.Value ?? string.Empty
        };
    }

    internal static string IdOf(XmlElement assertion)
        => assertion.NamespaceURI == Saml20Namespace ? assertion.GetAttribute("ID") : assertion.GetAttribute("AssertionID");

    internal static IEnumerable<string> Secrets(XmlElement assertion)
    {
        yield return assertion.OuterXml;
        yield return IdOf(assertion);

        var signatureValue = assertion.GetElementsByTagName("SignatureValue", DsNamespace).Cast<XmlElement>().FirstOrDefault()?.InnerText.Trim();

        if (!string.IsNullOrEmpty(signatureValue))
            yield return signatureValue;

        var nameId = (assertion.GetElementsByTagName("NameID", Saml20Namespace).Cast<XmlElement>().FirstOrDefault()
                      ?? assertion.GetElementsByTagName("NameIdentifier", Saml11Namespace).Cast<XmlElement>().FirstOrDefault())?.InnerText.Trim();

        if (!string.IsNullOrEmpty(nameId))
            yield return nameId;
    }

    internal static XmlElement Tamper(XmlElement assertion)
    {
        var attributeValue = assertion.GetElementsByTagName("AttributeValue", assertion.NamespaceURI).Cast<XmlElement>()
            .First(element => element.InnerText == SubjectRole);

        attributeValue.InnerText = "administrators";

        return assertion;
    }

    private static string Write(SamlIssuanceOptions options)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            TokenIssuerName = SamlProbeContract.IssuerName,
            AppliesToAddress = options.Audience,
            Lifetime = new Lifetime(options.NotBefore, options.NotOnOrAfter),
            Subject = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, options.SubjectName),
                    new Claim(ClaimTypes.Name, options.SubjectName),
                    new Claim(ClaimTypes.Role, options.SubjectRole)
                },
                "SamlTests"),
            SigningCredentials = new X509SigningCredentials(options.IssuerCertificate),
            Proof = options.ProofCertificate is null
                ? null
                : new AsymmetricProofDescriptor(new SecurityKeyIdentifier(new X509RawDataKeyIdentifierClause(options.ProofCertificate)))
        };

        SecurityTokenHandler handler = options.Saml20 ? new Saml2SecurityTokenHandler() : new SamlSecurityTokenHandler();

        var token = handler.CreateToken(descriptor);
        var builder = new StringBuilder();

        using (var writer = XmlWriter.Create(builder, new XmlWriterSettings { OmitXmlDeclaration = true }))
            handler.WriteToken(writer, token);

        return builder.ToString();
    }

    private static XmlDocument Load(string xml, bool prettyPrint)
    {
        var compact = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        compact.LoadXml(xml);

        foreach (var signature in compact.DocumentElement.GetElementsByTagName("Signature", DsNamespace).Cast<XmlElement>().ToList())
            signature.ParentNode.RemoveChild(signature);

        if (!prettyPrint)
            return compact;

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            NewLineChars = "\r\n",
            OmitXmlDeclaration = true
        };

        var builder = new StringBuilder();

        using (var writer = XmlWriter.Create(builder, settings))
            compact.Save(writer);

        var pretty = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        pretty.LoadXml(builder.ToString());

        return pretty;
    }

    private static void SignAssertion(XmlDocument document, X509Certificate2 issuer, bool saml20)
    {
        var assertion = document.DocumentElement;

        var signedXml = new SamlAssertionSignedXml(document) { SigningKey = issuer.GetRSAPrivateKey() };
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

        var reference = new Reference("#" + IdOf(assertion)) { DigestMethod = SignedXml.XmlDsigSHA256Url };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(reference);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(issuer));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();

        var signature = document.ImportNode(signedXml.GetXml(), true);

        if (saml20)
        {
            var issuerElement = assertion.ChildNodes.Cast<XmlNode>().OfType<XmlElement>()
                .First(element => element.LocalName == "Issuer" && element.NamespaceURI == Saml20Namespace);

            assertion.InsertAfter(signature, issuerElement);
        }
        else
            assertion.AppendChild(signature);
    }
}
