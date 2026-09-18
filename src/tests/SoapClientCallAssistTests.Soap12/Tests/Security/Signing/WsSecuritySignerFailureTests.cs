#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Net.Http;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class WsSecuritySignerFailureTests
{

    [TestMethod]
    public void BuildRequest_WithSigningEnabledButNoCertificate_FailsAndProducesNoMessage_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(new SoapSecurityDto { Enabled = true });

        AssertFailedClosed(built, WsSecurityTestSupport.MissingSigningKeyMessage,
            "Sign");
    }

    [TestMethod]
    public void BuildRequest_WithAPublicKeyOnlyCertificate_FailsAndProducesNoMessage_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(
            WsSecurityTestSupport.Security(security =>
                security.SigningCertificate = WsSecurityTestSupport.PublicOnlyCertificate));

        AssertFailedClosed(built, WsSecurityTestSupport.MissingSigningKeyMessage,
            "");
    }

    [TestMethod]
    public void BuildRequest_WithNeitherBodyNorTimestampSigned_RefusesToEmitASignatureCoveringNothing_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(
            WsSecurityTestSupport.Security(security =>
            {
                security.SignBody = false;
                security.IncludeTimestamp = false;
            }));

        AssertFailedClosed(built, WsSecurityTestSupport.InsufficientSigningInputMessage,
            "");
    }

    [TestMethod]
    public void BuildRequest_WithAnAdditionalSignedElementIdThatResolvesToNothing_FailsAndProducesNoMessage_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(
            WsSecurityTestSupport.Security(security =>
                security.AdditionalSignedElementIds = new[] { "no-such-element" }));

        AssertFailedClosed(built, WsSecurityTestSupport.InsufficientSigningInputMessage,
            "");
    }

    [TestMethod]
    public void BuildRequest_WithAnAdditionalSignedElementIdThatResolvesToTwoElements_FailsAndProducesNoMessage_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(
            WsSecurityTestSupport.Security(security =>
                security.AdditionalSignedElementIds = new[] { "shared" }),
            TaggedBody("First", "shared"),
            TaggedBody("Second", "shared"));

        AssertFailedClosed(built, WsSecurityTestSupport.InsufficientSigningInputMessage,
            "");
    }

    [TestMethod]
    public void BuildRequest_WithAnAdditionalSignedElementIdThatResolvesToOneElement_SignsIt_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire(
            WsSecurityTestSupport.Security(security =>
                security.AdditionalSignedElementIds = new[] { "extra" }),
            TaggedBody("Only", "extra"));

        var document = WsSecurityTestSupport.ParseWire(wire);
        var namespaces = WsSecurityTestSupport.Namespaces(document);

        WsSecurityTestSupport.RequireNode(
            document,
            "//ds:Signature/ds:SignedInfo/ds:Reference[@URI='#extra']",
            namespaces,
            "");

        var references = document.SelectNodes("//ds:Signature/ds:SignedInfo/ds:Reference", namespaces);

        Assert.AreEqual(3, references.Count, wire);
    }

    [TestMethod]
    public void BuildRequest_WithGetAndSigningEnabled_FailsBecauseSigningNeedsABody_Test()
    {
        var built = WsSecurityTestSupport.Build(
            SoapProtocolType.SOAP_1_2, HttpMethod.Get, WsSecurityTestSupport.Security());

        AssertFailedClosed(built, WsSecurityTestSupport.PostOnlySigningMessage,
            "");
    }

    [TestMethod]
    public void BuildRequest_WithGetAndSigningDisabled_Succeeds_Test()
    {
        var built = WsSecurityTestSupport.Build(
            SoapProtocolType.SOAP_1_2, HttpMethod.Get, new SoapSecurityDto { Enabled = false });

        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));

        built.Response?.Dispose();
    }

    [TestMethod]
    public void BuildRequest_SigningTwiceWithTheSameCertificateInstance_SucceedsBothTimes_Test()
    {
        var first = WsSecurityTestSupport.SignedWire();

        Assert.IsTrue(first.Contains("SignatureValue", StringComparison.Ordinal), first);

        var second = WsSecurityTestSupport.SignedWire();

        Assert.IsTrue(second.Contains("SignatureValue", StringComparison.Ordinal), second);
    }

    [TestMethod]
    public void BuildRequest_SignedTwice_ProducesDistinctWsuIdValues_Test()
    {
        var firstId = WsSecurityTestSupport.SignedBodyId(WsSecurityTestSupport.SignedWire());
        var secondId = WsSecurityTestSupport.SignedBodyId(WsSecurityTestSupport.SignedWire());

        Assert.IsFalse(string.IsNullOrEmpty(firstId));

        Assert.AreNotEqual(firstId, secondId);
    }

    [TestMethod]
    public void BuildRequest_WhenSigningFails_KeepsTheSecurityValidationCode_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(new SoapSecurityDto { Enabled = true });

        var messages = NegativeTestSupport.Messages(built);

        Assert.AreEqual(1, messages.Count, NegativeTestSupport.Describe(built));

        Assert.AreEqual(WsSecurityTestSupport.SigningKeyCode, messages[0].Key, NegativeTestSupport.Describe(built));
    }

    [TestMethod]
    public void BuildRequest_WhenSigningFails_ShouldSurfaceTheSecurityValidationCode_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(new SoapSecurityDto { Enabled = true });

        Assert.AreEqual(WsSecurityTestSupport.SigningKeyCode, NegativeTestSupport.Messages(built)[0].Key);
    }

    [TestMethod]
    public void BuildRequest_WithAnAdditionalSignedElementIdOnTheEnvelope_RefusesToSignItsOwnSignature_Test()
    {
        var built = WsSecurityTestSupport.Client(SoapProtocolType.SOAP_1_2).BuildRequest(
            HttpMethod.Post,
            new BuildSoapRequestDto
            {
                Client = new HttpClientDto(WsSecurityTestSupport.Endpoint),
                Envelope = new SoapEnvelopeDto(
                    new[] { WsSecurityTestSupport.DefaultBody() },
                    null,
                    WsSecurityTestSupport.Action,
                    new[]
                    {
                        new XAttribute(XNamespace.Xmlns + "wsu", WsSecurityTestSupport.WsuNamespace),
                        new XAttribute(XNamespace.Get(WsSecurityTestSupport.WsuNamespace) + "Id", "env-1")
                    }),
                Security = WsSecurityTestSupport.Security(security =>
                    security.AdditionalSignedElementIds = new[] { "env-1" })
            });

        AssertFailedClosed(built, WsSecurityTestSupport.SelfEnclosingSignatureMessage,
            "");
    }

    [TestMethod]
    public void Sign_WhenTheSignerPlantsASecretInMessageDetails_TheLeakSweepFindsIt_Test()
    {
        var signed = new StubSoapMessageSigner(StubSoapMessageSigner.PlantedDetailSecret).Sign(
            WsSecurityTestSupport.DefaultBody(), WsSecurityTestSupport.Security());

        Assert.IsFalse(signed.IsSuccess);

        Assert.IsFalse(
            NegativeTestSupport.FirstMessageInfo(signed).Contains(
                StubSoapMessageSigner.PlantedDetailSecret, StringComparison.Ordinal));

        SecretLeakAssert.CarriesSecret(
            signed,
            StubSoapMessageSigner.PlantedDetailSecret,
            "");
    }

    [TestMethod]
    public void BuildRequest_WhenTheSignerPlantsASecretInMessageDetails_ShouldCarryTheDetailsAcross_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(
            WsSecurityTestSupport.Security(security => security.Signer = new StubSoapMessageSigner(StubSoapMessageSigner.PlantedDetailSecret)));

        Assert.IsFalse(built.IsSuccess, NegativeTestSupport.Describe(built));

        SecretLeakAssert.CarriesSecret(
            built,
            StubSoapMessageSigner.PlantedDetailSecret,
            "");
    }

    [TestMethod]
    public void Sign_WhenAFailurePathPutsCertificateMaterialInDetails_TheLeakCanaryGoesRed_Test()
    {
        var thumbprint = WsSecurityTestSupport.SigningCertificate.Thumbprint;

        var leaking = new StubSoapMessageSigner(thumbprint).Sign(
            WsSecurityTestSupport.DefaultBody(), WsSecurityTestSupport.Security());

        Assert.IsFalse(NegativeTestSupport.FirstMessageInfo(leaking).Contains(thumbprint, StringComparison.OrdinalIgnoreCase));

        Assert.ThrowsException<AssertFailedException>(() => SecretLeakAssert.CarriesNoSecret(leaking, "signing failure"));

        var clean = new StubSoapMessageSigner(null).Sign(
            WsSecurityTestSupport.DefaultBody(), WsSecurityTestSupport.Security());

        SecretLeakAssert.CarriesNoSecret(
            clean,
            "");
    }

    [TestMethod]
    public void BuildRequest_WhenTheSignerPlantsNothing_LeaksNoCertificateInternalsOnAnyMessageMember_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(
            WsSecurityTestSupport.Security(security => security.Signer = new StubSoapMessageSigner(null)));

        Assert.IsFalse(built.IsSuccess, NegativeTestSupport.Describe(built));

        SecretLeakAssert.CarriesNoSecret(built, "signing failure");
    }

    private static XElement TaggedBody(string name, string wsuId)
        => new(
            WsSecurityTestSupport.Service + name,
            new XAttribute(XNamespace.Get(WsSecurityTestSupport.WsuNamespace) + "Id", wsuId),
            new XElement(WsSecurityTestSupport.Service + "id", name));

    private static void AssertFailedClosed(IResult<HttpRequestMessage> built, string expectedMessage, string because)
    {
        Assert.IsFalse(built.IsSuccess, $"{because} | {NegativeTestSupport.Describe(built)}");

        Assert.IsNull(built.Response, because);

        Assert.AreEqual(expectedMessage, NegativeTestSupport.FirstMessageInfo(built), $"{because} | {NegativeTestSupport.Describe(built)}");

        SecretLeakAssert.CarriesNoSecret(built, because);
    }
}
