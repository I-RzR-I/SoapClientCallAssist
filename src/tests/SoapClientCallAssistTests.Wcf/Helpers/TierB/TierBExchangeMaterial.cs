#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Wcf.Helpers.Symmetric;
using SoapClientCallAssistTests.Wcf.Service.TierB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace SoapClientCallAssistTests.Wcf.Helpers.TierB;

internal sealed class TierBExchangeMaterial
{

    private const string DefaultLabel = "WS-SecureConversationWS-SecureConversation";

    private const string XencNamespace = "http://www.w3.org/2001/04/xmlenc#";

    private const string DsNamespace = "http://www.w3.org/2000/09/xmldsig#";

    private const string WsseNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

    private const string WsuNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    private readonly string _responseWire;

    private TierBExchangeMaterial(string responseWire, byte[] secret, XmlElement encryptionToken, XmlElement signatureToken)
    {
        _responseWire = responseWire;
        Secret = secret;
        EncryptionKey = DerivedKey(secret, encryptionToken);
        SignatureKey = signatureToken is null ? null : DerivedKey(secret, signatureToken);
        EncryptionTokenLength = ChildText(encryptionToken, "Length");
        BodyCipherValuePrefix = CipherValueOf(BodyEncryptedData(Load(responseWire))).Substring(0, 40);
    }

    internal byte[] Secret { get; }

    internal byte[] EncryptionKey { get; }

    internal byte[] SignatureKey { get; }

    internal string EncryptionTokenLength { get; }

    internal string BodyCipherValuePrefix { get; }

    internal static TierBExchangeMaterial Of(TierBProbeHost host, SymmetricExchange exchange)
    {
        var request = Load(exchange.RequestWire);
        var response = Load(exchange.ResponseWire);

        var encryptedKey = (XmlElement)request.GetElementsByTagName("EncryptedKey", XencNamespace)[0];
        var wrapped = Convert.FromBase64String(CipherValueOf(encryptedKey));

        byte[] secret;

        using (var privateKey = host.ServiceCertificate.Certificate.GetRSAPrivateKey())
            secret = privateKey.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA1);

        var bodyEncryptedData = BodyEncryptedData(response);
        var encryptionToken = TokenReferencedBy(response, bodyEncryptedData);

        var signatureToken = SecurityHeader(response).ChildNodes.Cast<XmlNode>().OfType<XmlElement>()
            .FirstOrDefault(element => element.LocalName == "DerivedKeyToken" && ReferenceEquals(element, encryptionToken) is false);

