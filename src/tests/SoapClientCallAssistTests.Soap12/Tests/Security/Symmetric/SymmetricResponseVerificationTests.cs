#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Symmetric;

[TestClass]
public sealed class SymmetricResponseVerificationTests
{

    [TestMethod]
    public void Verify_AResponseSignedUnderTheKeyDerivedFromOurSecretWithItsOwnNonce_IsAcceptedAndReportsCoverage_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var response = Response(wire).Confirm(wire.PrimarySignatureValue).Build();

        var coverage = WsSecurityAssert.Accepted(new WsSecurityResponseSecurity().Verify(request, response), "");

        Assert.IsTrue(coverage.BodySigned);
        Assert.IsTrue(coverage.TimestampSigned);
        CollectionAssert.Contains(coverage.SignedElementLocalNames.ToList(), "RelatesTo");
    }

    [TestMethod]
    public void Verify_ACertificateCredentialResponseConfirmingBothSignatures_IsAccepted_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.CertificateSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var endorsingValue = SymmetricTestSupport.ChildText(wire.EndorsingSignature, "SignatureValue", WsSecurityTestSupport.DsNamespace);
        var response = Response(wire).Confirm(wire.PrimarySignatureValue).Confirm(endorsingValue).Build();

        WsSecurityAssert.Accepted(new WsSecurityResponseSecurity().Verify(request, response), "");
    }

    [DataTestMethod]
    [DataRow(SoapSecureConversationVersionType.February2005)]
    [DataRow(SoapSecureConversationVersionType.December2005)]
    public void Verify_EitherSecureConversationNamespaceOnTheResponseToken_IsAccepted_Test(SoapSecureConversationVersionType version)
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security => security.SymmetricBinding.Version = version));
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var response = Response(wire).WithScNamespace(SymmetricTestSupport.ScNamespace(version)).Confirm(wire.PrimarySignatureValue).Build();

        WsSecurityAssert.Accepted(new WsSecurityResponseSecurity().Verify(request, response), $"{version}");
    }

    [TestMethod]
    public void Verify_WithConfirmationNotRequired_AcceptsAResponseWithoutOne_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security =>
            security.ResponseSecurity = new SoapResponseSecurityDto { RequireSignatureConfirmation = false }));
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        WsSecurityAssert.Accepted(new WsSecurityResponseSecurity().Verify(request, Response(wire).Build()), "");
    }

    [TestMethod]
    public void Verify_ASecondTime_IsRefusedAsConsumedAndTheMaterialIsZeroed_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));
        var response = Response(wire).Confirm(wire.PrimarySignatureValue).Build();

        var service = new WsSecurityResponseSecurity();

        WsSecurityAssert.Accepted(service.Verify(request, response), "");
        Rejected(service.Verify(request, response), SymmetricTestSupport.ConsumedCode, "", wire);
    }

    [TestMethod]
    public void Verify_TheRequestReflectedBackAsTheResponse_IsRefusedByNonce_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Rejected(new WsSecurityResponseSecurity().Verify(request, wire.Text), SymmetricTestSupport.ReflectedNonceCode, "", wire);
    }

    [TestMethod]
    public void Verify_AResponseReusingOurSignatureNonce_IsRefusedByNonce_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var response = Response(wire).WithNonce(wire.NonceOf(wire.SignatureToken)).Confirm(wire.PrimarySignatureValue).Build();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), SymmetricTestSupport.ReflectedNonceCode, "", wire);
    }

    [TestMethod]
    public void Verify_AResponseTokenWhoseLabelAbsorbsTheFirstByteOfOurNonce_IsRefusedByNameBeforeAnyKeyIsDerived_Test()
    {
        HttpRequestMessage request = null;
        SymmetricWire wire = null;
        byte[] ourNonce = null;

        for (var attempt = 0; attempt < 400 && ourNonce is null; attempt++)
        {
            request?.Dispose();
            request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
            wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

            var candidate = wire.NonceOf(wire.SignatureToken);
            if (char.IsAsciiLetterOrDigit((char)candidate[0]))
                ourNonce = candidate;
        }

        Assert.IsNotNull(ourNonce);

        using (request)
        {
            var shiftedLabel = SymmetricTestSupport.DefaultLabel + (char)ourNonce[0];
            var shiftedNonce = ourNonce.Skip(1).ToArray();

            var response = Response(wire).WithLabel(shiftedLabel).WithNonce(shiftedNonce).Confirm(wire.PrimarySignatureValue);

            CollectionAssert.AreEqual(wire.SignatureKey, response.DerivedKey);

            Rejected(new WsSecurityResponseSecurity().Verify(request, response.Build()), SymmetricTestSupport.DerivedKeyTokenUnreadableCode, "", wire);
        }
    }

    [TestMethod]
    public void Verify_AResponseTokenReusingOurNonceUnderAnotherBase64Rendering_IsRefusedAsAReflectionByKey_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var ourNonce = wire.NonceOf(wire.SignatureToken);
        var rendering = Convert.ToBase64String(ourNonce);

        var response = Response(wire)
            .WithNonce(ourNonce)
            .Confirm(wire.PrimarySignatureValue)
            .ThenMutate(text => text.Replace($"<sc:Nonce>{rendering}</sc:Nonce>", $"<sc:Nonce>{rendering.Insert(4, "\t")}</sc:Nonce>"));

        CollectionAssert.AreEqual(wire.SignatureKey, response.DerivedKey);

        var built = response.Build();
        Assert.IsTrue(built.Contains(rendering.Insert(4, "\t")));

        Rejected(new WsSecurityResponseSecurity().Verify(request, built), SymmetricTestSupport.ReflectedNonceCode, "", wire);
    }

    [DataTestMethod]
    [DataRow(15)]
    [DataRow(17)]
    [DataRow(32)]
    public void Verify_AResponseTokenWithANonceThatIsNot16Bytes_IsRefusedByNameBeforeAnyKeyIsDerived_Test(int length)
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var nonce = new byte[length];
        RandomNumberGenerator.Fill(nonce);

        Rejected(new WsSecurityResponseSecurity().Verify(request, Response(wire).WithNonce(nonce).Confirm(wire.PrimarySignatureValue).Build()), SymmetricTestSupport.DerivedKeyTokenUnreadableCode, "", wire);
    }

    [TestMethod]
    public void Verify_WithAnExplicitRequestLabel_RequiresTheSameLabelOnTheResponseToken_Test()
    {
        const string label = "custom-label-for-this-request";

        using var unlabeled = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security => security.SymmetricBinding.KeyDerivationLabel = label));
        var unlabeledWire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(unlabeled));

        Rejected(new WsSecurityResponseSecurity().Verify(unlabeled, Response(unlabeledWire).Confirm(unlabeledWire.PrimarySignatureValue).Build()), SymmetricTestSupport.DerivedKeyTokenUnreadableCode, "", unlabeledWire);

        using var labeled = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security => security.SymmetricBinding.KeyDerivationLabel = label));
        var labeledWire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(labeled));

        WsSecurityAssert.Accepted(new WsSecurityResponseSecurity().Verify(labeled, Response(labeledWire).WithLabel(label).Confirm(labeledWire.PrimarySignatureValue).Build()), "");
    }

    [TestMethod]
    public void Verify_AResponseCarryingOurSignatureValue_IsRefusedAsAReflection_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.CertificateSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var ours = wire.PrimarySignatureValue;
        var response = Response(wire).Confirm(ours).ThenMutate(text => ReplaceSignatureValue(text, ours)).Build();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), SymmetricTestSupport.ReflectedSignatureCode, "", wire);
    }

    [TestMethod]
    public void Verify_AResponseKeyedToADifferentEncryptedKey_IsRefusedAsNotOurs_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var response = Response(wire).WithKeyIdentifier(SymmetricTestSupport.Sha1(new byte[] { 1, 2, 3 })).Confirm(wire.PrimarySignatureValue).Build();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), SymmetricTestSupport.EncryptedKeyNotOursCode, "", wire);
    }

    [TestMethod]
    public void Verify_AResponseCarryingAForeignEncryptedKey_IsRefusedUnderTheDecryptionCodeWithoutUnwrapping_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var foreign = new byte[256];
        RandomNumberGenerator.Fill(foreign);

        var response = Response(wire).WithForeignEncryptedKey(foreign).Confirm(wire.PrimarySignatureValue).Build();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), SymmetricTestSupport.DecryptionCode, "", wire);
    }

    [TestMethod]
    public void Verify_AResponseEchoingOurOwnEncryptedKey_IsNotRefusedForCarryingIt_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var response = Response(wire).WithForeignEncryptedKey(wire.Wrapped).Confirm(wire.PrimarySignatureValue).Build();

        WsSecurityAssert.Accepted(new WsSecurityResponseSecurity().Verify(request, response), "");
    }

    [TestMethod]
    public void Verify_AResponseWhoseConfirmationDoesNotEchoOurSignature_IsRefused_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var cases = new (string Name, Func<SymmetricResponseBuilder, SymmetricResponseBuilder> Shape)[]
        {
            ("missing", builder => builder),
            ("foreign", builder => builder.Confirm(Convert.ToBase64String(new byte[32]))),
            ("unsigned", builder => builder.Confirm(wire.PrimarySignatureValue).LeaveConfirmationsUnsigned()),
            ("one of two unsigned", builder => builder.Confirm(wire.PrimarySignatureValue).Confirm(Convert.ToBase64String(new byte[32])).LeaveConfirmationsUnsigned())
        };

        foreach (var (name, shape) in cases)
        {
            using var each = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
            var eachWire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(each));

            Rejected(
                new WsSecurityResponseSecurity().Verify(each, shape(Response(eachWire)).Build()),
                SymmetricTestSupport.ConfirmationCode,
                $"{name}",
                eachWire);
        }
    }

    [TestMethod]
    public void Verify_AResponseWhoseRelatesToIsMissingUnsignedOrForeign_IsRefused_Test()
    {
        var cases = new (string Name, Func<SymmetricWire, SymmetricResponseBuilder> Shape)[]
        {
            ("missing", wire => new SymmetricResponseBuilder(wire.Secret, wire.EncryptedKeySha1).Confirm(wire.PrimarySignatureValue)),
            ("unsigned", wire => Response(wire).Confirm(wire.PrimarySignatureValue).LeaveRelatesToUnsigned()),
            ("foreign", wire => new SymmetricResponseBuilder(wire.Secret, wire.EncryptedKeySha1).RelatesTo("urn:uuid:" + Guid.NewGuid().ToString("D")).Confirm(wire.PrimarySignatureValue))
        };

        foreach (var (name, shape) in cases)
        {
            using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
            var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

            Rejected(new WsSecurityResponseSecurity().Verify(request, shape(wire).Build()), SymmetricTestSupport.RelatesToCode, $"{name}", wire);
        }
    }

    [TestMethod]
    public void Verify_ATamperedBody_FailsTheHmacCheck_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var response = Response(wire).Confirm(wire.PrimarySignatureValue).ThenMutate(text => text.Replace("<IsValidResult>answer<", "<IsValidResult>tampered<")).Build();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), WsSecurityTestSupport.SignatureVerificationCode, "", wire);
    }

    [TestMethod]
    public void Verify_AResponseSignedUnderAKeyDerivedFromAnotherSecret_FailsTheHmacCheck_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var otherKey = new byte[24];
        RandomNumberGenerator.Fill(otherKey);

        var response = Response(wire).WithSigningKey(otherKey).Confirm(wire.PrimarySignatureValue).Build();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), WsSecurityTestSupport.SignatureVerificationCode, "", wire);
    }

    [TestMethod]
    public void Verify_ADerivedKeyTokenTheLibraryCannotRead_IsRefusedByName_Test()
    {
        var cases = new (string Name, Func<SymmetricResponseBuilder, SymmetricResponseBuilder> Shape)[]
        {
            ("length below 16", builder => builder.WithLength(8)),
            ("length above 64", builder => builder.WithLength(128)),
            ("unknown algorithm", builder => builder.WithAlgorithmAttribute("urn:not-p-sha1")),
            ("key reference value type of the other version", builder => builder.WithKeyReferenceValueType(SymmetricTestSupport.ScDecember2005Namespace + "/dk")),
            ("key identifier of another kind", builder => builder.WithKeyIdentifier(new byte[20], SymmetricTestSupport.ThumbprintSha1ValueType)),
            ("token id the signature does not reference", builder => builder.ThenMutate(text => text.Replace("wsu:Id=\"dk-1\"", "wsu:Id=\"dk-2\"")))
        };

        foreach (var (name, shape) in cases)
        {
            using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
            var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

            Rejected(
                new WsSecurityResponseSecurity().Verify(request, shape(Response(wire).Confirm(wire.PrimarySignatureValue)).Build()),
                SymmetricTestSupport.DerivedKeyTokenUnreadableCode,
                $"{name}",
                wire);
        }
    }

    [TestMethod]
    public void Verify_AResponseWithTheDefaultLengthImplied_DerivesA32ByteKey_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var response = Response(wire).WithLength(32, false).Confirm(wire.PrimarySignatureValue).Build();

        WsSecurityAssert.Accepted(new WsSecurityResponseSecurity().Verify(request, response), "");
    }

    [TestMethod]
    public void Verify_AnHmacSha1Response_IsAcceptedOnlyUnderTheSha1OptIn_Test()
    {
        using var optedIn = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security =>
            security.ResponseVerificationPolicy = new SoapVerificationPolicyDto { AllowSha1Algorithms = true }));
        var optedInWire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(optedIn));

        WsSecurityAssert.Accepted(
            new WsSecurityResponseSecurity().Verify(optedIn, Response(optedInWire).WithSignatureMethod(SymmetricTestSupport.HmacSha1Signature).Confirm(optedInWire.PrimarySignatureValue).Build()),
            "");

        using var strict = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var strictWire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(strict));

        Rejected(
            new WsSecurityResponseSecurity().Verify(strict, Response(strictWire).WithSignatureMethod(SymmetricTestSupport.HmacSha1Signature).Confirm(strictWire.PrimarySignatureValue).Build()),
            WsSecurityTestSupport.AlgorithmCode,
            "",
            strictWire);
    }

    [TestMethod]
    public void Verify_AResponseSignedWithACertificate_IsRefusedByTheShapeGateNotTheKey_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Rejected(
            new WsSecurityResponseSecurity().Verify(request, WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null)),
            WsSecurityTestSupport.AlgorithmCode,
            "",
            wire);
    }

    [TestMethod]
    public void Verify_AResponseWithoutASignature_IsRefusedByCount_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var response = Response(wire).Confirm(wire.PrimarySignatureValue).ThenMutate(StripSignature).Build();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), WsSecurityTestSupport.SignatureCountCode, "", wire);
    }

    [TestMethod]
    public void Decrypt_OnASymmetricRequestBuiltWithoutAllowDecryption_RefusesByNameAndConsumesTheMaterial_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var service = new WsSecurityResponseSecurity();
        var decrypted = service.Decrypt(request, Response(wire).Build());

        Assert.IsFalse(decrypted.IsSuccess);
        Assert.AreEqual(TierBTestSupport.DecryptionNotAllowedCode, WsSecurityFoundationTestSupport.FirstCode(decrypted));
        SecretLeakAssert.CarriesNoSecret(decrypted, "decryption refusal", SymmetricTestSupport.ForbiddenValues(wire, WsSecurityFoundationTestSupport.Password));

        Rejected(service.Verify(request, Response(wire).Confirm(wire.PrimarySignatureValue).Build()), SymmetricTestSupport.ConsumedCode, "", wire);
    }

    [TestMethod]
    public void Verify_WithTheStringOverloadOnASymmetricBinding_KeepsRefusingUnderTheDedicatedCode_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        Rejected(new WsSecurityResponseSecurity().Verify(Response(wire).Build(), SymmetricTestSupport.UserNameSecurity()), "V-SEC-032", "", wire);
    }

    private static SymmetricResponseBuilder Response(SymmetricWire wire)
        => new SymmetricResponseBuilder(wire.Secret, wire.EncryptedKeySha1).RelatesTo(wire.MessageId);

    private static void Rejected(IResult<SoapSignatureVerificationResult> result, string expectedCode, string because, SymmetricWire wire)
    {
        WsSecurityAssert.Rejected(result, expectedCode, because);
        SecretLeakAssert.CarriesNoSecret(result, expectedCode, SymmetricTestSupport.ForbiddenValues(wire, WsSecurityFoundationTestSupport.Password));
    }

    private static string ReplaceSignatureValue(string text, string value)
    {
        var start = text.IndexOf("<SignatureValue>", StringComparison.Ordinal) + "<SignatureValue>".Length;
        var end = text.IndexOf("</SignatureValue>", start, StringComparison.Ordinal);

        return text.Substring(0, start) + value + text.Substring(end);
    }

    private static string StripSignature(string text)
    {
        var start = text.IndexOf("<Signature xmlns", StringComparison.Ordinal);
        var end = text.IndexOf("</Signature>", start, StringComparison.Ordinal) + "</Signature>".Length;

        return text.Substring(0, start) + text.Substring(end);
    }
}
