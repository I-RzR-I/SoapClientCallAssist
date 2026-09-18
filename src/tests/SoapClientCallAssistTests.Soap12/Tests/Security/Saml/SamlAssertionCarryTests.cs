#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Saml;

[TestClass]
public sealed class SamlAssertionCarryTests
{

    public TestContext TestContext { get; set; }

    [DataTestMethod]
    [DataRow(true, DisplayName = "SAML 2.0")]
    [DataRow(false, DisplayName = "SAML 1.1")]
    public void BuildRequest_WithABearerAssertionAlone_CarriesItRawAsTheOnlyTokenWithNoSignature_Test(bool saml20)
    {
        var assertion = saml20 ? SamlTestSupport.Bearer20() : SamlTestSupport.Bearer11();

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            "Build");

        var document = WsSecurityTestSupport.ParseWire(wire);
        var security = WsSecurityFoundationTestSupport.SecurityHeader(document);
        var children = security.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Select(element => element.LocalName).ToArray();

        CollectionAssert.AreEqual(new[] { "Timestamp", "Assertion" }, children, $"{string.Join(", ", children)} | {wire}");

        var carried = SamlTestSupport.AssertionIn(document);

        Assert.AreEqual(assertion.OuterXml, carried.OuterXml, wire);
        Assert.IsNull(document.SelectSingleNode("//wsse:Security/ds:Signature", WsSecurityTestSupport.Namespaces(document)), wire);
    }

    [TestMethod]
    public void BuildRequest_WithABearerAssertionAndNoTimestamp_CarriesTheAssertionAlone_Test()
    {
        var assertion = SamlTestSupport.Bearer20();

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion, configure: security => security.IncludeTimestamp = false)),
            "Build");

        var document = WsSecurityTestSupport.ParseWire(wire);
        var children = WsSecurityFoundationTestSupport.SecurityHeader(document).ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Select(element => element.LocalName).ToArray();

        CollectionAssert.AreEqual(new[] { "Assertion" }, children, $"{string.Join(", ", children)} | {wire}");
    }

    [TestMethod]
    public void BuildRequest_WithABearerAssertionAndASigningCertificate_CarriesTheAssertionUnsignedNextToTheCertificateSignature_Test()
    {
        var assertion = SamlTestSupport.Bearer20();

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(WsSecurityTestSupport.Security(security => security.SamlToken = SamlTestSupport.Token(assertion))),
            "Build");

        var document = WsSecurityTestSupport.ParseWire(wire);
        var children = WsSecurityFoundationTestSupport.SecurityHeader(document).ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Select(element => element.LocalName).ToArray();

        CollectionAssert.AreEqual(
            new[] { "BinarySecurityToken", "Timestamp", "Assertion", "Signature" },
            children,
            $"{string.Join(", ", children)} | {wire}");

        var references = SamlTestSupport.MessageSignatureReferences(document);

        Assert.IsFalse(references.Contains("#" + SamlTestSupport.IdOf(assertion)), $"{string.Join(", ", references)} | {wire}");

        var keyInfoReference = WsSecurityTestSupport.RequireNode(
            document, "//wsse:Security/ds:Signature/ds:KeyInfo/wsse:SecurityTokenReference/wsse:Reference", WsSecurityTestSupport.Namespaces(document), "certificate token reference");

        Assert.AreEqual(WsSecurityTestSupport.X509TokenValueType, keyInfoReference.GetAttribute("ValueType"), wire);
    }

    [DataTestMethod]
    [DataRow(true, DisplayName = "SAML 2.0")]
    [DataRow(false, DisplayName = "SAML 1.1")]
    public void BuildRequest_WithAHolderOfKeyAssertion_SignsWithTheNamedKeyAndPointsKeyInfoAtTheAssertion_Test(bool saml20)
    {
        var assertion = saml20
            ? SamlTestSupport.HolderOfKey20(WsSecurityTestSupport.SigningCertificate)
            : SamlTestSupport.HolderOfKey11(WsSecurityTestSupport.SigningCertificate);

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.HolderOfKeySecurity(assertion)),
            "Build");

        var document = WsSecurityTestSupport.ParseWire(wire);
        var namespaces = WsSecurityTestSupport.Namespaces(document);
        var children = WsSecurityFoundationTestSupport.SecurityHeader(document).ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Select(element => element.LocalName).ToArray();

        CollectionAssert.AreEqual(new[] { "Timestamp", "Assertion", "Signature" }, children, $"{string.Join(", ", children)} | {wire}");

        var keyIdentifier = WsSecurityTestSupport.RequireNode(
            document, "//wsse:Security/ds:Signature/ds:KeyInfo/wsse:SecurityTokenReference/wsse:KeyIdentifier", namespaces, "SAML key identifier");

        Assert.AreEqual(SamlTestSupport.IdOf(assertion), keyIdentifier.InnerText, wire);
        Assert.AreEqual(saml20 ? SamlTestSupport.SamlIdValueType : SamlTestSupport.SamlAssertionIdValueType, keyIdentifier.GetAttribute("ValueType"), wire);
        Assert.IsFalse(keyIdentifier.HasAttribute("EncodingType"), wire);

        var tokenReference = (XmlElement)keyIdentifier.ParentNode;

        Assert.AreEqual(
            saml20 ? SamlTestSupport.SamlV20TokenType : SamlTestSupport.SamlV11TokenType,
            tokenReference.GetAttribute("TokenType", WsSecurityFoundationTestSupport.Wsse11Namespace),
            wire);

        var references = SamlTestSupport.MessageSignatureReferences(document);

        Assert.IsTrue(references.Contains("#" + SamlTestSupport.IdOf(assertion)), $"{string.Join(", ", references)} | {wire}");

        Assert.IsTrue(references.Contains("#" + WsSecurityTestSupport.SignedBodyId(wire)), wire);

        var signature = WsSecurityTestSupport.RequireNode(document, "//wsse:Security/ds:Signature", namespaces, "message signature");
        var signedXml = new SamlAssertionSignedXml(document);

        signedXml.LoadXml(signature);

        using var key = WsSecurityTestSupport.SigningCertificate.GetRSAPublicKey();

        Assert.IsTrue(signedXml.CheckSignature(key), wire);
    }

    [DataTestMethod]
    [DataRow(true, DisplayName = "SAML 2.0")]
    [DataRow(false, DisplayName = "SAML 1.1")]
    public void BuildRequest_WithAPrettyPrintedSignedAssertion_PreservesTheWhitespaceTheIssuerSignatureDigested_Test(bool saml20)
    {
        var assertion = SamlTestSupport.Assertion(issuance =>
        {
            issuance.Saml20 = saml20;
            issuance.PrettyPrint = true;
        });

        Assert.IsTrue(assertion.OuterXml.Contains("\r\n    "), $"{assertion.OuterXml}");

        Assert.IsTrue(SamlTestSupport.VerifyIssuerSignature(assertion, SamlTestSupport.IssuerCertificate));

        Assert.IsFalse(SamlTestSupport.VerifyIssuerSignature(SamlTestSupport.ThroughLinqToXml(assertion), SamlTestSupport.IssuerCertificate));

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            "Build");

        var carried = SamlTestSupport.AssertionIn(WsSecurityTestSupport.ParseWire(wire));
        var verifiesAfterCarry = SamlTestSupport.VerifyIssuerSignature(carried, SamlTestSupport.IssuerCertificate);

        TestContext.WriteLine($"SAML {(saml20 ? "2.0" : "1.1")}: whitespace text nodes in the issued assertion={CountWhitespaceNodes(assertion)}, in the carried assertion={CountWhitespaceNodes(carried)}; issuer signature verifies after carry={verifiesAfterCarry}; after LINQ to XML={SamlTestSupport.VerifyIssuerSignature(SamlTestSupport.ThroughLinqToXml(assertion), SamlTestSupport.IssuerCertificate)}");

        Assert.IsTrue(verifiesAfterCarry, wire);

        Assert.AreEqual(CountWhitespaceNodes(assertion), CountWhitespaceNodes(carried));

        Assert.IsFalse(SamlTestSupport.VerifyIssuerSignature(carried, WsSecurityTestSupport.SigningCertificate));
    }

    private static int CountWhitespaceNodes(XmlElement element)
        => element.SelectNodes("descendant::text()").Cast<XmlNode>().Count(node => node is XmlWhitespace || node is XmlSignificantWhitespace);

    [TestMethod]
    public void BuildRequest_WithAHolderOfKeyAssertionAndSignAssertionOff_KeepsTheKeyInfoButLeavesTheAssertionUncovered_Test()
    {
        var assertion = SamlTestSupport.HolderOfKey20(WsSecurityTestSupport.SigningCertificate);

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.HolderOfKeySecurity(assertion, security => security.SamlToken.SignAssertion = false)),
            "Build");

        var document = WsSecurityTestSupport.ParseWire(wire);
        var references = SamlTestSupport.MessageSignatureReferences(document);

        Assert.IsFalse(references.Contains("#" + SamlTestSupport.IdOf(assertion)), $"{string.Join(", ", references)} | {wire}");
        Assert.AreEqual(2, references.Count, $"{string.Join(", ", references)} | {wire}");

        var keyIdentifier = WsSecurityTestSupport.RequireNode(
            document, "//wsse:Security/ds:Signature/ds:KeyInfo/wsse:SecurityTokenReference/wsse:KeyIdentifier", WsSecurityTestSupport.Namespaces(document), "SAML key identifier");

        Assert.AreEqual(SamlTestSupport.IdOf(assertion), keyIdentifier.InnerText, wire);
    }

    [TestMethod]
    public void BuildRequest_WithAHolderOfKeyAssertion_LeavesTheIssuerSignatureVerifiableNextToTheMessageSignature_Test()
    {
        var assertion = SamlTestSupport.HolderOfKey20(WsSecurityTestSupport.SigningCertificate, issuance => issuance.PrettyPrint = true);

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.HolderOfKeySecurity(assertion)),
            "Build");

        var document = WsSecurityTestSupport.ParseWire(wire);
        var carried = SamlTestSupport.AssertionIn(document);

        Assert.IsTrue(SamlTestSupport.VerifyIssuerSignature(carried, SamlTestSupport.IssuerCertificate), wire);

        var signatures = document.SelectNodes("//ds:Signature", WsSecurityTestSupport.Namespaces(document));

        Assert.AreEqual(2, signatures.Count, wire);
    }

    [TestMethod]
    public void BuildRequest_WithAHolderOfKeyAssertionOverHttp_IsNotRefusedBecausePossessionIsProven_Test()
    {
        var built = WsSecurityTestSupport.Client(SoapProtocolType.SOAP_1_2).BuildRequest(
            HttpMethod.Post,
            new SoapClientCallAssist.Dto.Public.BuildSoapRequestDto
            {
                Client = new SoapClientCallAssist.Dto.Public.HttpClientDto(SamlTestSupport.InsecureEndpoint),
                Envelope = new SoapClientCallAssist.Dto.Public.SoapEnvelopeDto(new[] { WsSecurityTestSupport.DefaultBody() }, null, WsSecurityTestSupport.Action),
                Security = SamlTestSupport.HolderOfKeySecurity(SamlTestSupport.HolderOfKey20(WsSecurityTestSupport.SigningCertificate))
            });

        WsSecurityTestSupport.Wire(built, "Build");
    }
}