        return new TierBExchangeMaterial(exchange.ResponseWire, secret, encryptionToken, signatureToken);
    }

    internal string TamperBodyCipherText()
        => Mutate(document =>
        {
            var cipherValue = CipherValueElement(BodyEncryptedData(document));
            var bytes = Convert.FromBase64String(cipherValue.InnerText);

            bytes[bytes.Length - 24] ^= 0x5A;
            cipherValue.InnerText = Convert.ToBase64String(bytes);
        });

    internal string ReEncryptBody(string plaintext)
        => Mutate(document =>
        {
            using var aes = Aes.Create();
            aes.Key = EncryptionKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.ISO10126;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();

            var bytes = Encoding.UTF8.GetBytes(plaintext);

            CipherValueElement(BodyEncryptedData(document)).InnerText = Convert.ToBase64String(aes.IV.Concat(encryptor.TransformFinalBlock(bytes, 0, bytes.Length)).ToArray());
        });

    internal string WithFreshEncryptionTokenNonce()
        => Mutate(document =>
        {
            var token = TokenReferencedBy(document, BodyEncryptedData(document));
            Child(token, "Nonce").InnerText = Convert.ToBase64String(RandomBytes(16));
        });

    internal string PlantForeignEncryptedKey()
        => Mutate(document =>
        {
            var security = SecurityHeader(document);
            var encryptedKey = document.CreateElement("xenc", "EncryptedKey", XencNamespace);
            var method = document.CreateElement("xenc", "EncryptionMethod", XencNamespace);
            method.SetAttribute("Algorithm", "http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p");
            encryptedKey.AppendChild(method);

            var cipherData = document.CreateElement("xenc", "CipherData", XencNamespace);
            var cipherValue = document.CreateElement("xenc", "CipherValue", XencNamespace);
            cipherValue.InnerText = Convert.ToBase64String(RandomBytes(256));
            cipherData.AppendChild(cipherValue);
            encryptedKey.AppendChild(cipherData);

            security.InsertBefore(encryptedKey, security.FirstChild);
        });

    internal string PlantDuplicateBodyId()
        => Mutate(document =>
        {
            var header = (XmlElement)SecurityHeader(document).ParentNode;
            var planted = document.CreateElement("planted", "http://SoapClientCallAssist.local/planted");

            planted.SetAttribute("Id", BodyEncryptedData(document).GetAttribute("Id"));
            header.AppendChild(planted);
        });

    internal string TamperSignatureValue()
        => Mutate(document =>
        {
            var signature = document.GetElementsByTagName("Signature", DsNamespace).Cast<XmlElement>().Single();
            var value = (XmlElement)signature.GetElementsByTagName("SignatureValue", DsNamespace)[0];
            var bytes = Convert.FromBase64String(value.InnerText);

            bytes[0] ^= 0x5A;
            value.InnerText = Convert.ToBase64String(bytes);
        });

    private string Mutate(Action<XmlDocument> mutation)
    {
        var document = Load(_responseWire);

        mutation(document);

        return document.OuterXml;
    }

    private static XmlDocument Load(string xml)
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.LoadXml(xml);

        return document;
    }

    private static XmlElement SecurityHeader(XmlDocument document)
        => document.GetElementsByTagName("Security", WsseNamespace).Cast<XmlElement>().Single();

    private static XmlElement BodyEncryptedData(XmlDocument document)
    {
        var body = document.DocumentElement.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Single(element => element.LocalName == "Body");
        var encryptedData = body.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().SingleOrDefault(element => element.LocalName == "EncryptedData" && element.NamespaceURI == XencNamespace);

        Assert.IsNotNull(encryptedData);

        return encryptedData;
    }

    private static XmlElement TokenReferencedBy(XmlDocument document, XmlElement encryptedDataOrSignature)
    {
        var keyInfo = (XmlElement)encryptedDataOrSignature.GetElementsByTagName("KeyInfo", DsNamespace)[0];
        var reference = (XmlElement)keyInfo.GetElementsByTagName("Reference", WsseNamespace)[0];
        var id = reference.GetAttribute("URI").Substring(1);

        var token = SecurityHeader(document).ChildNodes.Cast<XmlNode>().OfType<XmlElement>()
            .SingleOrDefault(element => element.LocalName == "DerivedKeyToken" && element.GetAttribute("Id", WsuNamespace) == id);

        Assert.IsNotNull(token, id);

        return token;
    }

    private static XmlElement Child(XmlElement parent, string localName)
        => parent.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().SingleOrDefault(element => element.LocalName == localName);

    private static string ChildText(XmlElement parent, string localName) => Child(parent, localName)?.InnerText.Trim();

    private static XmlElement CipherValueElement(XmlElement encrypted)
        => (XmlElement)encrypted.GetElementsByTagName("CipherValue", XencNamespace)[0];

    private static string CipherValueOf(XmlElement encrypted) => CipherValueElement(encrypted).InnerText.Trim();

    private static byte[] DerivedKey(byte[] secret, XmlElement token)
    {
        var nonce = Convert.FromBase64String(ChildText(token, "Nonce"));
        var length = int.Parse(ChildText(token, "Length") ?? "32");
        var offset = int.Parse(ChildText(token, "Offset") ?? "0");
        var label = ChildText(token, "Label") ?? DefaultLabel;
        var seed = Encoding.UTF8.GetBytes(label).Concat(nonce).ToArray();

        return WsTrustKeyDerivation.PSha1(secret, seed, offset + length).Skip(offset).Take(length).ToArray();
    }

    private static byte[] RandomBytes(int length)
    {
        var bytes = new byte[length];

        using (var generator = RandomNumberGenerator.Create())
            generator.GetBytes(bytes);

        return bytes;
    }
}
