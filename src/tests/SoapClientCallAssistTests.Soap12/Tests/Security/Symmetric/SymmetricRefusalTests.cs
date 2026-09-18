#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Symmetric;

[TestClass]
public sealed class SymmetricRefusalTests
{

    private static readonly ForbiddenSecret PasswordSecret = ForbiddenSecret.OfText("the username token password", WsSecurityFoundationTestSupport.Password);

    [TestMethod]
    public void BuildRequest_WithHmacSha1ButNoSha1OptIn_RefusesUnderTheOptInRule_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(SymmetricTestSupport.UserNameSecurity(security =>
                security.SymmetricBinding.SignatureAlgorithm = SoapSymmetricSignatureAlgorithmType.HmacSha1)),
            "V-SEC-044",
            "",
            PasswordSecret);

    [DataTestMethod]
    [DataRow(SoapServiceKeyIdentifierType.SubjectKeyIdentifier)]
    [DataRow(SoapServiceKeyIdentifierType.IssuerSerial)]
    [DataRow(SoapServiceKeyIdentifierType.BinarySecurityToken)]
    public void BuildRequest_WithAServiceKeyIdentifierOtherThanTheThumbprint_RefusesAsNotAvailable_Test(SoapServiceKeyIdentifierType identifier)
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(SymmetricTestSupport.UserNameSecurity(security =>
                security.SymmetricBinding.ServiceKeyIdentifier = identifier)),
            "V-SEC-045",
            "",
            PasswordSecret);

    [TestMethod]
    public void BuildRequest_WithRsa15KeyWrap_RefusesIt_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(SymmetricTestSupport.UserNameSecurity(security =>
                security.Encryption = new SoapEncryptionDto { KeyWrap = SoapKeyWrapAlgorithmType.Rsa15 })),
            "V-SEC-055",
            "",
            PasswordSecret);

    [DataTestMethod]
    [DataRow(SoapDataEncryptionAlgorithmType.Aes128Cbc, 32)]
    [DataRow(SoapDataEncryptionAlgorithmType.Aes192Cbc, 32)]
    [DataRow(SoapDataEncryptionAlgorithmType.Aes256Cbc, 16)]
    public void BuildRequest_WithAnEncryptionKeyLengthThatDoesNotFitTheDataAlgorithm_RefusesIt_Test(SoapDataEncryptionAlgorithmType algorithm, int keyLength)
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(SymmetricTestSupport.UserNameSecurity(security =>
            {
                security.Encryption = new SoapEncryptionDto { DataAlgorithm = algorithm };
                security.SymmetricBinding.EncryptionKeyLength = keyLength;
            })),
            "V-SEC-056",
            "",
            PasswordSecret);

    [DataTestMethod]
    [DataRow(16)]
    [DataRow(24)]
    public void BuildRequest_WithAnEncryptionKeyLengthThatDoesNotFitTheDefaultDataAlgorithmAndNoEncryptionOptions_RefusesByName_Test(int keyLength)
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(SymmetricTestSupport.UserNameSecurity(security => security.SymmetricBinding.EncryptionKeyLength = keyLength)),
            "V-SEC-056",
            "",
            PasswordSecret);

    [TestMethod]
    public void BuildRequest_WithAnEncryptionKeyLengthThatDoesNotFitTheDefaultDataAlgorithmAndNothingToEncrypt_StillRefusesByName_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(SymmetricTestSupport.CertificateSecurity(security => security.SymmetricBinding.EncryptionKeyLength = 16)),
            "V-SEC-056",
            "");

    [TestMethod]
    public void BuildRequest_WithAnEncryptionKeyLengthThatFitsTheDefaultDataAlgorithm_EncryptsTheTokenUnderAes256_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security => security.SymmetricBinding.EncryptionKeyLength = 32));
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Assert.AreEqual(32, wire.EncryptionKey.Length);
        Assert.AreEqual(SymmetricTestSupport.Aes256Cbc, SymmetricTestSupport.ChildOf(wire.EncryptedDatas[0], "EncryptionMethod", SymmetricTestSupport.XencNamespace).GetAttribute("Algorithm"));
    }

    [TestMethod]
    public void BuildRequest_WithEncryptBodyAndAllowDecryption_BuildsWithTheBodyContentEncrypted_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security =>
        {
            security.Encryption = new SoapEncryptionDto { EncryptBody = true };
            security.ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true };
        }));

        var wire = SymmetricTestSupport.Wire(request);
        var body = TierBTestSupport.Body(SymmetricTestSupport.NewDocument(wire));
        var encryptedData = SymmetricTestSupport.ChildrenOf(body, "EncryptedData", SymmetricTestSupport.XencNamespace);

        Assert.AreEqual(1, encryptedData.Count);
        Assert.AreEqual(TierBTestSupport.ContentType, encryptedData[0].GetAttribute("Type"));
        Assert.IsFalse(wire.Contains(WsSecurityFoundationTestSupport.Password));
    }

    [DataTestMethod]
    [DataRow(SoapDataEncryptionAlgorithmType.Aes128Cbc, 16)]
    [DataRow(SoapDataEncryptionAlgorithmType.Aes192Cbc, 24)]
    public void BuildRequest_WithASmallerDataAlgorithmAndAMatchingKeyLength_Builds_Test(SoapDataEncryptionAlgorithmType algorithm, int keyLength)
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security =>
        {
            security.Encryption = new SoapEncryptionDto { DataAlgorithm = algorithm };
            security.SymmetricBinding.EncryptionKeyLength = keyLength;
        }));

        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Assert.AreEqual(keyLength, wire.LengthOf(wire.EncryptionToken));
        Assert.AreEqual(WsSecurityFoundationTestSupport.Username, WsSecurityFoundationTestSupport.ChildText(SymmetricTestSupport.DecryptElement(wire.EncryptedDatas[0], wire.EncryptionKey), "Username"));
    }

    [TestMethod]
    public void BuildRequest_WithEncryptionOptionsButNothingToEncrypt_EmitsNoReferenceListAndOneDerivedKeyToken_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.CertificateSecurity(security =>
            security.Encryption = new SoapEncryptionDto()));

        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Assert.IsNull(wire.ReferenceList);
        Assert.AreEqual(1, wire.DerivedKeyTokens.Count);
        Assert.AreEqual(0, wire.EncryptedDatas.Count);
    }
}
