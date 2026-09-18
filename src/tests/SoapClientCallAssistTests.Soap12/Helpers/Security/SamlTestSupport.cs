#nullable disable

using KeyInfo = Microsoft.IdentityModel.Xml.KeyInfo;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Tokens.Saml2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal static class SamlTestSupport
{

    internal const string Saml11Namespace = "urn:oasis:names:tc:SAML:1.0:assertion";

    internal const string Saml20Namespace = "urn:oasis:names:tc:SAML:2.0:assertion";

    internal const string Issuer = "urn:soapclientcallassist:tests:saml-issuer";

    internal const string Audience = "https://ws-security.invalid/Service.svc";

    internal const string SubjectName = "alice.saml";

    internal const string SubjectRole = "soap-callers";

    internal const string SamlIdValueType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLID";

    internal const string SamlAssertionIdValueType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.0#SAMLAssertionID";

    internal const string SamlV20TokenType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0";

    internal const string SamlV11TokenType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1";

    internal const string Bearer20Method = "urn:oasis:names:tc:SAML:2.0:cm:bearer";

    internal const string HolderOfKey20Method = "urn:oasis:names:tc:SAML:2.0:cm:holder-of-key";

    internal const string Bearer11Method = "urn:oasis:names:tc:SAML:1.0:cm:bearer";

    internal const string HolderOfKey11Method = "urn:oasis:names:tc:SAML:1.0:cm:holder-of-key";

    internal const string BearerOverInsecureTransportCode = "V-SEC-083";

    internal const string NotAnAssertionCode = "V-SEC-084";

    internal const string MissingIdCode = "V-SEC-085";

    internal const string AmbiguousIdCode = "V-SEC-086";

    internal const string LapsedCode = "V-SEC-087";

    internal const string SymmetricFamilyCode = "V-SEC-088";

    internal static readonly X509Certificate2 IssuerCertificate =
        CertificateScenarioSupport.CreateRsaCertificate("CN=SoapClientCallAssist.Tests.SamlIssuer", 2048);

    internal static readonly Uri InsecureEndpoint = new("http://ws-security.invalid/Service.svc");

    internal static XmlElement Assertion(Action<SamlIssuance> tune = null)
    {
        var issuance = new SamlIssuance();

        tune?.Invoke(issuance);

        if (issuance.HolderOfKey && issuance.ProofCertificate is null)
            throw new InvalidOperationException("A holder-of-key issuance needs the certificate whose key the assertion names.");

        var document = Load(issuance.Saml20 ? WriteSaml20(issuance) : WriteSaml11(issuance), issuance.PrettyPrint);

        if (issuance.NotOnOrAfterOverride is not null)
            Conditions(document.DocumentElement).SetAttribute("NotOnOrAfter", issuance.NotOnOrAfterOverride);

        if (issuance.Sign)
            SignAssertion(document, issuance.IssuerCertificate, issuance.Saml20);

        return document.DocumentElement;
    }

    internal static XmlElement Bearer20(Action<SamlIssuance> tune = null) => Assertion(tune);

    internal static XmlElement Bearer11(Action<SamlIssuance> tune = null)
        => Assertion(issuance =>
        {
            issuance.Saml20 = false;
            tune?.Invoke(issuance);
        });

    internal static XmlElement HolderOfKey20(X509Certificate2 proof, Action<SamlIssuance> tune = null)
        => Assertion(issuance =>
        {
            issuance.HolderOfKey = true;
            issuance.ProofCertificate = proof;
            tune?.Invoke(issuance);
        });

    internal static XmlElement HolderOfKey11(X509Certificate2 proof, Action<SamlIssuance> tune = null)
        => Assertion(issuance =>
        {
            issuance.Saml20 = false;
            issuance.HolderOfKey = true;
            issuance.ProofCertificate = proof;
            tune?.Invoke(issuance);
        });

    internal static SoapSamlTokenDto Token(
        XmlElement assertion,
        SoapSamlConfirmationType confirmation = SoapSamlConfirmationType.Bearer,
        bool allowInsecureTransport = false)
        => new()
        {
            Assertion = assertion,
            Confirmation = confirmation,
            AllowBearerOverInsecureTransport = allowInsecureTransport
        };

    internal static SoapSamlTokenDto HolderOfKeyToken(X509Certificate2 proof, bool saml20 = true)
        => Token(saml20 ? HolderOfKey20(proof) : HolderOfKey11(proof), SoapSamlConfirmationType.HolderOfKey);

    internal static SoapSecurityDto HolderOfKeySecurity(XmlElement assertion, Action<SoapSecurityDto> configure = null)
        => WsSecurityTestSupport.Security(security =>
        {
            security.SamlToken = Token(assertion, SoapSamlConfirmationType.HolderOfKey);
            configure?.Invoke(security);
        });

    internal static SoapSecurityDto BearerOnlySecurity(XmlElement assertion, bool allowInsecureTransport = false, Action<SoapSecurityDto> configure = null)
    {
        var security = new SoapSecurityDto
        {
            Enabled = true,
            SamlToken = Token(assertion, SoapSamlConfirmationType.Bearer, allowInsecureTransport)
        };

        configure?.Invoke(security);

        return security;
    }

    internal static string IdOf(XmlElement assertion)
        => assertion.NamespaceURI == Saml20Namespace ? assertion.GetAttribute("ID") : assertion.GetAttribute("AssertionID");

    internal static string SignatureValueOf(XmlElement assertion)
        => Descendant(assertion, WsSecurityTestSupport.DsNamespace, "SignatureValue")?.InnerText.Trim();

    internal static string NameIdOf(XmlElement assertion)
        => (Descendant(assertion, assertion.NamespaceURI, "NameID") ?? Descendant(assertion, assertion.NamespaceURI, "NameIdentifier"))?.InnerText.Trim();

    internal static ForbiddenSecret[] Secrets(XmlElement assertion)
    {
        var secrets = new List<ForbiddenSecret> { ForbiddenSecret.OfText("the SAML assertion XML", assertion.OuterXml) };

        var id = IdOf(assertion);

        if (!string.IsNullOrWhiteSpace(id))
            secrets.Add(ForbiddenSecret.OfText("the SAML assertion identifier", id));

        var signatureValue = SignatureValueOf(assertion);

        if (!string.IsNullOrEmpty(signatureValue))
            secrets.Add(ForbiddenSecret.OfText("the SAML assertion SignatureValue", signatureValue));

        var nameId = NameIdOf(assertion);

        if (!string.IsNullOrEmpty(nameId))
            secrets.Add(ForbiddenSecret.OfText("the SAML subject NameID", nameId));

        return secrets.ToArray();
    }

    internal static XmlElement AssertionIn(XmlDocument wire)
    {
        var security = WsSecurityFoundationTestSupport.SecurityHeader(wire);

        var assertions = security.ChildNodes.Cast<XmlNode>().OfType<XmlElement>()
            .Where(element => element.LocalName == "Assertion" && (element.NamespaceURI == Saml20Namespace || element.NamespaceURI == Saml11Namespace))
            .ToList();

        Assert.AreEqual(1, assertions.Count, $"{wire.OuterXml}");

        return assertions[0];
    }

    internal static IReadOnlyList<string> MessageSignatureReferences(XmlDocument wire)
        => wire.SelectNodes("//wsse:Security/ds:Signature/ds:SignedInfo/ds:Reference", WsSecurityTestSupport.Namespaces(wire))
            .Cast<XmlElement>().Select(reference => reference.GetAttribute("URI")).ToList();

    internal static XmlElement AssertionSignature(XmlElement assertion)
        => assertion.ChildNodes.Cast<XmlNode>().OfType<XmlElement>()
            .SingleOrDefault(element => element.LocalName == "Signature" && element.NamespaceURI == WsSecurityTestSupport.DsNamespace);

    internal static bool VerifyIssuerSignature(XmlElement assertion, X509Certificate2 issuer)
    {
        var signature = AssertionSignature(assertion);

        Assert.IsNotNull(signature);

        var signedXml = new SamlAssertionSignedXml(assertion.OwnerDocument);
        signedXml.LoadXml(signature);

        using var key = issuer.GetRSAPublicKey();

        return signedXml.CheckSignature(key);
    }

    internal static XmlElement ThroughLinqToXml(XmlElement assertion)
    {
        var reparsed = XElement.Parse(assertion.OuterXml);

        return Load(reparsed.ToString(), false).DocumentElement;
    }

    internal static XmlElement Conditions(XmlElement assertion)
    {
        var conditions = assertion.ChildNodes.Cast<XmlNode>().OfType<XmlElement>()
            .SingleOrDefault(element => element.LocalName == "Conditions" && element.NamespaceURI == assertion.NamespaceURI);

        Assert.IsNotNull(conditions);

        return conditions;
    }

    internal static XmlElement WrapInSecurityHeader(XmlElement assertion)
    {
        var document = new XmlDocument { PreserveWhitespace = true };
        var security = document.CreateElement("wsse", "Security", WsSecurityTestSupport.WsseNamespace);

        security.AppendChild(document.ImportNode(assertion, true));
        document.AppendChild(security);

        return security;
    }

    internal static XmlElement ForeignElement(string localName, string namespaceUri, string id)
    {
        var document = new XmlDocument { PreserveWhitespace = true };
        var element = document.CreateElement("x", localName, namespaceUri);

        element.SetAttribute("ID", id);
        document.AppendChild(element);

        return element;
    }

    internal static XmlDocument Load(string xml, bool prettyPrint)
    {
        var text = prettyPrint ? PrettyPrint(xml) : xml;

        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };

        document.LoadXml(text);

        return document;
    }

    private static string PrettyPrint(string xml)
    {
        var compact = new XmlDocument { PreserveWhitespace = false, XmlResolver = null };

        compact.LoadXml(xml);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            NewLineChars = "\r\n",
            OmitXmlDeclaration = true,
            Encoding = new UTF8Encoding(false)
        };

        var builder = new StringBuilder();

        using (var writer = XmlWriter.Create(builder, settings))
            compact.Save(writer);

        return builder.ToString();
    }

    private static string WriteSaml20(SamlIssuance issuance)
    {
        var handler = new Saml2SecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };
        var token = (Saml2SecurityToken)handler.CreateToken(Descriptor(issuance));

        var confirmation = new Saml2SubjectConfirmation(new Uri(issuance.HolderOfKey ? HolderOfKey20Method : Bearer20Method));

        if (issuance.HolderOfKey)
            confirmation.SubjectConfirmationData = new Saml2SubjectConfirmationData { KeyInfos = { new KeyInfo(issuance.ProofCertificate) } };

        token.Assertion.Subject.SubjectConfirmations.Clear();
        token.Assertion.Subject.SubjectConfirmations.Add(confirmation);

        return handler.WriteToken(token);
    }

    private static string WriteSaml11(SamlIssuance issuance)
    {
        var handler = new SamlSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };
        var token = (SamlSecurityToken)handler.CreateToken(Descriptor(issuance));

        foreach (var statement in token.Assertion.Statements.OfType<SamlSubjectStatement>())
        {
            statement.Subject.ConfirmationMethods.Clear();
            statement.Subject.ConfirmationMethods.Add(issuance.HolderOfKey ? HolderOfKey11Method : Bearer11Method);

            if (issuance.HolderOfKey)
                statement.Subject.KeyInfo = new KeyInfo(issuance.ProofCertificate);
        }

        return handler.WriteToken(token);
    }

    private static SecurityTokenDescriptor Descriptor(SamlIssuance issuance)
        => new()
        {
            Issuer = Issuer,
            Audience = issuance.Audience,
            IssuedAt = issuance.NotBefore,
            NotBefore = issuance.NotBefore,
            Expires = issuance.NotOnOrAfter,
            Subject = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, issuance.SubjectName),
                    new Claim(ClaimTypes.Name, issuance.SubjectName),
                    new Claim(ClaimTypes.Role, issuance.SubjectRole)
                },
                "SamlTests")
        };

    private static void SignAssertion(XmlDocument document, X509Certificate2 issuer, bool saml20)
    {
        var assertion = document.DocumentElement;

        using var key = issuer.GetRSAPrivateKey();

        var signedXml = new SamlAssertionSignedXml(document) { SigningKey = key };
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

        var reference = new Reference("#" + IdOf(assertion)) { DigestMethod = SignedXml.XmlDsigSHA256Url };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(reference);

        var keyInfo = new System.Security.Cryptography.Xml.KeyInfo();
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

    private static XmlElement Descendant(XmlElement scope, string namespaceUri, string localName)
        => scope.GetElementsByTagName(localName, namespaceUri).Cast<XmlNode>().OfType<XmlElement>().FirstOrDefault();
}
