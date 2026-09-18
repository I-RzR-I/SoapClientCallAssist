#nullable disable

using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal sealed class SymmetricResponseBuilder
{

    private const string Soap12 = "http://www.w3.org/2003/05/soap-envelope";

    private readonly byte[] _secret;

    private readonly List<string> _confirmations = new();

    private readonly List<Func<string, string>> _mutations = new();

    private byte[] _keyIdentifier;

    private string _relatesTo;

    private string _scNamespace = SymmetricTestSupport.ScFebruary2005Namespace;

    private byte[] _nonce = RandomData.Nonce();

    private int _length = 24;

    private int _offset;

    private string _label;

    private bool _writeLength = true;

    private bool _signRelatesTo = true;

    private bool _signConfirmations = true;

    private bool _signTimestamp = true;

    private bool _signBody = true;

    private string _signatureMethod = WsSecurityTestSupport.HmacSha256Signature;

    private string _algorithmAttribute;

    private byte[] _foreignWrappedKey;

    private byte[] _signingKeyOverride;

    private string _keyReferenceValueType;

    private string _keyIdentifierValueType = SymmetricTestSupport.EncryptedKeySha1ValueType;

    private string _bodyPayload = "answer";

    internal SymmetricResponseBuilder(byte[] secret, byte[] encryptedKeySha1)
    {
        _secret = secret;
        _keyIdentifier = encryptedKeySha1;
    }

    internal byte[] Nonce => _nonce;

    internal byte[] DerivedKey => SymmetricTestSupport.DeriveKey(_secret, _label ?? SymmetricTestSupport.DefaultLabel, _nonce, _offset, _length);

    internal SymmetricResponseBuilder RelatesTo(string messageId)
    {
        _relatesTo = messageId;

        return this;
    }

    internal SymmetricResponseBuilder Confirm(string base64SignatureValue)
    {
        _confirmations.Add(base64SignatureValue);

        return this;
    }

    internal SymmetricResponseBuilder WithScNamespace(string ns)
    {
        _scNamespace = ns;

        return this;
    }

    internal SymmetricResponseBuilder WithNonce(byte[] nonce)
    {
        _nonce = nonce;

        return this;
    }

    internal SymmetricResponseBuilder WithLength(int length, bool write = true)
    {
        _length = length;
        _writeLength = write;

        return this;
    }

    internal SymmetricResponseBuilder WithOffset(int offset)
    {
        _offset = offset;

        return this;
    }

    internal SymmetricResponseBuilder WithLabel(string label)
    {
        _label = label;

        return this;
    }

    internal SymmetricResponseBuilder WithKeyIdentifier(byte[] value, string valueType = SymmetricTestSupport.EncryptedKeySha1ValueType)
    {
        _keyIdentifier = value;
        _keyIdentifierValueType = valueType;

        return this;
    }

    internal SymmetricResponseBuilder WithAlgorithmAttribute(string algorithm)
    {
        _algorithmAttribute = algorithm;

        return this;
    }

    internal SymmetricResponseBuilder WithSignatureMethod(string method)
    {
        _signatureMethod = method;

        return this;
    }

    internal SymmetricResponseBuilder WithSigningKey(byte[] key)
    {
        _signingKeyOverride = key;

        return this;
    }

    internal SymmetricResponseBuilder WithKeyReferenceValueType(string valueType)
    {
        _keyReferenceValueType = valueType;

        return this;
    }

    internal SymmetricResponseBuilder WithForeignEncryptedKey(byte[] wrapped)
    {
        _foreignWrappedKey = wrapped;

        return this;
    }

    internal SymmetricResponseBuilder WithBody(string payload)
    {
        _bodyPayload = payload;

        return this;
    }

    internal SymmetricResponseBuilder LeaveRelatesToUnsigned()
    {
        _signRelatesTo = false;

        return this;
    }

    internal SymmetricResponseBuilder LeaveConfirmationsUnsigned()
    {
        _signConfirmations = false;

        return this;
    }

    internal SymmetricResponseBuilder LeaveTimestampUnsigned()
    {
        _signTimestamp = false;

        return this;
    }

    internal SymmetricResponseBuilder LeaveBodyUnsigned()
    {
        _signBody = false;

        return this;
    }

    internal SymmetricResponseBuilder ThenMutate(Func<string, string> mutation)
    {
        _mutations.Add(mutation);

        return this;
    }

    internal string Build()
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.LoadXml(Template());

        var namespaces = SymmetricTestSupport.Namespaces(document);
        var security = WsSecurityFoundationTestSupport.SecurityHeader(document);

        var signedXml = new SymmetricTestSignedXml(document);
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = _signatureMethod;

        var references = new List<string>();

        if (_signBody)
            references.Add("body-1");

        references.Add("action-1");

        if (_relatesTo is not null && _signRelatesTo)
            references.Add("relates-1");

        if (_signTimestamp)
            references.Add("ts-1");

        if (_signConfirmations)
            for (var index = 0; index < _confirmations.Count; index++)
                references.Add($"conf-{index + 1}");

        foreach (var id in references)
        {
            var reference = new Reference("#" + id) { DigestMethod = SignedXml.XmlDsigSHA256Url };
            reference.AddTransform(new XmlDsigExcC14NTransform());
            signedXml.AddReference(reference);
        }

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoNode(KeyReference(document)));
        signedXml.KeyInfo = keyInfo;

        var key = _signingKeyOverride ?? DerivedKey;

        using (HMAC hmac = _signatureMethod == SymmetricTestSupport.HmacSha1Signature ? new HMACSHA1(key) : new HMACSHA256(key))
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
        reference.SetAttribute("ValueType", _keyReferenceValueType ?? _scNamespace + "/dk");
        tokenReference.AppendChild(reference);

        return tokenReference;
    }

    private string Template()
    {
        var now = DateTimeOffset.UtcNow;
        var relatesTo = _relatesTo is null ? string.Empty : $"<wsa:RelatesTo wsu:Id=\"relates-1\">{_relatesTo}</wsa:RelatesTo>";

        var confirmations = string.Empty;

        for (var index = 0; index < _confirmations.Count; index++)
            confirmations += $"<wsse11:SignatureConfirmation wsu:Id=\"conf-{index + 1}\" Value=\"{_confirmations[index]}\"/>";

        var foreignKey = _foreignWrappedKey is null
            ? string.Empty
            : "<xenc:EncryptedKey Id=\"foreign-1\"><xenc:EncryptionMethod Algorithm=\"" + SymmetricTestSupport.RsaOaepMgf1p + "\"/>"
              + "<xenc:CipherData><xenc:CipherValue>" + Convert.ToBase64String(_foreignWrappedKey) + "</xenc:CipherValue></xenc:CipherData></xenc:EncryptedKey>";

        var algorithm = _algorithmAttribute is null ? string.Empty : $" Algorithm=\"{_algorithmAttribute}\"";
        var offset = _offset > 0 || _writeLength ? $"<sc:Offset>{_offset}</sc:Offset>" : string.Empty;
        var length = _writeLength ? $"<sc:Length>{_length}</sc:Length>" : string.Empty;
        var label = _label is null ? string.Empty : $"<sc:Label>{_label}</sc:Label>";

        return
            $"<s:Envelope xmlns:s=\"{Soap12}\" xmlns:wsa=\"{WsSecurityFoundationTestSupport.WsAddressing10Namespace}\" xmlns:wsu=\"{WsSecurityTestSupport.WsuNamespace}\">"
            + "<s:Header>"
            + "<wsa:Action s:mustUnderstand=\"1\" wsu:Id=\"action-1\">urn:ws-security-tests-response</wsa:Action>"
            + relatesTo
            + $"<wsse:Security s:mustUnderstand=\"1\" xmlns:wsse=\"{WsSecurityTestSupport.WsseNamespace}\" xmlns:wsse11=\"{WsSecurityFoundationTestSupport.Wsse11Namespace}\" xmlns:xenc=\"{SymmetricTestSupport.XencNamespace}\">"
            + $"<wsu:Timestamp wsu:Id=\"ts-1\"><wsu:Created>{SymmetricTestSupport.TimestampInstant(now)}</wsu:Created><wsu:Expires>{SymmetricTestSupport.TimestampInstant(now.AddMinutes(5))}</wsu:Expires></wsu:Timestamp>"
            + foreignKey
            + $"<sc:DerivedKeyToken wsu:Id=\"dk-1\"{algorithm} xmlns:sc=\"{_scNamespace}\">"
            + $"<wsse:SecurityTokenReference wsse11:TokenType=\"{SymmetricTestSupport.EncryptedKeyTokenType}\">"
            + $"<wsse:KeyIdentifier ValueType=\"{_keyIdentifierValueType}\" EncodingType=\"{WsSecurityFoundationTestSupport.Base64BinaryEncodingType}\">{Convert.ToBase64String(_keyIdentifier)}</wsse:KeyIdentifier>"
            + "</wsse:SecurityTokenReference>"
            + offset + length + label
            + $"<sc:Nonce>{Convert.ToBase64String(_nonce)}</sc:Nonce>"
            + "</sc:DerivedKeyToken>"
            + confirmations
            + "</wsse:Security>"
            + "</s:Header>"
            + $"<s:Body wsu:Id=\"body-1\"><IsValidResponse xmlns=\"{SoapAssert.ServiceNs}\"><IsValidResult>{_bodyPayload}</IsValidResult></IsValidResponse></s:Body>"
            + "</s:Envelope>";
    }
}
