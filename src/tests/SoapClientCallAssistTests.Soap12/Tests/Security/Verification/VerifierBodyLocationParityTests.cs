#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using SoapClientCallAssistTests.Soap12.Helpers.Wire;
using System;
using System.Linq;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Verification;

[TestClass]
public sealed class VerifierBodyLocationParityTests
{

    private const string NoSingleBodyCode = "ER-BEC-GRB-01";

    private const string SignatureStart = "<Signature ";

    private const string SignatureEnd = "</Signature>";

    private const string BodyEnd = "</soap:Body>";

    private static readonly string DecoyStart =
        $"<Decoy xmlns:wsse=\"{WsSecurityTestSupport.WsseNamespace}\" xmlns:wsu=\"{WsSecurityTestSupport.WsuNamespace}\">";

    private const string DecoyEnd = "</Decoy>";

    private WsSecurityMessageVerifier _verifier;

    private ISoapClientEndpoint _client;

    [TestInitialize]
    public void Initialize()
    {
        _verifier = new WsSecurityMessageVerifier();
        _client = SoapClientFactoryHelper.CreateDirectSoap12Client();
    }

    [TestMethod]
    public void Verify_AndGetXmlNodeResponseBody_CreditAndHandOverTheSameBodyElement_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();

        var coverage = WsSecurityAssert.Accepted(
            _verifier.Verify(signed, WsSecurityTestSupport.SigningCertificate),
            "");

        Assert.IsTrue(coverage.BodySigned, WsSecurityAssert.DescribeCoverage(coverage));

        var payload = _client.GetXmlNodeResponseBody(signed);

        Assert.IsTrue(payload.IsSuccess, NegativeTestSupport.Describe(payload));

        var body = payload.Response.ParentNode as XmlElement;

        Assert.IsNotNull(body);
        Assert.AreEqual("Body", body.LocalName);
        Assert.AreSame(body.OwnerDocument.DocumentElement, body.ParentNode);

        var consumedBodyId = WsSecurityTestSupport.WsuId(body);

        Assert.IsTrue(coverage.SignedElementIds.Contains(consumedBodyId), $"{consumedBodyId} | {string.Join(",", coverage.SignedElementIds)}");
    }

    [TestMethod]
    public void Verify_AndGetXmlNodeResponseBody_BothRefuseAnAppendedDecoyBody_Test()
    {
        var wrapped = WsSecurityWireMutator.WithAppendedDecoyBody(WsSecurityTestSupport.SignedWire());

        WsSecurityAssert.Rejected(
            _verifier.Verify(wrapped, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.BodyNotSignedCode,
            "");

        var payload = _client.GetXmlNodeResponseBody(wrapped);

        Assert.IsFalse(payload.IsSuccess, NegativeTestSupport.Describe(payload));

        Assert.AreEqual(NoSingleBodyCode, NegativeTestSupport.Messages(payload)[0].Key, NegativeTestSupport.Describe(payload));
    }

    [TestMethod]
    public void Verify_WithTheSignatureMovedOutOfTheSecurityHeaderIntoTheBody_RejectsWithTheSignatureCountCode_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();
        var signature = SignatureElement(signed);

        var moved = WsSecurityWireMutator.ReplaceOnce(
            WsSecurityWireMutator.WithoutSignature(signed), BodyEnd, DecoyStart + signature + DecoyEnd + BodyEnd);

        Assert.IsTrue(moved.Contains(SignatureStart, StringComparison.Ordinal));

        WsSecurityAssert.Rejected(
            _verifier.Verify(moved, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.SignatureCountCode,
            "");
    }

    [TestMethod]
    public void Verify_WithADecoySignaturePlantedInTheBodyBesideTheRealOne_VerifiesTheHeaderSignatureAndDetectsTheTamper_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();
        var signature = SignatureElement(signed);

        var planted = WsSecurityWireMutator.ReplaceOnce(signed, BodyEnd, DecoyStart + signature + DecoyEnd + BodyEnd);

        WsSecurityAssert.Rejected(
            _verifier.Verify(planted, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.SignatureVerificationCode,
            "");
    }

    private static string SignatureElement(string wire)
    {
        var start = wire.IndexOf(SignatureStart, StringComparison.Ordinal);
        var end = wire.IndexOf(SignatureEnd, StringComparison.Ordinal) + SignatureEnd.Length;

        Assert.IsTrue(start >= 0 && end > start, wire);

        return wire[start..end];
    }
}
