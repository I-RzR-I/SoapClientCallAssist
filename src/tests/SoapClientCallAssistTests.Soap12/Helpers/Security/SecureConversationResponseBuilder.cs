#nullable disable

using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal sealed class SecureConversationResponseBuilder
{

    private const string Soap12 = "http://www.w3.org/2003/05/soap-envelope";

    private readonly byte[] _secret;

    private readonly string _messageId;

    private string _sctReferenceUri;

    private byte[] _nonce = RandomData.Nonce();

    private int _length = SecureConversationTestSupport.SignatureKeyLength;

    private byte[] _signingKeyOverride;

    private bool _addEncryptedKey;

    private bool _addProperties;

    private readonly List<Func<string, string>> _mutations = new();

    internal SecureConversationResponseBuilder(byte[] secret, string sctIdentifier, string messageId)
    {
        _secret = secret;
        _sctReferenceUri = sctIdentifier;
        _messageId = messageId;
    }

    internal SecureConversationResponseBuilder WithForeignContextReference(string uri)
    {
        _sctReferenceUri = uri;

        return this;
    }

    internal SecureConversationResponseBuilder WithSigningKey(byte[] key)
    {
        _signingKeyOverride = key;

        return this;
    }

    internal SecureConversationResponseBuilder WithEncryptedKey()
    {
        _addEncryptedKey = true;

        return this;
    }

    internal SecureConversationResponseBuilder WithProperties()
    {
        _addProperties = true;

        return this;
    }

    internal SecureConversationResponseBuilder ThenMutate(Func<string, string> mutation)
    {
        _mutations.Add(mutation);

        return this;
    }

    internal string Build()
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.LoadXml(Template());

        var namespaces = WsSecurityTestSupport.Namespaces(document);
        namespaces.AddNamespace("sc", SecureConversationTestSupport.ScFebruary2005Namespace);

        var security = WsSecurityFoundationTestSupport.SecurityHeader(document);

        var signedXml = new SymmetricTestSignedXml(document);
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = WsSecurityTestSupport.HmacSha256Signature;

        foreach (var id in new[] { "body-1", "action-1", "relates-1", "ts-1" })
        {
            var reference = new Reference("#" + id) { DigestMethod = SignedXml.XmlDsigSHA256Url };
            reference.AddTransform(new XmlDsigExcC14NTransform());
            signedXml.AddReference(reference);
        }

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoNode(KeyReference(document)));
        signedXml.KeyInfo = keyInfo;

        var key = _signingKeyOverride ?? SecureConversationTestSupport.DeriveKey(_secret, _nonce, _length);
        using (var hmac = new HMACSHA256(key))
            signedXml.ComputeSignature(hmac);

        security.AppendChild(document.ImportNode(signedXml.GetXml(), true));

        var wire = document.OuterXml;
        foreach (var mutation in _mutations)
            wire = mutation(wire);

        return wire;
    }

    private XmlElement KeyReference(XmlDocument document)
    {
        var tokenReference = document.CreateElement("wsse", "SecurityTokenReference", WsSecurityTestSupport.WsseNamespace);
        var reference = document.CreateElement("wsse", "Reference", WsSecurityTestSupport.WsseNamespace);
        reference.SetAttribute("URI", "#dk-1");
        reference.SetAttribute("ValueType", SecureConversationTestSupport.ScFebruary2005Namespace + "/dk");
        tokenReference.AppendChild(reference);

        return tokenReference;
    }

    private string Template()
    {
        var now = DateTimeOffset.UtcNow;
        var sc = SecureConversationTestSupport.ScFebruary2005Namespace;

        var encryptedKey = _addEncryptedKey
            ? "<xenc:EncryptedKey><xenc:EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p\"/>"
              + "<xenc:CipherData><xenc:CipherValue>" + Convert.ToBase64String(RandomData.Bytes(256)) + "</xenc:CipherValue></xenc:CipherData></xenc:EncryptedKey>"
            : string.Empty;

        var properties = _addProperties ? "<sc:Properties/>" : string.Empty;

        return
            $"<s:Envelope xmlns:s=\"{Soap12}\" xmlns:wsa=\"{WsSecurityFoundationTestSupport.WsAddressing10Namespace}\" xmlns:wsu=\"{WsSecurityTestSupport.WsuNamespace}\">"
            + "<s:Header>"
            + "<wsa:Action s:mustUnderstand=\"1\" wsu:Id=\"action-1\">urn:sc-response</wsa:Action>"
            + $"<wsa:RelatesTo wsu:Id=\"relates-1\">{_messageId}</wsa:RelatesTo>"
            + $"<wsse:Security s:mustUnderstand=\"1\" xmlns:wsse=\"{WsSecurityTestSupport.WsseNamespace}\" xmlns:wsse11=\"{WsSecurityFoundationTestSupport.Wsse11Namespace}\" xmlns:xenc=\"http://www.w3.org/2001/04/xmlenc#\">"
            + $"<wsu:Timestamp wsu:Id=\"ts-1\"><wsu:Created>{Instant(now)}</wsu:Created><wsu:Expires>{Instant(now.AddMinutes(5))}</wsu:Expires></wsu:Timestamp>"
            + encryptedKey
            + $"<sc:DerivedKeyToken wsu:Id=\"dk-1\" xmlns:sc=\"{sc}\">"
            + $"<wsse:SecurityTokenReference><wsse:Reference URI=\"{_sctReferenceUri}\" ValueType=\"{sc}/sct\"/></wsse:SecurityTokenReference>"
            + properties
            + "<sc:Offset>0</sc:Offset>"
            + $"<sc:Length>{_length}</sc:Length>"
            + $"<sc:Nonce>{Convert.ToBase64String(_nonce)}</sc:Nonce>"
            + "</sc:DerivedKeyToken>"
            + "</wsse:Security>"
            + "</s:Header>"
            + $"<s:Body wsu:Id=\"body-1\"><WhoAmIResponse xmlns=\"{SoapAssert.ServiceNs}\"><WhoAmIResult>caller</WhoAmIResult></WhoAmIResponse></s:Body>"
            + "</s:Envelope>";
    }

    private static string Instant(DateTimeOffset instant) => WsSecurityTestSupport.TimestampInstant(instant);
}
