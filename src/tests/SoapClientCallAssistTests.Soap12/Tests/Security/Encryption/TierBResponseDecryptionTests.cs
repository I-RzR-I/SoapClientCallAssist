#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Encryption;

[TestClass]
public sealed class TierBResponseDecryptionTests
{

    private const string ReflectedNonceCode = "V-SEC-041";

    private const string ResponsePayload = "<IsValidResponse xmlns=\"" + SoapAssert.ServiceNs + "\"><IsValidResult>" + TierBTestSupport.Canary + "</IsValidResult></IsValidResponse>";

    [TestMethod]
    public void DecryptAndVerify_AResponseEncryptedUnderAKeyDerivedFromOurSecret_ReturnsThePlaintextOnceTheSignatureVerifies_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire);

        var plaintext = TierBTestSupport.Accepted(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), "");

        Assert.IsTrue(plaintext.Contains(TierBTestSupport.Canary));
        Assert.IsTrue(plaintext.Contains("<s:Body wsu:Id=\"body-1\"><IsValidResponse"));
        Assert.IsFalse(plaintext.Contains("EncryptedData"));
        Assert.IsTrue(plaintext.Contains("ReferenceList"));
        Assert.IsTrue(plaintext.Contains("<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">"));
    }

    [TestMethod]
    public void DecryptAndVerify_WithTheSignatureLeftInClear_IsAccepted_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);

        var plaintext = TierBTestSupport.Accepted(new WsSecurityResponseSecurity().DecryptAndVerify(request, TierBTestSupport.Response(wire).LeaveSignatureInClear().Build()), "");

        Assert.IsTrue(plaintext.Contains(TierBTestSupport.Canary));
    }

    [TestMethod]
    public void DecryptAndVerify_AResponseToTheCertificateCredential_IsAccepted_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(TierBTestSupport.CertificateSecurity());
        var wire = TierBTestSupport.Parse(request);

        var plaintext = TierBTestSupport.Accepted(new WsSecurityResponseSecurity().DecryptAndVerify(request, TierBTestSupport.Response(wire).Build()), "");

        Assert.IsTrue(plaintext.Contains(TierBTestSupport.Canary));
    }

    [TestMethod]
    public void DecryptAndVerify_AResponseWithItsSignatureConfirmationsEncrypted_IsAcceptedAndStillBoundByThem_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(TierBTestSupport.CertificateSecurity());
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire);

        response.Inner.Confirm(SymmetricTestSupport.ChildText(wire.EndorsingSignature, "SignatureValue", WsSecurityTestSupport.DsNamespace));
        response.EncryptConfirmations();

        var built = response.Build();

        Assert.AreEqual(0, CountElements(built, "SignatureConfirmation"));
        Assert.AreEqual(4, CountElements(built, "EncryptedData"));

        var plaintext = TierBTestSupport.Accepted(new WsSecurityResponseSecurity().DecryptAndVerify(request, built), "");

        Assert.AreEqual(2, CountElements(plaintext, "SignatureConfirmation"));
        Assert.IsTrue(plaintext.Contains(TierBTestSupport.Canary));
    }

    [TestMethod]
    public void DecryptAndVerify_AResponseWithItsConfirmationsEncryptedButNoneMatchingTheRequest_RefusesUniformly_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = new TierBResponseBuilder(wire, new SymmetricResponseBuilder(wire.Secret, wire.EncryptedKeySha1).RelatesTo(wire.MessageId).Confirm(Convert.ToBase64String(RandomData.Bytes(32))).WithBody(TierBTestSupport.Canary)).EncryptConfirmations();

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithMoreEncryptedElementsThanTheSecurityHeaderBound_RefusesBeforeAnyKeyIsTouched_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire);

        for (var index = 0; index < 8; index++)
            response.Inner.Confirm(Convert.ToBase64String(RandomData.Bytes(32)));

        response.EncryptConfirmations();

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.EncryptedShapeCode, "", wire, response);
    }

    [TestMethod]
    public void Decrypt_IsTheSameOperationAsDecryptAndVerify_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);

        var plaintext = TierBTestSupport.Accepted(new WsSecurityResponseSecurity().Decrypt(request, TierBTestSupport.Response(wire).Build()), "");

        Assert.IsTrue(plaintext.Contains(TierBTestSupport.Canary));
        TierBTestSupport.Refused(new WsSecurityResponseSecurity().Decrypt(request, TierBTestSupport.Response(wire).Build()), TierBTestSupport.ConsumedCode, "", wire);
    }

    [TestMethod]
    public void DecryptAndVerify_ASecondTimeOnTheSameRequest_IsRefusedAsConsumed_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var service = new WsSecurityResponseSecurity();
        var response = TierBTestSupport.Response(wire);

        TierBTestSupport.Accepted(service.DecryptAndVerify(request, response.Build()), "");
        TierBTestSupport.Refused(service.DecryptAndVerify(request, response.Build()), TierBTestSupport.ConsumedCode, "", wire, response);
        WsSecurityAssert.Rejected(service.Verify(request, response.Build()), TierBTestSupport.ConsumedCode, "");
    }

    [TestMethod]
    public void DecryptAndVerify_AfterAFailedAttempt_IsRefusedAsConsumed_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var service = new WsSecurityResponseSecurity();
        var tampered = TierBTestSupport.Response(wire).TamperBodyCipherText(20);

        TierBTestSupport.Refused(service.DecryptAndVerify(request, tampered.Build()), TierBTestSupport.DecryptionCode, "", wire, tampered);
        TierBTestSupport.Refused(service.DecryptAndVerify(request, TierBTestSupport.Response(wire).Build()), TierBTestSupport.ConsumedCode, "", wire);
    }

    [TestMethod]
    public void Verify_OverAnEncryptedResponse_RefusesByNameAndSurfacesNoCipherText_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire);

        var verified = new WsSecurityResponseSecurity().Verify(request, response.Build());

        WsSecurityAssert.Rejected(verified, TierBTestSupport.DecryptionNotAllowedCode, "");
        SecretLeakAssert.CarriesNoSecret(verified, "refusal", TierBTestSupport.ForbiddenValues(wire, response));
    }

    [TestMethod]
    public void DecryptAndVerify_OnARequestBuiltWithoutAllowDecryption_RefusesByNameAndSurfacesNoCipherText_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire);

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionNotAllowedCode, "", wire, response);
    }

    [DataTestMethod]
    [DataRow(0, DisplayName = "last block, padding")]
    [DataRow(20, DisplayName = "middle of the cipher text")]
    [DataRow(-1, DisplayName = "initialisation vector")]
    public void DecryptAndVerify_WithTamperedCipherText_RefusesUniformlyWithoutPlaintext_Test(int indexFromEnd)
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire);

        if (indexFromEnd < 0)
            response.ThenMutate(text => FlipFirstCipherByte(text, response));
        else
            response.TamperBodyCipherText(indexFromEnd);

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_UnderAWrongEncryptionKey_RefusesUniformly_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).WithEncryptionKey(RandomData.Bytes(32));

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithASignatureThatDoesNotVerifyOverThePlaintext_RefusesUniformlyWithoutPlaintext_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire);

        response.Inner.WithSigningKey(RandomData.Bytes(24));

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithAClearSignatureTamperedAfterSigning_RefusesUniformlyWithoutPlaintext_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).LeaveSignatureInClear().ThenMutate(text => text.Replace("<SignatureValue>", "<SignatureValue>AAAA"));

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_EveryFailureAfterAKeyIsDerived_CarriesTheSameMessageAndNoException_Test()
    {
        var infos = new List<string>();

        foreach (var arrange in PostKeyFailures())
        {
            using var request = TierBTestSupport.BuildRequest();
            var wire = TierBTestSupport.Parse(request);
            var response = arrange(TierBTestSupport.Response(wire));

            var result = new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build());

            TierBTestSupport.Refused(result, TierBTestSupport.DecryptionCode, "", wire, response);

            var message = NegativeTestSupport.Messages(result)[0];

            Assert.IsTrue(message.Message.Details is null || message.Message.Details.Count == 0, NegativeTestSupport.Describe(result));

            infos.Add(NegativeTestSupport.FirstMessageInfo(result));
        }

        Assert.AreEqual(1, infos.Distinct(StringComparer.Ordinal).Count(), string.Join(" || ", infos));
    }

    [TestMethod]
    public void DecryptAndVerify_WithDiagnosticDetail_NamesTheFirstFailingStageAndNothingElse_Test()
    {
        using var request = TierBTestSupport.BuildRequest(security => security.ResponseVerificationPolicy = new SoapVerificationPolicyDto { DiagnosticDetail = true });
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire);

        response.Inner.WithSigningKey(RandomData.Bytes(24));

        var result = new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build());

        TierBTestSupport.Refused(result, TierBTestSupport.DecryptionCode, "", wire, response);
        StringAssert.Contains(NegativeTestSupport.FirstMessageInfo(result), "signature");
        StringAssert.Contains(NegativeTestSupport.FirstMessageInfo(result), "not for production");
    }

    [TestMethod]
    public void DecryptAndVerify_WithoutDiagnosticDetail_NamesNoStage_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire);

        response.Inner.WithSigningKey(RandomData.Bytes(24));

        var result = new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build());

        Assert.IsFalse(NegativeTestSupport.FirstMessageInfo(result).Contains("stage"), NegativeTestSupport.Describe(result));
    }

    [DataTestMethod]
    [DataRow("foreign EncryptedKey")]
    [DataRow("CipherReference")]
    [DataRow("RetrievalMethod")]
    [DataRow("X509 KeyInfo")]
    [DataRow("CarriedKeyName")]
    [DataRow("KeyReference")]
    [DataRow("unreferenced EncryptedData")]
    [DataRow("no ReferenceList")]
    [DataRow("EncryptedData outside the Security header")]
    [DataRow("Body of type Element")]
    [DataRow("DataReference to nothing")]
    [DataRow("DataReference to the Body itself")]
    [DataRow("EncryptedData in a second Security header")]
    public void DecryptAndVerify_WithAShapeTheLibraryDoesNotDecrypt_RefusesBeforeAnyKeyIsTouched_Test(string shape)
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = Shape(TierBTestSupport.Response(wire), shape);

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.EncryptedShapeCode, shape, wire, response);
    }

    [DataTestMethod]
    [DataRow(TierBTestSupport.Aes128Cbc, DisplayName = "AES-128 instead of the request's AES-256")]
    [DataRow(TierBTestSupport.Aes256Gcm, DisplayName = "AES-256-GCM")]
    [DataRow(TierBTestSupport.TripleDesCbc, DisplayName = "Triple DES")]
    [DataRow(null, DisplayName = "no EncryptionMethod")]
    public void DecryptAndVerify_WithADataAlgorithmOtherThanTheRequests_RefusesBeforeDecrypting_Test(string algorithm)
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = algorithm is null ? TierBTestSupport.Response(wire).WithoutEncryptionMethod() : TierBTestSupport.Response(wire).WithAlgorithm(algorithm);

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.EncryptedShapeCode, "", wire, response);
    }

    [DataTestMethod]
    [DataRow(16, true, DisplayName = "Length 16 under AES-256")]
    [DataRow(24, true, DisplayName = "Length 24 under AES-256")]
    [DataRow(64, true, DisplayName = "Length 64 under AES-256")]
    public void DecryptAndVerify_WithATokenLengthThatDisagreesWithTheAlgorithm_RefusesByName_Test(int length, bool write)
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).WithEncryptionLength(length, write).WithEncryptionKey(RandomData.Bytes(32));

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.EncryptedShapeCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithAnUnstatedTokenLength_TakesTheSpecificationDefaultOf32_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);

        var plaintext = TierBTestSupport.Accepted(new WsSecurityResponseSecurity().DecryptAndVerify(request, TierBTestSupport.Response(wire).WithEncryptionLength(32, false).Build()), "");

        Assert.IsTrue(plaintext.Contains(TierBTestSupport.Canary));
    }

    [TestMethod]
    public void DecryptAndVerify_WithABodyKeyedToTheSignatureToken_RefusesByLength_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).WithKeyReferenceUri("#dk-1");

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.EncryptedShapeCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithABodyKeyedToATokenThatDoesNotExist_RefusesAsUnreadable_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).WithKeyReferenceUri("#dk-9");

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.TokenUnreadableCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithATokenKeyedToAnotherEncryptedKey_RefusesAsForeign_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).WithEncryptionKeyIdentifier(RandomData.Bytes(20));

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.TokenForeignCode, "", wire, response);
    }

    [DataTestMethod]
    [DataRow("<x Id=\"ed-body\"/>", DisplayName = "unqualified Id")]
    [DataRow("<x xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" wsu:Id=\"ed-body\"/>", DisplayName = "wsu:Id")]
    [DataRow("<x ID=\"ed-body\"/>", DisplayName = "ID")]
    [DataRow("<x id=\"ed-body\"/>", DisplayName = "id")]
    public void DecryptAndVerify_WithAPlantedElementClaimingTheEncryptedDataId_Refuses_Test(string planted)
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).PlantInHeader(planted);

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.EncryptedShapeCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithCipherTextTheCapCannotHold_RefusesBeforeDecrypting_Test()
    {
        using var request = TierBTestSupport.BuildRequest(security => security.ResponseSecurity.MaxPlaintextBytes = 32);
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire);

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.CipherCapCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithCipherTextUnderTheCap_IsAccepted_Test()
    {
        using var request = TierBTestSupport.BuildRequest(security => security.ResponseSecurity.MaxPlaintextBytes = 64 * 1024);
        var wire = TierBTestSupport.Parse(request);

        Assert.IsTrue(TierBTestSupport.Accepted(new WsSecurityResponseSecurity().DecryptAndVerify(request, TierBTestSupport.Response(wire).Build()), "").Contains(TierBTestSupport.Canary));
    }

    [DataTestMethod]
    [DataRow("<!DOCTYPE a [<!ENTITY e \"x\">]><a>&e;</a>", DisplayName = "DOCTYPE")]
    [DataRow("<a>", DisplayName = "unclosed element")]
    [DataRow("plain text only", DisplayName = "no element")]
    [DataRow("<a xmlns=\"urn:a\"><b>", DisplayName = "unclosed nested element")]
    public void DecryptAndVerify_WithAPlaintextThatDoesNotParse_RefusesUniformly_Test(string plaintext)
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).WithBodyPlaintext(plaintext);

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, "", wire, response);
    }

    [DataTestMethod]
    [DataRow("Envelope")]
    [DataRow("Header")]
    [DataRow("Body")]
    [DataRow("Security")]
    [DataRow("Signature")]
    [DataRow("EncryptedKey")]
    [DataRow("EncryptedData")]
    public void DecryptAndVerify_WithAPlaintextCarryingAForbiddenElement_RefusesUniformly_Test(string localName)
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).WithBodyPlaintext(ResponsePayload + $"<{localName} xmlns=\"urn:planted\">{TierBTestSupport.Canary}</{localName}>");

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, localName, wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithAPlaintextCarryingAForbiddenElementNestedDeep_RefusesUniformly_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).WithBodyPlaintext("<a><b><c><Signature xmlns=\"urn:planted\"/></c></b></a>");

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithAPlaintextThatIsNotUtf8_RefusesUniformly_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).WithBodyPlaintext(new byte[] { 0x3C, 0x61, 0x3E, 0xFF, 0xFE, 0xC0, 0x3C, 0x2F, 0x61, 0x3E });

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithAnEncryptedSignatureThatIsNotASignature_RefusesUniformly_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).ThenMutate(text => SwapEncryptedParts(text));

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithAnEncryptionTokenReusingOurNonceInAnotherRendering_RefusesByKeyNotByString_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var ourNonce = wire.NonceOf(wire.EncryptionToken);
        var rendering = Convert.ToBase64String(ourNonce).Insert(8, " ");

        CollectionAssert.AreEqual(ourNonce, Convert.FromBase64String(rendering));

        var response = TierBTestSupport.Response(wire).WithEncryptionNonce(ourNonce, rendering);

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithAnEncryptionTokenReusingOurSignatureNonceAtTheEncryptionLength_RefusesByKey_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var ourNonce = wire.NonceOf(wire.SignatureToken);
        var rendering = Convert.ToBase64String(ourNonce).Insert(4, "\t");

        var response = TierBTestSupport.Response(wire).WithEncryptionNonce(ourNonce, rendering);

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.DecryptionCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithAnEncryptionTokenCarryingADifferentLabel_RefusesBeforeAnyKeyIsTouched_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).WithEncryptionLabel(SymmetricTestSupport.DefaultLabel + "W");

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.TokenUnreadableCode, "", wire, response);
    }

    [DataTestMethod]
    [DataRow(15)]
    [DataRow(17)]
    [DataRow(32)]
    public void DecryptAndVerify_WithAnEncryptionTokenNonceThatIsNot16Bytes_RefusesBeforeAnyKeyIsTouched_Test(int length)
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire).WithEncryptionNonce(RandomData.Bytes(length));

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), TierBTestSupport.TokenUnreadableCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithAnExplicitRequestLabel_RequiresTheSameLabelOnTheResponse_Test()
    {
        const string label = "custom-label-for-this-request";

        using var request = TierBTestSupport.BuildRequest(security => security.SymmetricBinding.KeyDerivationLabel = label);
        var wire = TierBTestSupport.Parse(request);

        var unlabeled = TierBTestSupport.Response(wire);
        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, unlabeled.Build()), TierBTestSupport.TokenUnreadableCode, "", wire, unlabeled);

        using var second = TierBTestSupport.BuildRequest(security => security.SymmetricBinding.KeyDerivationLabel = label);
        var secondWire = TierBTestSupport.Parse(second);
        var labeled = TierBTestSupport.Response(secondWire).WithEncryptionLabel(label);
        labeled.Inner.WithLabel(label);

        Assert.IsTrue(TierBTestSupport.Accepted(new WsSecurityResponseSecurity().DecryptAndVerify(second, labeled.Build()), "").Contains(TierBTestSupport.Canary));
    }

    [TestMethod]
    public void DecryptAndVerify_WithASignatureTokenReusingOurSignatureNonce_IsRefusedAsAReflection_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var response = TierBTestSupport.Response(wire);

        response.Inner.WithNonce(wire.NonceOf(wire.SignatureToken));

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, response.Build()), ReflectedNonceCode, "", wire, response);
    }

    [TestMethod]
    public void DecryptAndVerify_WithNoResponseOrAnOversizedOne_RefusesByName_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(request, null), "V-SEC-005", "", wire);
    }

    [TestMethod]
    public void DecryptAndVerify_WithoutASentRequest_RefusesByName_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);

        using var bare = new HttpRequestMessage(HttpMethod.Post, "http://localhost/");

        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(bare, TierBTestSupport.Response(wire).Build()), "V-SEC-030", "", wire);
        TierBTestSupport.Refused(new WsSecurityResponseSecurity().DecryptAndVerify(null, TierBTestSupport.Response(wire).Build()), "V-SEC-030", "", wire);
    }

    [TestMethod]
    public void ResponseSecurity_HoldsNoTransportSoADecryptionFailureCanTakeNoWireAction_Test()
    {
        foreach (var type in new[] { typeof(WsSecurityResponseSecurity), WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.WsSecurityResponseDecryptor") })
        {
            var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            foreach (var field in fields)
                Assert.IsFalse(
                    typeof(HttpClient).IsAssignableFrom(field.FieldType) || typeof(HttpMessageHandler).IsAssignableFrom(field.FieldType) || typeof(IHttpClientFactory).IsAssignableFrom(field.FieldType),
                    $"{type.Name} | {field.Name}");
        }
    }

    [TestMethod]
    public void RequestMaterial_ZeroesTheDerivedKeysWhenConsumed_Test()
    {
        using var request = TierBTestSupport.BuildRequest();
        var wire = TierBTestSupport.Parse(request);
        var material = WsSecurityFoundationTestSupport.KeyMaterialOf(request);
        var field = material.GetType().GetField("_requestDerivedKeys", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var keys = (byte[][])field.GetValue(material);

        Assert.IsNotNull(keys);
        Assert.IsTrue(keys.Any(key => key.SequenceEqual(wire.EncryptionKey)));
        Assert.IsTrue(keys.Any(key => key.SequenceEqual(wire.SignatureKey)));

        TierBTestSupport.Accepted(new WsSecurityResponseSecurity().DecryptAndVerify(request, TierBTestSupport.Response(wire).Build()), "Consumed.");

        Assert.IsNull(field.GetValue(material));
        Assert.IsTrue(keys.All(key => key.All(b => b == 0)));
    }

    private static IEnumerable<Func<TierBResponseBuilder, TierBResponseBuilder>> PostKeyFailures()
    {
        yield return response => response.TamperBodyCipherText(20);
        yield return response => response.WithEncryptionKey(RandomData.Bytes(32));
        yield return response => response.WithBodyPlaintext("<!DOCTYPE a [<!ENTITY e \"x\">]><a>&e;</a>");
        yield return response => response.WithBodyPlaintext(new byte[] { 0xFF, 0xFE, 0x3C });
        yield return response => response.WithBodyPlaintext("<Envelope/>");
        yield return response =>
        {
            response.Inner.WithSigningKey(RandomData.Bytes(24));

            return response;
        };
    }

    private static TierBResponseBuilder Shape(TierBResponseBuilder response, string shape)
    {
        switch (shape)
        {
            case "foreign EncryptedKey":
                return response.WithForeignEncryptedKey();
            case "CipherReference":
                return response.WithCipherReference();
            case "RetrievalMethod":
                return response.WithRetrievalMethod();
            case "X509 KeyInfo":
                return response.WithX509KeyInfo();
            case "CarriedKeyName":
                return response.PlantInHeader("<xenc:CarriedKeyName xmlns:xenc=\"http://www.w3.org/2001/04/xmlenc#\">k</xenc:CarriedKeyName>");
            case "KeyReference":
                return response.PlantInHeader("<xenc:KeyReference URI=\"#ed-body\" xmlns:xenc=\"http://www.w3.org/2001/04/xmlenc#\"/>");
            case "unreferenced EncryptedData":
                return response.WithUnreferencedEncryptedData();
            case "no ReferenceList":
                return response.WithoutReferenceList();
            case "EncryptedData outside the Security header":
                return response.WithEncryptedDataOutsideSecurity();
            case "Body of type Element":
                return response.WithBodyType(TierBTestSupport.ElementType);
            case "DataReference to nothing":
                return response.WithBodyReferenceUri("#ed-nowhere");
            case "DataReference to the Body itself":
                return response.WithBodyReferenceUri("#body-1");
            case "EncryptedData in a second Security header":
                return response.PlantInHeader("<wsse:Security xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\"/>");
            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }
    }

    private static string FlipFirstCipherByte(string text, TierBResponseBuilder response)
    {
        var prefix = response.BodyCipherValuePrefix;
        var start = text.IndexOf(prefix, StringComparison.Ordinal);
        var end = text.IndexOf("</xenc:CipherValue>", start, StringComparison.Ordinal);
        var bytes = Convert.FromBase64String(text.Substring(start, end - start));

        bytes[0] ^= 0x5A;

        return text.Substring(0, start) + Convert.ToBase64String(bytes) + text.Substring(end);
    }

    private static string SwapEncryptedParts(string text)
    {
        var document = SymmetricTestSupport.NewDocument(text);
        var parts = document.GetElementsByTagName("EncryptedData", SymmetricTestSupport.XencNamespace).Cast<XmlElement>().ToList();

        Assert.AreEqual(2, parts.Count);

        var first = SymmetricTestSupport.ChildOf(SymmetricTestSupport.ChildOf(parts[0], "CipherData", SymmetricTestSupport.XencNamespace), "CipherValue", SymmetricTestSupport.XencNamespace);
        var second = SymmetricTestSupport.ChildOf(SymmetricTestSupport.ChildOf(parts[1], "CipherData", SymmetricTestSupport.XencNamespace), "CipherValue", SymmetricTestSupport.XencNamespace);

        (first.InnerText, second.InnerText) = (second.InnerText, first.InnerText);

        return document.OuterXml;
    }

    private static int CountElements(string envelope, string localName)
        => SymmetricTestSupport.NewDocument(envelope).GetElementsByTagName("*").Cast<XmlNode>().Count(node => node.LocalName == localName);
}
