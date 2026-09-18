#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Linq;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Symmetric;

[TestClass]
public sealed class SymmetricWireContractTests
{

    private static readonly string[] UserNameOrder = { "Timestamp", "EncryptedKey", "DerivedKeyToken", "DerivedKeyToken", "ReferenceList", "EncryptedData", "Signature" };

    private static readonly string[] CertificateOrder = { "Timestamp", "EncryptedKey", "DerivedKeyToken", "BinarySecurityToken", "Signature", "Signature" };

    [TestMethod]
    public void Build_UserNameCredential_EmitsTheSecurityHeaderChildrenInTheOrderWcfAccepts_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());

        CollectionAssert.AreEqual(UserNameOrder, SymmetricTestSupport.SecurityChildLocalNames(SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request)).Document).ToList());
    }

    [TestMethod]
    public void Build_CertificateCredential_EmitsTheSecurityHeaderChildrenInTheOrderWcfAccepts_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.CertificateSecurity());

        CollectionAssert.AreEqual(CertificateOrder, SymmetricTestSupport.SecurityChildLocalNames(SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request)).Document).ToList());
    }

    [TestMethod]
    public void Build_EncryptedKey_IsRsaOaepOverSha1ForTheServiceThumbprintAndUnwrapsToAFresh32ByteSecret_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var method = SymmetricTestSupport.ChildOf(wire.EncryptedKey, "EncryptionMethod", SymmetricTestSupport.XencNamespace);

        Assert.AreEqual(SymmetricTestSupport.RsaOaepMgf1p, method.GetAttribute("Algorithm"));
        Assert.AreEqual(SymmetricTestSupport.Sha1Digest, SymmetricTestSupport.ChildOf(method, "DigestMethod", WsSecurityTestSupport.DsNamespace).GetAttribute("Algorithm"));
        Assert.IsFalse(string.IsNullOrEmpty(wire.EncryptedKeyId));

        var identifier = SymmetricWire.KeyInfoReference(wire.EncryptedKey);

        Assert.AreEqual("KeyIdentifier", identifier.LocalName);
        Assert.AreEqual(SymmetricTestSupport.ThumbprintSha1ValueType, identifier.GetAttribute("ValueType"));
        Assert.AreEqual(WsSecurityFoundationTestSupport.Base64BinaryEncodingType, identifier.GetAttribute("EncodingType"));
        Assert.AreEqual(Convert.ToBase64String(SymmetricTestSupport.Sha1(WsSecurityFoundationTestSupport.ServiceCertificate.RawData)), identifier.InnerText);

        Assert.AreEqual(32, wire.Secret.Length);
        Assert.AreEqual(256, wire.Wrapped.Length);
    }

    [TestMethod]
    public void Build_TwoBuilds_MintDistinctSecretsNoncesAndInitialisationVectors_Test()
    {
        using var first = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        using var second = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());

        var one = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(first));
        var two = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(second));

        CollectionAssert.AreNotEqual(one.Secret, two.Secret);
        Assert.AreEqual(4, one.Nonces.Concat(two.Nonces).Select(Convert.ToBase64String).Distinct().Count());

        var firstIv = CipherValue(one.EncryptedDatas[0]).Take(16).ToArray();
        var secondIv = CipherValue(two.EncryptedDatas[0]).Take(16).ToArray();

        CollectionAssert.AreNotEqual(firstIv, secondIv);
    }

    [DataTestMethod]
    [DataRow(SoapSecureConversationVersionType.February2005, SymmetricTestSupport.ScFebruary2005Namespace)]
    [DataRow(SoapSecureConversationVersionType.December2005, SymmetricTestSupport.ScDecember2005Namespace)]
    public void Build_DerivedKeyTokens_ReferenceTheEncryptedKeyAndStateBothLengths_Test(SoapSecureConversationVersionType version, string ns)
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security => security.SymmetricBinding.Version = version));
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Assert.AreEqual(2, wire.DerivedKeyTokens.Count);
        Assert.AreEqual(ns, wire.ScNamespace);

        foreach (var token in wire.DerivedKeyTokens)
        {
            var tokenReference = SymmetricWire.TokenReference(token);
            var reference = SymmetricTestSupport.ChildOf(tokenReference, "Reference", WsSecurityTestSupport.WsseNamespace);

            Assert.AreEqual(SymmetricTestSupport.EncryptedKeyTokenType, tokenReference.GetAttribute("TokenType", WsSecurityFoundationTestSupport.Wsse11Namespace));
            Assert.AreEqual("#" + wire.EncryptedKeyId, reference.GetAttribute("URI"));
            Assert.AreEqual(SymmetricTestSupport.EncryptedKeyTokenType, reference.GetAttribute("ValueType"));
            Assert.AreEqual(0, wire.OffsetOf(token));
            Assert.AreEqual(16, wire.NonceOf(token).Length);
            Assert.IsNull(SymmetricTestSupport.ChildOf(token, "Label", ns));
            Assert.IsFalse(string.IsNullOrEmpty(token.GetAttribute("Id", WsSecurityTestSupport.WsuNamespace)));
        }

        Assert.AreEqual(24, wire.LengthOf(wire.SignatureToken));
        Assert.AreEqual(32, wire.LengthOf(wire.EncryptionToken));
    }

    [TestMethod]
    public void Build_PrimarySignature_IsHmacSha256UnderTheSignatureDerivedKeyAndCoversBodyTimestampTokenAndAddressing_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Assert.AreEqual(WsSecurityTestSupport.HmacSha256Signature, SymmetricWire.SignatureMethod(wire.PrimarySignature));

        var independent = SymmetricTestSupport.IndependentHmac(wire.PrimarySignature, wire.SignatureKey, WsSecurityTestSupport.HmacSha256Signature);

        Assert.AreEqual(Convert.ToBase64String(independent), wire.PrimarySignatureValue);
        Assert.IsTrue(SymmetricTestSupport.PrimarySignatureVerifiesAfterDecryption(wire));

        var keyReference = SymmetricWire.KeyInfoReference(wire.PrimarySignature);

        Assert.AreEqual("#" + wire.SignatureToken.GetAttribute("Id", WsSecurityTestSupport.WsuNamespace), keyReference.GetAttribute("URI"));
        Assert.AreEqual(wire.ScNamespace + "/dk", keyReference.GetAttribute("ValueType"));

        var decrypted = SymmetricTestSupport.DecryptedClone(wire);

        var signedLocalNames = SymmetricWire.ReferenceUris(wire.PrimarySignature)
            .Select(uri => new SymmetricTestSignedXml(decrypted).GetIdElement(decrypted, uri.Substring(1)).LocalName)
            .ToList();

        CollectionAssert.AreEquivalent(new[] { "Body", "Timestamp", "UsernameToken", "Action", "MessageID", "ReplyTo", "To" }, signedLocalNames);
    }

    [TestMethod]
    public void Build_CertificateCredential_EndorsesThePrimarySignatureWithTheSigningCertificate_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.CertificateSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Assert.IsNotNull(wire.EndorsingSignature);
        Assert.AreEqual(WsSecurityTestSupport.RsaSha256Signature, SymmetricWire.SignatureMethod(wire.EndorsingSignature));
        Assert.AreEqual(1, wire.DerivedKeyTokens.Count);
        Assert.IsNull(wire.ReferenceList);

        var primaryId = wire.PrimarySignature.GetAttribute("Id");

        Assert.IsFalse(string.IsNullOrEmpty(primaryId));
        CollectionAssert.AreEqual(new[] { "#" + primaryId }, SymmetricWire.ReferenceUris(wire.EndorsingSignature).ToList());
        Assert.IsTrue(SymmetricTestSupport.SignedXmlVerifies(wire.Document, wire.EndorsingSignature, WsSecurityTestSupport.SigningCertificate));
        Assert.IsTrue(SymmetricTestSupport.PrimarySignatureVerifiesAfterDecryption(wire));

        var bst = SymmetricTestSupport.ChildOf(wire.Security, "BinarySecurityToken", WsSecurityTestSupport.WsseNamespace);

        Assert.AreEqual("#" + bst.GetAttribute("Id", WsSecurityTestSupport.WsuNamespace), SymmetricWire.KeyInfoReference(wire.EndorsingSignature).GetAttribute("URI"));
        Assert.AreEqual(Convert.ToBase64String(WsSecurityTestSupport.SigningCertificate.RawData), bst.InnerText);
    }

    [TestMethod]
    public void Build_UserNameToken_IsSignedThenEncryptedInPlaceUnderTheEncryptionDerivedKeyAndNeverTravelsInClear_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Assert.IsFalse(wire.Text.Contains(WsSecurityFoundationTestSupport.Password));
        Assert.IsFalse(wire.Text.Contains("UsernameToken"));

        var encryptedData = wire.EncryptedDatas.Single();

        Assert.AreEqual(SymmetricTestSupport.ElementType, encryptedData.GetAttribute("Type"));
        Assert.AreEqual(SymmetricTestSupport.Aes256Cbc, SymmetricTestSupport.ChildOf(encryptedData, "EncryptionMethod", SymmetricTestSupport.XencNamespace).GetAttribute("Algorithm"));

        var keyReference = SymmetricWire.KeyInfoReference(encryptedData);

        Assert.AreEqual("#" + wire.EncryptionToken.GetAttribute("Id", WsSecurityTestSupport.WsuNamespace), keyReference.GetAttribute("URI"));
        Assert.AreEqual(wire.ScNamespace + "/dk", keyReference.GetAttribute("ValueType"));

        var dataReferences = SymmetricTestSupport.ChildrenOf(wire.ReferenceList, "DataReference", SymmetricTestSupport.XencNamespace);

        CollectionAssert.AreEqual(new[] { "#" + encryptedData.GetAttribute("Id") }, dataReferences.Select(reference => reference.GetAttribute("URI")).ToList());

        var token = SymmetricTestSupport.DecryptElement(encryptedData, wire.EncryptionKey);

        Assert.AreEqual("UsernameToken", token.LocalName);
        Assert.AreEqual(WsSecurityTestSupport.WsseNamespace, token.NamespaceURI);
        Assert.AreEqual(WsSecurityFoundationTestSupport.Username, WsSecurityFoundationTestSupport.ChildText(token, "Username"));
        Assert.AreEqual(WsSecurityFoundationTestSupport.Password, WsSecurityFoundationTestSupport.ChildText(token, "Password"));
        Assert.AreEqual(WsSecurityFoundationTestSupport.PasswordTextType, WsSecurityFoundationTestSupport.Child(token, "Password").GetAttribute("Type"));

        var tokenId = token.GetAttribute("Id", WsSecurityTestSupport.WsuNamespace);

        Assert.IsTrue(SymmetricWire.ReferenceUris(wire.PrimarySignature).Contains("#" + tokenId));
    }

    [TestMethod]
    public void Build_WithEncryptSignature_EncryptsThePrimarySignatureInPlaceAfterSigning_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security =>
            security.Encryption = new SoapEncryptionDto { EncryptSignature = true }));
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Assert.AreEqual(0, wire.Signatures.Count);
        Assert.AreEqual(2, wire.EncryptedDatas.Count);

        var dataReferences = SymmetricTestSupport.ChildrenOf(wire.ReferenceList, "DataReference", SymmetricTestSupport.XencNamespace).Select(reference => reference.GetAttribute("URI")).ToList();

        CollectionAssert.AreEquivalent(wire.EncryptedDatas.Select(data => "#" + data.GetAttribute("Id")).ToList(), dataReferences);

        var decrypted = wire.EncryptedDatas.Select(data => SymmetricTestSupport.DecryptElement(data, wire.EncryptionKey)).ToList();
        var signature = decrypted.Single(element => element.LocalName == "Signature");
        var token = decrypted.Single(element => element.LocalName == "UsernameToken");

        Assert.AreEqual(WsSecurityTestSupport.HmacSha256Signature, SymmetricWire.SignatureMethod(signature));
        Assert.AreEqual(WsSecurityFoundationTestSupport.Username, WsSecurityFoundationTestSupport.ChildText(token, "Username"));

        var signedInfoHmac = SymmetricTestSupport.IndependentHmac(signature, wire.SignatureKey, WsSecurityTestSupport.HmacSha256Signature);

        Assert.AreEqual(Convert.ToBase64String(signedInfoHmac), SymmetricTestSupport.ChildText(signature, "SignatureValue", WsSecurityTestSupport.DsNamespace));
    }

    [TestMethod]
    public void Build_WithACustomLabel_WritesTheLabelAndDerivesUnderIt_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security => security.SymmetricBinding.KeyDerivationLabel = "custom-label"));
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Assert.AreEqual("custom-label", SymmetricTestSupport.ChildText(wire.SignatureToken, "Label", wire.ScNamespace));
        Assert.IsTrue(SymmetricTestSupport.PrimarySignatureVerifiesAfterDecryption(wire));
    }

    [TestMethod]
    public void Build_WithHmacSha1AndTheSha1OptIn_EmitsHmacSha1_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security =>
        {
            security.SymmetricBinding.SignatureAlgorithm = SoapSymmetricSignatureAlgorithmType.HmacSha1;
            security.ResponseVerificationPolicy = new SoapVerificationPolicyDto { AllowSha1Algorithms = true };
        }));
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Assert.AreEqual(SymmetricTestSupport.HmacSha1Signature, SymmetricWire.SignatureMethod(wire.PrimarySignature));
        Assert.IsTrue(SymmetricTestSupport.PrimarySignatureVerifiesAfterDecryption(wire, SymmetricTestSupport.HmacSha1Signature));
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1)]
    [DataRow(SoapProtocolType.SOAP_1_2)]
    public void Build_BothProtocols_ProduceAVerifiablePrimarySignature_Test(SoapProtocolType protocol)
    {
        var built = WsSecurityTestSupport.Build(protocol, System.Net.Http.HttpMethod.Post, SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(WsSecurityTestSupport.Wire(built, $"Build {protocol}"));

        Assert.IsTrue(SymmetricTestSupport.PrimarySignatureVerifiesAfterDecryption(wire));
    }

    [TestMethod]
    public void Build_StoresTheEncryptedKeySha1AndTheSecretInTheRequestMaterialWithoutExposingThem_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var material = WsSecurityFoundationTestSupport.KeyMaterialOf(request);

        Assert.IsNotNull(material);

        var json = System.Text.Json.JsonSerializer.Serialize(request.Options);

        foreach (var (name, value) in SymmetricTestSupport.ForbiddenValues(wire, WsSecurityFoundationTestSupport.Password).SelectMany(secret => secret.Renderings()))
            Assert.IsFalse(json.Contains(value, StringComparison.OrdinalIgnoreCase), $"{name} | {value}");
    }

    private static byte[] CipherValue(XmlElement encryptedData)
        => Convert.FromBase64String(SymmetricTestSupport.ChildText(SymmetricTestSupport.ChildOf(encryptedData, "CipherData", SymmetricTestSupport.XencNamespace), "CipherValue", SymmetricTestSupport.XencNamespace));
}
