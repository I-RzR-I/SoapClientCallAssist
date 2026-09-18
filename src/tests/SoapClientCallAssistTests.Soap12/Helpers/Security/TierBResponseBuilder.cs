#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal sealed class TierBResponseBuilder
{

    private const string BodyEncryptedDataId = "ed-body";

    private const string SignatureEncryptedDataId = "ed-sig";

    private const string EncryptionTokenId = "dk-2";

    private readonly SymmetricWire _wire;

    private readonly SymmetricResponseBuilder _inner;

    private readonly List<Func<string, string>> _mutations = new();

    private byte[] _encryptionNonce = RandomData.Nonce();

    private string _encryptionNonceText;

    private string _encryptionLabel;

    private int _encryptionLength = 32;

    private bool _writeEncryptionLength = true;

    private string _algorithm = SymmetricTestSupport.Aes256Cbc;

    private bool _writeEncryptionMethod = true;

    private byte[] _encryptionKeyOverride;

    private byte[] _keyIdentifierOverride;

    private bool _encryptSignature = true;

    private bool _encryptConfirmations;

    private byte[] _bodyPlaintextOverride;

    private int? _tamperIndexFromEnd;

    private string _plantedHeaderXml;

    private bool _foreignEncryptedKey;

    private bool _cipherReference;

    private bool _retrievalMethod;

    private bool _x509KeyInfo;

    private bool _unreferencedEncryptedData;

    private bool _omitReferenceList;

    private bool _encryptedDataOutsideSecurity;

    private string _bodyType = TierBTestSupport.ContentType;

    private string _bodyReferenceUri = "#" + BodyEncryptedDataId;

    private string _keyReferenceUri = "#" + EncryptionTokenId;

    internal TierBResponseBuilder(SymmetricWire wire, SymmetricResponseBuilder inner)
    {
        _wire = wire;
        _inner = inner;
    }

    internal SymmetricResponseBuilder Inner => _inner;

    internal byte[] EncryptionKey
        => SymmetricTestSupport.DeriveKey(_wire.Secret, _encryptionLabel ?? SymmetricTestSupport.DefaultLabel, _encryptionNonce, 0, _encryptionLength);

    internal byte[] SignatureKey => _inner.DerivedKey;

    internal string BodyCipherValuePrefix { get; private set; }

    internal TierBResponseBuilder WithEncryptionNonce(byte[] nonce, string text = null)
    {
        _encryptionNonce = nonce;
        _encryptionNonceText = text;

        return this;
    }

    internal TierBResponseBuilder WithEncryptionLabel(string label)
    {
        _encryptionLabel = label;

        return this;
    }

    internal TierBResponseBuilder WithEncryptionLength(int length, bool write = true)
    {
        _encryptionLength = length;
        _writeEncryptionLength = write;

        return this;
    }

    internal TierBResponseBuilder WithAlgorithm(string algorithm)
    {
        _algorithm = algorithm;

        return this;
    }

    internal TierBResponseBuilder WithoutEncryptionMethod()
    {
        _writeEncryptionMethod = false;

        return this;
    }

    internal TierBResponseBuilder WithEncryptionKey(byte[] key)
    {
        _encryptionKeyOverride = key;

        return this;
    }

    internal TierBResponseBuilder WithEncryptionKeyIdentifier(byte[] keyIdentifier)
    {
        _keyIdentifierOverride = keyIdentifier;

        return this;
    }

    internal TierBResponseBuilder LeaveSignatureInClear()
    {
        _encryptSignature = false;

        return this;
    }

    internal TierBResponseBuilder EncryptConfirmations()
    {
        _encryptConfirmations = true;

        return this;
    }

    internal TierBResponseBuilder WithBodyPlaintext(string plaintext) => WithBodyPlaintext(Encoding.UTF8.GetBytes(plaintext));

    internal TierBResponseBuilder WithBodyPlaintext(byte[] plaintext)
    {
        _bodyPlaintextOverride = plaintext;

        return this;
    }

    internal TierBResponseBuilder TamperBodyCipherText(int indexFromEnd)
    {
        _tamperIndexFromEnd = indexFromEnd;

        return this;
    }

    internal TierBResponseBuilder PlantInHeader(string xml)
    {
        _plantedHeaderXml = xml;

        return this;
    }

    internal TierBResponseBuilder WithForeignEncryptedKey()
    {
        _foreignEncryptedKey = true;

        return this;
    }

    internal TierBResponseBuilder WithCipherReference()
    {
        _cipherReference = true;

        return this;
    }

    internal TierBResponseBuilder WithRetrievalMethod()
    {
        _retrievalMethod = true;

        return this;
    }

    internal TierBResponseBuilder WithX509KeyInfo()
    {
        _x509KeyInfo = true;

        return this;
    }

    internal TierBResponseBuilder WithUnreferencedEncryptedData()
    {
        _unreferencedEncryptedData = true;

        return this;
    }

    internal TierBResponseBuilder WithoutReferenceList()
    {
        _omitReferenceList = true;

        return this;
    }

    internal TierBResponseBuilder WithEncryptedDataOutsideSecurity()
    {
        _encryptedDataOutsideSecurity = true;

        return this;
    }

    internal TierBResponseBuilder WithBodyType(string type)
    {
        _bodyType = type;

        return this;
    }

    internal TierBResponseBuilder WithBodyReferenceUri(string uri)
    {
        _bodyReferenceUri = uri;

        return this;
    }

    internal TierBResponseBuilder WithKeyReferenceUri(string uri)
    {
        _keyReferenceUri = uri;

        return this;
    }

    internal TierBResponseBuilder ThenMutate(Func<string, string> mutation)
    {
        _mutations.Add(mutation);

        return this;
    }

    internal string Build()
    {
        var document = SymmetricTestSupport.NewDocument(_inner.Build());
        var security = WsSecurityFoundationTestSupport.SecurityHeader(document);
        var body = TierBTestSupport.Body(document);
        var signatureToken = SymmetricTestSupport.ChildrenOf(security, "DerivedKeyToken")[0];
        var scNamespace = signatureToken.NamespaceURI;

        var encryptionToken = EncryptionTokenElement(document, scNamespace);
        security.InsertAfter(encryptionToken, signatureToken);

        var key = _encryptionKeyOverride ?? EncryptionKey;

        var bodyCipherText = _bodyPlaintextOverride is null
            ? TierBTestSupport.EncryptContent(document, body, key)
            : TierBTestSupport.EncryptWithIv(_bodyPlaintextOverride, key);

        if (_tamperIndexFromEnd.HasValue)
            bodyCipherText[bodyCipherText.Length - 1 - _tamperIndexFromEnd.Value] ^= 0x5A;

        BodyCipherValuePrefix = Convert.ToBase64String(bodyCipherText)[..40];

        var bodyEncryptedData = EncryptedData(document, BodyEncryptedDataId, _bodyType, bodyCipherText);

        while (body.FirstChild is not null)
            body.RemoveChild(body.FirstChild);

        body.AppendChild(bodyEncryptedData);

        var referenceIds = new List<string> { _bodyReferenceUri };

        if (_encryptSignature)
        {
            var signature = SymmetricTestSupport.ChildOf(security, "Signature", WsSecurityTestSupport.DsNamespace);
            var signatureCipherText = TierBTestSupport.EncryptElement(document, signature, key);

            security.ReplaceChild(EncryptedData(document, SignatureEncryptedDataId, TierBTestSupport.ElementType, signatureCipherText), signature);
            referenceIds.Add("#" + SignatureEncryptedDataId);
        }

        if (_encryptConfirmations)
        {
            var index = 0;

            foreach (var confirmation in SymmetricTestSupport.ChildrenOf(security, "SignatureConfirmation", WsSecurityFoundationTestSupport.Wsse11Namespace).ToList())
            {
                var id = $"ed-conf-{++index}";

                security.ReplaceChild(EncryptedData(document, id, TierBTestSupport.ElementType, TierBTestSupport.EncryptElement(document, confirmation, key)), confirmation);
                referenceIds.Add("#" + id);
            }
        }

        if (_unreferencedEncryptedData)
            security.AppendChild(EncryptedData(document, "ed-extra", TierBTestSupport.ElementType, TierBTestSupport.EncryptWithIv(Encoding.UTF8.GetBytes("<x/>"), key)));

        if (_encryptedDataOutsideSecurity)
        {
            var header = (XmlElement)security.ParentNode;
            var stray = EncryptedData(document, "ed-stray", TierBTestSupport.ElementType, TierBTestSupport.EncryptWithIv(Encoding.UTF8.GetBytes("<x/>"), key));

            header.AppendChild(stray);
            referenceIds.Add("#ed-stray");
        }

        if (_omitReferenceList is false)
            security.InsertAfter(ReferenceList(document, referenceIds), encryptionToken);

        if (_foreignEncryptedKey)
            security.InsertBefore(ForeignEncryptedKey(document), signatureToken);

        if (_retrievalMethod)
            security.AppendChild(document.CreateElement("ds", "RetrievalMethod", WsSecurityTestSupport.DsNamespace));

        if (_plantedHeaderXml is not null)
        {
            var header = (XmlElement)security.ParentNode;
            var planted = SymmetricTestSupport.NewDocument(_plantedHeaderXml).DocumentElement;

            header.InsertBefore(document.ImportNode(planted, true), security);
        }

        var wire = document.OuterXml;

        foreach (var mutation in _mutations)
            wire = mutation(wire);

        return wire;
    }

    private XmlElement EncryptionTokenElement(XmlDocument document, string scNamespace)
    {
        var token = document.CreateElement("sc", "DerivedKeyToken", scNamespace);
        token.SetAttribute("Id", WsSecurityTestSupport.WsuNamespace, EncryptionTokenId);

        var tokenReference = document.CreateElement("wsse", "SecurityTokenReference", WsSecurityTestSupport.WsseNamespace);
        tokenReference.SetAttribute("TokenType", WsSecurityFoundationTestSupport.Wsse11Namespace, SymmetricTestSupport.EncryptedKeyTokenType);

        var identifier = document.CreateElement("wsse", "KeyIdentifier", WsSecurityTestSupport.WsseNamespace);
        identifier.SetAttribute("ValueType", SymmetricTestSupport.EncryptedKeySha1ValueType);
        identifier.SetAttribute("EncodingType", WsSecurityFoundationTestSupport.Base64BinaryEncodingType);
        identifier.InnerText = Convert.ToBase64String(_keyIdentifierOverride ?? _wire.EncryptedKeySha1);

        tokenReference.AppendChild(identifier);
        token.AppendChild(tokenReference);

        AppendText(document, token, "sc", "Offset", scNamespace, "0");

        if (_writeEncryptionLength)
            AppendText(document, token, "sc", "Length", scNamespace, _encryptionLength.ToString());

        if (_encryptionLabel is not null)
            AppendText(document, token, "sc", "Label", scNamespace, _encryptionLabel);

        AppendText(document, token, "sc", "Nonce", scNamespace, _encryptionNonceText ?? Convert.ToBase64String(_encryptionNonce));

        return token;
    }

    private XmlElement EncryptedData(XmlDocument document, string id, string type, byte[] cipherText)
    {
        var encryptedData = document.CreateElement("xenc", "EncryptedData", SymmetricTestSupport.XencNamespace);
        encryptedData.SetAttribute("Id", id);
        encryptedData.SetAttribute("Type", type);

        if (_writeEncryptionMethod)
        {
            var method = document.CreateElement("xenc", "EncryptionMethod", SymmetricTestSupport.XencNamespace);
            method.SetAttribute("Algorithm", _algorithm);
            encryptedData.AppendChild(method);
        }

        var keyInfo = document.CreateElement("ds", "KeyInfo", WsSecurityTestSupport.DsNamespace);

        if (_x509KeyInfo)
        {
            var x509Data = document.CreateElement("ds", "X509Data", WsSecurityTestSupport.DsNamespace);
            var certificate = document.CreateElement("ds", "X509Certificate", WsSecurityTestSupport.DsNamespace);
            certificate.InnerText = Convert.ToBase64String(WsSecurityFoundationTestSupport.ServiceCertificate.RawData);
            x509Data.AppendChild(certificate);
            keyInfo.AppendChild(x509Data);
        }
        else
        {
            var tokenReference = document.CreateElement("wsse", "SecurityTokenReference", WsSecurityTestSupport.WsseNamespace);
            var reference = document.CreateElement("wsse", "Reference", WsSecurityTestSupport.WsseNamespace);
            reference.SetAttribute("URI", _keyReferenceUri);
            reference.SetAttribute("ValueType", SymmetricTestSupport.ScFebruary2005Namespace + "/dk");
            tokenReference.AppendChild(reference);
            keyInfo.AppendChild(tokenReference);
        }

        encryptedData.AppendChild(keyInfo);

        var cipherData = document.CreateElement("xenc", "CipherData", SymmetricTestSupport.XencNamespace);

        if (_cipherReference)
        {
            var cipherReference = document.CreateElement("xenc", "CipherReference", SymmetricTestSupport.XencNamespace);
            cipherReference.SetAttribute("URI", "#" + id);
            cipherData.AppendChild(cipherReference);
        }
        else
        {
            var cipherValue = document.CreateElement("xenc", "CipherValue", SymmetricTestSupport.XencNamespace);
            cipherValue.InnerText = Convert.ToBase64String(cipherText);
            cipherData.AppendChild(cipherValue);
        }

        encryptedData.AppendChild(cipherData);

        return encryptedData;
    }

    private static XmlElement ReferenceList(XmlDocument document, IEnumerable<string> uris)
    {
        var referenceList = document.CreateElement("xenc", "ReferenceList", SymmetricTestSupport.XencNamespace);

        foreach (var uri in uris)
        {
            var reference = document.CreateElement("xenc", "DataReference", SymmetricTestSupport.XencNamespace);
            reference.SetAttribute("URI", uri);
            referenceList.AppendChild(reference);
        }

        return referenceList;
    }

    private static XmlElement ForeignEncryptedKey(XmlDocument document)
    {
        var encryptedKey = document.CreateElement("xenc", "EncryptedKey", SymmetricTestSupport.XencNamespace);
        encryptedKey.SetAttribute("Id", "ek-foreign");

        var method = document.CreateElement("xenc", "EncryptionMethod", SymmetricTestSupport.XencNamespace);
        method.SetAttribute("Algorithm", SymmetricTestSupport.RsaOaepMgf1p);
        encryptedKey.AppendChild(method);

        var cipherData = document.CreateElement("xenc", "CipherData", SymmetricTestSupport.XencNamespace);
        var cipherValue = document.CreateElement("xenc", "CipherValue", SymmetricTestSupport.XencNamespace);
        cipherValue.InnerText = Convert.ToBase64String(RandomData.Bytes(256));
        cipherData.AppendChild(cipherValue);
        encryptedKey.AppendChild(cipherData);

        return encryptedKey;
    }

    private static void AppendText(XmlDocument document, XmlElement parent, string prefix, string localName, string ns, string value)
    {
        var child = document.CreateElement(prefix, localName, ns);
        child.InnerText = value;
        parent.AppendChild(child);
    }
}
