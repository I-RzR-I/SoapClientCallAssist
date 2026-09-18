#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using SoapClientCallAssistTests.Soap12.Helpers.Wire;
using System;
using System.Linq;
using System.Net.Http;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Verification;

[TestClass]
public sealed class WsSecurityCoverageTests
{

    private readonly WsSecurityMessageVerifier _verifier = new WsSecurityMessageVerifier();

    [TestMethod]
    public void Verify_WithATamperedBody_RejectsTheMessage_Test()
    {
        var tampered = WsSecurityWireMutator.WithTamperedBody(WsSecurityTestSupport.SignedWire());

        WsSecurityAssert.Rejected(
            _verifier.Verify(tampered, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.SignatureVerificationCode,
            "");
    }

    [TestMethod]
    public void Verify_AgainstAnUnrelatedCertificate_RejectsTheMessage_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();

        WsSecurityAssert.Rejected(
            _verifier.Verify(signed, WsSecurityTestSupport.OtherCertificate),
            WsSecurityTestSupport.SignatureVerificationCode,
            "");
    }

    [TestMethod]
    public void Verify_AgainstThePublicHalfOfTheSigningCertificate_AcceptsTheMessage_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();

        WsSecurityAssert.Accepted(
            _verifier.Verify(signed, WsSecurityTestSupport.PublicOnlyCertificate),
            "");
    }

    [TestMethod]
    public void Verify_WhenASecondElementCarriesTheSignedWsuId_RejectsTheMessage_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();
        var bodyId = WsSecurityTestSupport.SignedBodyId(signed);

        var mutated = WsSecurityWireMutator.WithDuplicateWsuId(signed, bodyId);

        WsSecurityAssert.RejectedWithAnyCode(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            $"{bodyId}");
    }

    [TestMethod]
    public void Verify_WhenADecoyCarriesAPlainIdMatchingTheSignedWsuId_RejectsTheMessage_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();
        var bodyId = WsSecurityTestSupport.SignedBodyId(signed);

        var mutated = WsSecurityWireMutator.WithDecoyCarryingPlainId(signed, bodyId);

        WsSecurityAssert.RejectedWithAnyCode(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            $"{bodyId}");
    }

    [TestMethod]
    public void SignedRequest_BinarySecurityToken_CarriesTheSigningCertificateAndIsReferencedByKeyInfo_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire();

        var document = WsSecurityTestSupport.ParseWire(wire);
        var namespaces = WsSecurityTestSupport.Namespaces(document);

        var token = WsSecurityTestSupport.RequireNode(
            document, "//wsse:Security/wsse:BinarySecurityToken", namespaces, "BinarySecurityToken");

        CollectionAssert.AreEqual(WsSecurityTestSupport.SigningCertificate.RawData, Convert.FromBase64String(token.InnerText));

        Assert.AreEqual(WsSecurityTestSupport.X509TokenValueType, token.GetAttribute("ValueType"));

        var tokenId = WsSecurityTestSupport.WsuId(token);

        var keyInfoReference = WsSecurityTestSupport.RequireNode(
            document,
            "//ds:Signature/ds:KeyInfo/wsse:SecurityTokenReference/wsse:Reference",
            namespaces,
            "KeyInfo SecurityTokenReference");

        Assert.AreEqual("#" + tokenId, keyInfoReference.GetAttribute("URI"), $"{tokenId}");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, "actor", "role", DisplayName = "SOAP 1.1 uses actor")]
    [DataRow(SoapProtocolType.SOAP_1_2, "role", "actor", DisplayName = "SOAP 1.2 uses role")]
    public void SignedRequest_SecurityHeader_UsesTheTargetingAttributeOfItsProtocol_Test(
        SoapProtocolType protocol, string expected, string unexpected)
    {
        const string actor = "urn:ws-security-tests:actor";

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.Build(
                protocol,
                HttpMethod.Post,
                WsSecurityTestSupport.Security(security => security.SecurityActor = actor)),
            $"Build {protocol}");

        var document = WsSecurityTestSupport.ParseWire(wire);
        var namespaces = WsSecurityTestSupport.Namespaces(document);

        var security = WsSecurityTestSupport.RequireNode(
            document, "//wsse:Security", namespaces, "wsse:Security header");

        var envelopeNamespace = document.DocumentElement.NamespaceURI;

        Assert.AreEqual(actor, security.GetAttribute(expected, envelopeNamespace), $"{protocol} | {expected} | {envelopeNamespace} | {wire}");

        Assert.AreEqual(string.Empty, security.GetAttribute(unexpected, envelopeNamespace), $"{protocol} | {unexpected} | {wire}");
    }

    [TestMethod]
    public void SignedRequest_SecurityHeader_CarriesMustUnderstandByDefaultAndDropsItWhenAsked_Test()
    {
        var withDefault = WsSecurityTestSupport.ParseWire(WsSecurityTestSupport.SignedWire());

        var defaultSecurity = WsSecurityTestSupport.RequireNode(
            withDefault, "//wsse:Security", WsSecurityTestSupport.Namespaces(withDefault), "wsse:Security");

        Assert.AreEqual("1", defaultSecurity.GetAttribute("mustUnderstand", withDefault.DocumentElement.NamespaceURI));

        var withoutMustUnderstand = WsSecurityTestSupport.ParseWire(
            WsSecurityTestSupport.SignedWire(
                WsSecurityTestSupport.Security(security => security.MustUnderstand = false)));

        var relaxedSecurity = WsSecurityTestSupport.RequireNode(
            withoutMustUnderstand,
            "//wsse:Security",
            WsSecurityTestSupport.Namespaces(withoutMustUnderstand),
            "wsse:Security");

        Assert.AreEqual(string.Empty, relaxedSecurity.GetAttribute("mustUnderstand", withoutMustUnderstand.DocumentElement.NamespaceURI));
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public void SignedRequest_ForEitherProtocol_RoundTripsThroughTheVerifier_Test(SoapProtocolType protocol)
    {
        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.Build(protocol, HttpMethod.Post, WsSecurityTestSupport.Security()),
            $"Build {protocol}");

        var coverage = WsSecurityAssert.Accepted(
            _verifier.Verify(wire, WsSecurityTestSupport.SigningCertificate),
            $"{protocol}");

        Assert.IsTrue(coverage.BodySigned && coverage.TimestampSigned, $"{protocol} | {WsSecurityAssert.DescribeCoverage(coverage)}");
    }

    [TestMethod]
    public void Verify_WithASignedMessage_ReportsTheLocalNamesOfEveryCoveredElement_Test()
    {
        var coverage = WsSecurityAssert.Accepted(
            _verifier.Verify(WsSecurityTestSupport.SignedWire(), WsSecurityTestSupport.SigningCertificate),
            "");

        CollectionAssert.AreEquivalent(
            new[] { "Body", "Timestamp" },
            coverage.SignedElementLocalNames.ToArray(),
            WsSecurityAssert.DescribeCoverage(coverage));
    }
}
