#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using SoapClientCallAssistTests.Soap12.Helpers.Wire;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Verification;

[TestClass]
public sealed class WsSecurityShapeGateTests
{

    private WsSecurityMessageVerifier _verifier;

    private string _signed;

    [TestInitialize]
    public void Initialize()
    {
        _verifier = new WsSecurityMessageVerifier();
        _signed = WsSecurityTestSupport.SignedWire();
    }

    [DataTestMethod]
    [DataRow("https://attacker.invalid/payload.xml", DisplayName = "external http reference")]
    [DataRow("file:///etc/passwd", DisplayName = "external file reference")]
    [DataRow("", DisplayName = "empty reference")]
    [DataRow("#", DisplayName = "bare fragment reference")]
    [DataRow("id-without-fragment", DisplayName = "reference missing the fragment marker")]
    public void Verify_WithAReferenceThatIsNotASameDocumentFragment_RejectsTheMessage_Test(string uri)
    {
        var mutated = WsSecurityWireMutator.WithFirstReferenceUri(_signed, uri);

        WsSecurityAssert.Rejected(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.ReferenceUriCode,
            uri);
    }

    [DataTestMethod]
    [DataRow("##X", DisplayName = "double-hash fragment")]
    [DataRow("#xpointer(id('x'))", DisplayName = "xpointer id fragment")]
    [DataRow("#xpointer(/)", DisplayName = "xpointer whole-document fragment")]
    public void Verify_WithAReferenceUriThatTwoReadingsResolveDifferently_RejectsTheMessage_Test(string uri)
    {
        var mutated = WsSecurityWireMutator.WithFirstReferenceUri(_signed, uri);

        WsSecurityAssert.Rejected(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.ReferenceUriCode,
            uri);
    }

    [DataTestMethod]
    [DataRow(WsSecurityTestSupport.XsltTransform, DisplayName = "XSLT transform")]
    [DataRow(WsSecurityTestSupport.XPathTransform, DisplayName = "XPath transform")]
    [DataRow(WsSecurityTestSupport.EnvelopedSignatureTransform, DisplayName = "enveloped-signature transform")]
    [DataRow(WsSecurityTestSupport.ExclusiveC14NWithComments, DisplayName = "exclusive c14n with comments")]
    public void Verify_WithADisallowedReferenceTransform_RejectsTheMessage_Test(string transform)
    {
        var mutated = WsSecurityWireMutator.WithTransformAlgorithm(_signed, transform);

        WsSecurityAssert.Rejected(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.AlgorithmCode,
            transform);
    }

    [DataTestMethod]
    [DataRow(WsSecurityTestSupport.ExclusiveC14NWithComments, DisplayName = "exclusive c14n with comments")]
    [DataRow(WsSecurityTestSupport.XsltTransform, DisplayName = "XSLT as canonicalization")]
    public void Verify_WithADisallowedCanonicalizationMethod_RejectsTheMessage_Test(string algorithm)
    {
        var mutated = WsSecurityWireMutator.WithCanonicalizationAlgorithm(_signed, algorithm);

        WsSecurityAssert.Rejected(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.AlgorithmCode,
            algorithm);
    }

    [TestMethod]
    public void Verify_WithAnHmacSignatureMethod_RejectsTheMessage_Test()
    {
        var mutated = WsSecurityWireMutator.WithSignatureMethod(_signed, WsSecurityTestSupport.HmacSha256Signature);

        WsSecurityAssert.Rejected(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.AlgorithmCode,
            "");
    }

    [TestMethod]
    public void Verify_WithAnHmacOutputLengthElement_RejectsTheMessage_Test()
    {
        var mutated = WsSecurityWireMutator.WithHmacOutputLength(_signed);

        WsSecurityAssert.Rejected(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.AlgorithmCode,
            "");
    }

    [TestMethod]
    public void Verify_WithARetrievalMethodInKeyInfo_RejectsTheMessage_Test()
    {
        var mutated = WsSecurityWireMutator.WithRetrievalMethodInKeyInfo(_signed);

        WsSecurityAssert.Rejected(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.AlgorithmCode,
            "");
    }

    [TestMethod]
    public void Verify_WithNoReferencesAtAll_RejectsTheMessage_Test()
    {
        var mutated = WsSecurityWireMutator.WithoutReferences(_signed);

        WsSecurityAssert.Rejected(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.AlgorithmCode,
            "");
    }

    [TestMethod]
    public void Verify_WithTwoSignatureElements_RejectsTheMessage_Test()
    {
        var mutated = WsSecurityWireMutator.WithDuplicatedSignature(_signed);

        WsSecurityAssert.Rejected(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.SignatureCountCode,
            "");
    }

    [TestMethod]
    public void Verify_WithNoSignatureAtAll_RejectsTheMessage_Test()
    {
        var mutated = WsSecurityWireMutator.WithoutSignature(_signed);

        WsSecurityAssert.Rejected(
            _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.SignatureCountCode,
            "");
    }

    [TestMethod]
    public void Verify_WithAMessageThatIsBothShapeInvalidAndWronglySigned_ReportsTheShapeFailure_Test()
    {
        var tampered = WsSecurityWireMutator.WithTamperedBody(_signed);
        var mutated = WsSecurityWireMutator.WithTransformAlgorithm(tampered, WsSecurityTestSupport.XsltTransform);

        var result = _verifier.Verify(mutated, WsSecurityTestSupport.SigningCertificate);

        WsSecurityAssert.Rejected(
            result,
            WsSecurityTestSupport.AlgorithmCode,
            "");

        CollectionAssert.DoesNotContain(
            WsSecurityAssert.Codes(result),
            WsSecurityTestSupport.SignatureVerificationCode,
            NegativeTestSupport.Describe(result));
    }

    [TestMethod]
    public void Verify_WithAnEmptyResponse_RejectsTheMessage_Test()
    {
        WsSecurityAssert.RejectedWithAnyCode(
            _verifier.Verify(string.Empty, WsSecurityTestSupport.SigningCertificate),
            "");
    }

    [TestMethod]
    public void Verify_WithMalformedXml_RejectsTheMessageWithoutThrowing_Test()
    {
        WsSecurityAssert.RejectedWithAnyCode(
            _verifier.Verify("<soap:Envelope><soap:Body></soap:Envelope>", WsSecurityTestSupport.SigningCertificate),
            "");
    }
}
