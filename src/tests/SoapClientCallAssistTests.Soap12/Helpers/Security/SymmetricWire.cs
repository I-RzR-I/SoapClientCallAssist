#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal sealed class SymmetricWire
{

    internal SymmetricWire(string wire, X509Certificate2 serviceCertificate)
    {
        Text = wire;
        Document = WsSecurityTestSupport.ParseWire(wire);
        Security = WsSecurityFoundationTestSupport.SecurityHeader(Document);

        EncryptedKey = SymmetricTestSupport.ChildOf(Security, "EncryptedKey", SymmetricTestSupport.XencNamespace);

        Assert.IsNotNull(EncryptedKey);

        Wrapped = Convert.FromBase64String(SymmetricTestSupport.ChildText(
            SymmetricTestSupport.ChildOf(EncryptedKey, "CipherData", SymmetricTestSupport.XencNamespace), "CipherValue", SymmetricTestSupport.XencNamespace));
        Secret = SymmetricTestSupport.UnwrapSecret(Wrapped, serviceCertificate);
        EncryptedKeySha1 = SymmetricTestSupport.Sha1(Wrapped);

        DerivedKeyTokens = Security.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Where(element => element.LocalName == "DerivedKeyToken").ToList();
        ScNamespace = DerivedKeyTokens.Count > 0 ? DerivedKeyTokens[0].NamespaceURI : null;
        Signatures = SymmetricTestSupport.ChildrenOf(Security, "Signature", WsSecurityTestSupport.DsNamespace);
        EncryptedDatas = SymmetricTestSupport.ChildrenOf(Security, "EncryptedData", SymmetricTestSupport.XencNamespace);
        ReferenceList = SymmetricTestSupport.ChildOf(Security, "ReferenceList", SymmetricTestSupport.XencNamespace);

        PrimarySignature = Signatures.FirstOrDefault(signature => SignatureMethod(signature).Contains("hmac"));
        EndorsingSignature = Signatures.FirstOrDefault(signature => SignatureMethod(signature).Contains("rsa"));

        SignatureToken = DerivedKeyTokens.Count > 0 ? DerivedKeyTokens[0] : null;
        EncryptionToken = DerivedKeyTokens.Count > 1 ? DerivedKeyTokens[1] : null;

        SignatureKey = SignatureToken is null ? null : DerivedKeyOf(SignatureToken);
        EncryptionKey = EncryptionToken is null ? null : DerivedKeyOf(EncryptionToken);
    }

    internal string Text { get; }

    internal XmlDocument Document { get; }

    internal XmlElement Security { get; }

    internal XmlElement EncryptedKey { get; }

    internal byte[] Wrapped { get; }

    internal byte[] Secret { get; }

    internal byte[] EncryptedKeySha1 { get; }

    internal IReadOnlyList<XmlElement> DerivedKeyTokens { get; }

    internal string ScNamespace { get; }

    internal XmlElement SignatureToken { get; }

    internal XmlElement EncryptionToken { get; }

    internal byte[] SignatureKey { get; }

    internal byte[] EncryptionKey { get; }

    internal IReadOnlyList<XmlElement> Signatures { get; }

    internal XmlElement PrimarySignature { get; }

    internal XmlElement EndorsingSignature { get; }

    internal IReadOnlyList<XmlElement> EncryptedDatas { get; }

    internal XmlElement ReferenceList { get; }

    internal IEnumerable<byte[]> Nonces => DerivedKeyTokens.Select(NonceOf);

    internal string EncryptedKeyId => EncryptedKey.GetAttribute("Id");

    internal byte[] NonceOf(XmlElement token) => Convert.FromBase64String(SymmetricTestSupport.ChildText(token, "Nonce", token.NamespaceURI));

    internal int LengthOf(XmlElement token) => int.Parse(SymmetricTestSupport.ChildText(token, "Length", token.NamespaceURI) ?? "32");

    internal int OffsetOf(XmlElement token) => int.Parse(SymmetricTestSupport.ChildText(token, "Offset", token.NamespaceURI) ?? "0");

    internal string LabelOf(XmlElement token) => SymmetricTestSupport.ChildText(token, "Label", token.NamespaceURI) ?? SymmetricTestSupport.DefaultLabel;

    internal byte[] DerivedKeyOf(XmlElement token) => SymmetricTestSupport.DeriveKey(Secret, LabelOf(token), NonceOf(token), OffsetOf(token), LengthOf(token));

    internal string PrimarySignatureValue => SymmetricTestSupport.ChildText(PrimarySignature, "SignatureValue", WsSecurityTestSupport.DsNamespace);

    internal string MessageId => SymmetricTestSupport.MessageIdOf(Document);

    internal static string SignatureMethod(XmlElement signature)
        => ((XmlElement)signature.GetElementsByTagName("SignatureMethod", WsSecurityTestSupport.DsNamespace)[0]).GetAttribute("Algorithm");

    internal static XmlElement TokenReference(XmlElement parent)
        => SymmetricTestSupport.ChildOf(parent, "SecurityTokenReference", WsSecurityTestSupport.WsseNamespace);

    internal static XmlElement KeyInfoReference(XmlElement signatureOrEncryptedData)
    {
        var keyInfo = SymmetricTestSupport.ChildOf(signatureOrEncryptedData, "KeyInfo", WsSecurityTestSupport.DsNamespace);
        var tokenReference = TokenReference(keyInfo);

        return SymmetricTestSupport.ChildOf(tokenReference, "Reference", WsSecurityTestSupport.WsseNamespace)
               ?? SymmetricTestSupport.ChildOf(tokenReference, "KeyIdentifier", WsSecurityTestSupport.WsseNamespace);
    }

    internal static IReadOnlyList<string> ReferenceUris(XmlElement signature)
        => signature.GetElementsByTagName("Reference", WsSecurityTestSupport.DsNamespace).Cast<XmlElement>().Select(reference => reference.GetAttribute("URI")).ToList();
}
