#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using SoapClientCallAssistTests.Soap12.Helpers.Wire;
using System;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Verification;

[TestClass]
public sealed class WsSecurityRegressionPinTests
{

    private readonly WsSecurityMessageVerifier _verifier = new WsSecurityMessageVerifier();

    [TestMethod]
    public void Verify_WhenASecondDecoyBodyIsAppended_RejectsTheMessage_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();
        var wrapped = WsSecurityWireMutator.WithAppendedDecoyBody(signed);

        WsSecurityAssert.Rejected(
            _verifier.Verify(wrapped, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.BodyNotSignedCode,
            "");
    }

    [TestMethod]
    public void Verify_WithAnExpiredSignedTimestamp_RejectsTheMessage_Test()
    {
        var expired = WsSecurityTestSupport.SignedWire(
            WsSecurityTestSupport.Security(security => security.TimestampTimeToLive = TimeSpan.FromMinutes(-10)));

        WsSecurityAssert.Rejected(
            _verifier.Verify(expired, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.TimestampWindowCode,
            "");
    }

    [TestMethod]
    public void Verify_WhenTheBodyCarriesAnOrdinaryElementNamedTimestamp_StillReportsTheSignedTimestamp_Test()
    {
        var body = new XElement(
            WsSecurityTestSupport.Service + "AddRecord",
            new XElement(WsSecurityTestSupport.Service + "Id", "7"),
            new XElement(WsSecurityTestSupport.Service + "Timestamp", "2026-01-01T00:00:00Z"));

        var signed = WsSecurityTestSupport.SignedWire(WsSecurityTestSupport.Security(), body);

        var coverage = WsSecurityAssert.Accepted(
            _verifier.Verify(signed, WsSecurityTestSupport.SigningCertificate),
            "");

        Assert.IsTrue(coverage.TimestampSigned, WsSecurityAssert.DescribeCoverage(coverage));

        Assert.IsTrue(coverage.Created.HasValue && coverage.Expires.HasValue, WsSecurityAssert.DescribeCoverage(coverage));
    }

    [TestMethod]
    public void Verify_WhenAReferenceDigestMethodIsMd5_RejectsTheMessage_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();
        var weakened = WsSecurityWireMutator.WithDigestMethod(signed, WsSecurityTestSupport.Md5Digest);

        WsSecurityAssert.Rejected(
            _verifier.Verify(weakened, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.AlgorithmCode,
            "");
    }

    [TestMethod]
    public void Verify_WhenAReferenceDigestMethodIsSha1_RejectsTheMessageUnderTheDefaultPolicy_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire(
            WsSecurityTestSupport.Security(security => security.DigestAlgorithm = SoapDigestAlgorithmType.Sha1));

        WsSecurityAssert.Rejected(
            _verifier.Verify(signed, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.AlgorithmCode,
            "");
    }

    [TestMethod]
    public void Verify_WithRequireBodySignedDisabled_AcceptsAMessageCarryingADecoyBody_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();
        var wrapped = WsSecurityWireMutator.WithAppendedDecoyBody(signed);

        var policy = new SoapVerificationPolicyDto { RequireBodySigned = false };

        var coverage = WsSecurityAssert.Accepted(
            _verifier.Verify(wrapped, WsSecurityTestSupport.SigningCertificate, policy),
            "");

        Assert.IsFalse(coverage.BodySigned, WsSecurityAssert.DescribeCoverage(coverage));
    }

    [TestMethod]
    public void Verify_WithRequireValidTimestampDisabled_AcceptsAnExpiredMessage_Test()
    {
        var expired = WsSecurityTestSupport.SignedWire(
            WsSecurityTestSupport.Security(security => security.TimestampTimeToLive = TimeSpan.FromMinutes(-10)));

        var policy = new SoapVerificationPolicyDto { RequireValidTimestamp = false };

        var coverage = WsSecurityAssert.Accepted(
            _verifier.Verify(expired, WsSecurityTestSupport.SigningCertificate, policy),
            "");

        Assert.IsTrue(coverage.Expires.HasValue && coverage.Expires.Value < DateTimeOffset.UtcNow, WsSecurityAssert.DescribeCoverage(coverage));
    }

