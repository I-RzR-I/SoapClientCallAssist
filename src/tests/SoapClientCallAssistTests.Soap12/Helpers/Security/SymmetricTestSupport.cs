#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal static class SymmetricTestSupport
{

    internal const string XencNamespace = "http://www.w3.org/2001/04/xmlenc#";

    internal const string ScFebruary2005Namespace = "http://schemas.xmlsoap.org/ws/2005/02/sc";

    internal const string ScDecember2005Namespace = "http://docs.oasis-open.org/ws-sx/ws-secureconversation/200512";

    internal const string EncryptedKeyTokenType = "http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKey";

    internal const string EncryptedKeySha1ValueType = "http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKeySHA1";

    internal const string ThumbprintSha1ValueType = "http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#ThumbprintSHA1";

    internal const string RsaOaepMgf1p = "http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p";

    internal const string Sha1Digest = "http://www.w3.org/2000/09/xmldsig#sha1";

    internal const string Aes256Cbc = "http://www.w3.org/2001/04/xmlenc#aes256-cbc";

    internal const string ElementType = "http://www.w3.org/2001/04/xmlenc#Element";

    internal const string DefaultLabel = "WS-SecureConversationWS-SecureConversation";

    internal const string HmacSha1Signature = "http://www.w3.org/2000/09/xmldsig#hmac-sha1";

    internal const string EncryptedKeyNotOursCode = "V-SEC-047";

    internal const string DerivedKeyTokenUnreadableCode = "V-SEC-046";

    internal const string DecryptionCode = "ER-SEC-DEC";

    internal const string ConfirmationCode = "V-SEC-040";

    internal const string ReflectedNonceCode = "V-SEC-041";

    internal const string ReflectedSignatureCode = "V-SEC-042";

    internal const string RelatesToCode = "V-SEC-023";

    internal const string ConsumedCode = "V-SEC-031";

    internal const string NoMaterialCode = "V-SEC-030";

    internal const string EncryptionNotAvailableCode = "V-SEC-050";

    internal static SoapSecurityDto UserNameSecurity(Action<SoapSecurityDto> configure = null)
        => WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
        {
            security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken();
            configure?.Invoke(security);
        });

    internal static SoapSecurityDto CertificateSecurity(Action<SoapSecurityDto> configure = null)
        => WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
        {
            security.SigningCertificate = WsSecurityTestSupport.SigningCertificate;
            security.SymmetricBinding.EndorseWithSigningCertificate = true;
            configure?.Invoke(security);
        });

    internal static HttpRequestMessage BuildRequest(SoapSecurityDto security, params XElement[] bodies)
    {
        var built = WsSecurityTestSupport.BuildPost(security, bodies);

        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));

        return built.Response;
    }

    internal static string Wire(HttpRequestMessage request) => request.Content.ReadAsStringAsync().GetAwaiter().GetResult();

    internal static SymmetricWire Parse(string wire, X509Certificate2 serviceCertificate = null)
        => new(wire, serviceCertificate ?? WsSecurityFoundationTestSupport.ServiceCertificate);

    internal static byte[] UnwrapSecret(byte[] wrapped, X509Certificate2 serviceCertificate)
    {
        using var privateKey = serviceCertificate.GetRSAPrivateKey();

        return privateKey.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA1);
    }

    internal static byte[] PSha1(byte[] secret, byte[] seed, int length)
        => WsTrustKeyDerivation.PSha1(secret, seed, length);

    internal static byte[] DeriveKey(byte[] secret, string label, byte[] nonce, int offset, int length)
        => WsTrustKeyDerivation.DeriveKey(secret, label, nonce, offset, length);

    internal static byte[] ExclusiveC14N(XmlElement element) => WsSecurityTestSupport.ExclusiveC14NBytes(element);

    internal static byte[] IndependentHmac(XmlElement signature, byte[] key, string method)
    {
        var signedInfo = (XmlElement)signature.GetElementsByTagName("SignedInfo", WsSecurityTestSupport.DsNamespace)[0];

        using HMAC hmac = method == HmacSha1Signature ? new HMACSHA1(key) : new HMACSHA256(key);

        return hmac.ComputeHash(ExclusiveC14N(signedInfo));
    }

    internal static bool SignedXmlVerifies(XmlDocument document, XmlElement signature, byte[] key, string method)
    {
        var signedXml = new SymmetricTestSignedXml(document);
        signedXml.LoadXml(signature);

        using HMAC hmac = method == HmacSha1Signature ? new HMACSHA1(key) : new HMACSHA256(key);

        return signedXml.CheckSignature(hmac);
    }

    internal static bool SignedXmlVerifies(XmlDocument document, XmlElement signature, X509Certificate2 certificate)
    {
        var signedXml = new SymmetricTestSignedXml(document);
        signedXml.LoadXml(signature);

        using var publicKey = certificate.GetRSAPublicKey();

        return signedXml.CheckSignature(publicKey);
    }

    internal static XmlDocument DecryptedClone(SymmetricWire wire)
    {
        var clone = (XmlDocument)wire.Document.Clone();
        var security = WsSecurityFoundationTestSupport.SecurityHeader(clone);

        foreach (var encryptedData in ChildrenOf(security, "EncryptedData", XencNamespace).ToList())
            security.ReplaceChild(clone.ImportNode(DecryptElement(encryptedData, wire.EncryptionKey), true), encryptedData);

        return clone;
    }

    internal static bool PrimarySignatureVerifiesAfterDecryption(SymmetricWire wire, string method = WsSecurityTestSupport.HmacSha256Signature)
    {
        var clone = DecryptedClone(wire);
        var security = WsSecurityFoundationTestSupport.SecurityHeader(clone);
        var signature = ChildrenOf(security, "Signature", WsSecurityTestSupport.DsNamespace).Single(candidate => SymmetricWire.SignatureMethod(candidate).Contains("hmac"));

        return SignedXmlVerifies(clone, signature, wire.SignatureKey, method);
    }

    internal static byte[] DecryptCipherValue(byte[] cipherValue, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.ISO10126;

        var iv = cipherValue.Take(16).ToArray();
        var cipherText = cipherValue.Skip(16).ToArray();

        using var decryptor = aes.CreateDecryptor(key, iv);

        return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
    }

    internal static XmlElement DecryptElement(XmlElement encryptedData, byte[] key)
    {
        var cipherValue = Convert.FromBase64String(ChildText(ChildOf(encryptedData, "CipherData", XencNamespace), "CipherValue", XencNamespace));

        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.LoadXml(Encoding.UTF8.GetString(DecryptCipherValue(cipherValue, key)));

        return document.DocumentElement;
    }

    internal static XmlElement ChildOf(XmlElement parent, string localName, string ns)
        => parent.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().SingleOrDefault(element => element.LocalName == localName && element.NamespaceURI == ns);

    internal static string ChildText(XmlElement parent, string localName, string ns) => ChildOf(parent, localName, ns)?.InnerText.Trim();

    internal static IReadOnlyList<XmlElement> ChildrenOf(XmlElement parent, string localName, string ns = null)
        => parent.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Where(element => element.LocalName == localName && (ns is null || element.NamespaceURI == ns)).ToList();

    internal static IReadOnlyList<string> SecurityChildLocalNames(XmlDocument document)
        => WsSecurityFoundationTestSupport.SecurityHeader(document).ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Select(element => element.LocalName).ToList();

    internal static byte[] Sha1(byte[] bytes)
    {
        using var sha1 = SHA1.Create();

        return sha1.ComputeHash(bytes);
    }

    internal static string TimestampInstant(DateTimeOffset instant) => WsSecurityTestSupport.TimestampInstant(instant);

    internal static XmlDocument NewDocument(string xml)
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };

        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        document.Load(reader);

        return document;
    }

    internal static ForbiddenSecret[] ForbiddenValues(SymmetricWire wire, string password = null)
    {
        var forbidden = new List<ForbiddenSecret>
        {
            ForbiddenSecret.OfBytes("the symmetric secret", wire.Secret),
            ForbiddenSecret.OfBytes("the signature derived key", wire.SignatureKey),
            ForbiddenSecret.OfBytes("the encrypted key SHA-1", wire.EncryptedKeySha1)
        };

        if (wire.EncryptionKey is not null)
            forbidden.Add(ForbiddenSecret.OfBytes("the encryption derived key", wire.EncryptionKey));

        foreach (var nonce in wire.Nonces)
            forbidden.Add(ForbiddenSecret.OfBytes("a derived key token nonce", nonce));

        if (password is not null)
            forbidden.Add(ForbiddenSecret.OfText("the username token password", password));

        return forbidden.ToArray();
    }

    internal static string MessageIdOf(XmlDocument document)
        => WsSecurityFoundationTestSupport.HeaderChildren(document).Single(element => element.LocalName == "MessageID").InnerText;

    internal static string ScNamespace(SoapSecureConversationVersionType version)
        => version == SoapSecureConversationVersionType.December2005 ? ScDecember2005Namespace : ScFebruary2005Namespace;

    internal static XmlNamespaceManager Namespaces(XmlDocument document)
    {
        var manager = WsSecurityTestSupport.Namespaces(document);

        manager.AddNamespace("xenc", XencNamespace);
        manager.AddNamespace("wsse11", WsSecurityFoundationTestSupport.Wsse11Namespace);
        manager.AddNamespace("wsa", WsSecurityFoundationTestSupport.WsAddressing10Namespace);
        manager.AddNamespace("sc05", ScFebruary2005Namespace);
        manager.AddNamespace("sc07", ScDecember2005Namespace);

        return manager;
    }

    internal static string Base64Of(SignedXml signedXml) => Convert.ToBase64String(signedXml.SignatureValue);
}