    [TestMethod]
    public void Verify_WithAllowSha1Algorithms_AcceptsASha1ReferenceDigest_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire(
            WsSecurityTestSupport.Security(security => security.DigestAlgorithm = SoapDigestAlgorithmType.Sha1));

        var policy = new SoapVerificationPolicyDto { AllowSha1Algorithms = true };

        var coverage = WsSecurityAssert.Accepted(
            _verifier.Verify(signed, WsSecurityTestSupport.SigningCertificate, policy),
            "");

        Assert.IsTrue(coverage.BodySigned, WsSecurityAssert.DescribeCoverage(coverage));
    }

    [TestMethod]
    public void Verify_WithAllowSha1Algorithms_StillRejectsMd5_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();
        var weakened = WsSecurityWireMutator.WithDigestMethod(signed, WsSecurityTestSupport.Md5Digest);

        var policy = new SoapVerificationPolicyDto { AllowSha1Algorithms = true };

        WsSecurityAssert.Rejected(
            _verifier.Verify(weakened, WsSecurityTestSupport.SigningCertificate, policy),
            WsSecurityTestSupport.AlgorithmCode,
            "");
    }

    [TestMethod]
    public void Verify_WithRequireBodySignedDisabled_StillRejectsATamperedBody_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire();
        var tampered = WsSecurityWireMutator.WithTamperedBody(signed);

        var policy = new SoapVerificationPolicyDto { RequireBodySigned = false };

        WsSecurityAssert.Rejected(
            _verifier.Verify(tampered, WsSecurityTestSupport.SigningCertificate, policy),
            WsSecurityTestSupport.SignatureVerificationCode,
            "");
    }

    [TestMethod]
    public void Verify_WhenTheSignedTimestampIsWithinItsWindow_ReportsAWindowMatchingTheConfiguredTimeToLive_Test()
    {
        var timeToLive = TimeSpan.FromMinutes(7);

        var signed = WsSecurityTestSupport.SignedWire(
            WsSecurityTestSupport.Security(security => security.TimestampTimeToLive = timeToLive));

        var coverage = WsSecurityAssert.Accepted(
            _verifier.Verify(signed, WsSecurityTestSupport.SigningCertificate),
            "");

        Assert.IsTrue(coverage.Created.HasValue && coverage.Expires.HasValue, WsSecurityAssert.DescribeCoverage(coverage));

        Assert.AreEqual(
            timeToLive,
            coverage.Expires.Value - coverage.Created.Value, WsSecurityAssert.DescribeCoverage(coverage));
    }

    [TestMethod]
    public void Verify_WhenTheSignatureCoversOnlyTheTimestamp_RejectsTheMessage_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire(
            WsSecurityTestSupport.Security(security => security.SignBody = false));

        var coverage = _verifier.Verify(signed, WsSecurityTestSupport.SigningCertificate);

        WsSecurityAssert.Rejected(
            coverage,
            WsSecurityTestSupport.BodyNotSignedCode,
            "");
    }

    [TestMethod]
    public void Verify_WhenTheSignatureCoversOnlyTheBody_RejectsTheMessageForWantOfATimestamp_Test()
    {
        var signed = WsSecurityTestSupport.SignedWire(
            WsSecurityTestSupport.Security(security =>
            {
                security.IncludeTimestamp = false;
                security.SignTimestamp = false;
            }));

        var result = _verifier.Verify(signed, WsSecurityTestSupport.SigningCertificate);

        WsSecurityAssert.Rejected(
            result,
            WsSecurityTestSupport.TimestampNotSignedCode,
            "");

        var relaxed = _verifier.Verify(
            signed,
            WsSecurityTestSupport.SigningCertificate,
            new SoapVerificationPolicyDto { RequireValidTimestamp = false });

        var coverage = WsSecurityAssert.Accepted(
            relaxed,
            "");

        Assert.IsTrue(coverage.BodySigned, WsSecurityAssert.DescribeCoverage(coverage));

        Assert.AreEqual(1, coverage.SignedElementIds.Count(), WsSecurityAssert.DescribeCoverage(coverage));
    }
}
