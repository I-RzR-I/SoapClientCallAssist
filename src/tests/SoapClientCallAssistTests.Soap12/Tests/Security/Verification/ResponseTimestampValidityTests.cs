#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Client;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using SoapClientCallAssistTests.Soap12.Helpers.Wire;
using System;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Verification;

[TestClass]
public sealed class ResponseTimestampValidityTests
{
    private const int DefaultSkewSeconds = 300;

    private const int WindowSeconds = 600;

    private Soap12Client _client;

    private string _signed;

    private DateTimeOffset _anchor;

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public void Initialize()
    {
        _client = new Soap12Client();
        _signed = WsSecurityTestSupport.SignedWire();
        _anchor = DateTimeOffset.UtcNow;
    }

    [TestMethod]
    public void VerifyResponseSignature_WithARewrittenAndResignedTimestampInsideTheWindow_IsAccepted_Test()
    {
        var coverage = WsSecurityAssert.Accepted(
            Verify(Resigned(-WindowSeconds, WindowSeconds)),
            "");

        Assert.IsTrue(coverage.TimestampSigned, WsSecurityAssert.DescribeCoverage(coverage));

        Assert.IsTrue(coverage.Created.HasValue && coverage.Expires.HasValue, WsSecurityAssert.DescribeCoverage(coverage));
    }

    [DataTestMethod]
    [DataRow(WindowSeconds, true, "well inside the window: Expires is ten minutes in the future")]
    [DataRow(-30, true, "inside the window: Expires is 30s past, far short of the 300s skew")]
    [DataRow(-(DefaultSkewSeconds - 30), true, "just inside the window: Expires is 270s past against a 300s skew")]
    [DataRow(-DefaultSkewSeconds, false, "exactly on the boundary: Expires is 300s past against a 300s skew")]
    [DataRow(-(DefaultSkewSeconds + 30), false, "just outside the window: Expires is 330s past against a 300s skew")]
    [DataRow(-3600, false, "far outside the window: Expires is an hour past")]
    public void VerifyResponseSignature_AcrossTheClockSkewBoundary_AppliesTheDefaultFiveMinuteSkew_Test(
        int expiresOffsetSeconds, bool expectedAccepted, string side)
    {
        var wire = Resigned(expiresOffsetSeconds - WindowSeconds, expiresOffsetSeconds);
        var result = Verify(wire);

        TestContext.WriteLine($"expiresOffsetSeconds={expiresOffsetSeconds} accepted={result.IsSuccess} ({side})");

        if (expectedAccepted)
        {
            WsSecurityAssert.Accepted(
                result,
                side);

            return;
        }

        WsSecurityAssert.Rejected(
            result,
            WsSecurityTestSupport.TimestampWindowCode,
            side);
    }

    [DataTestMethod]
    [DataRow(3600, false, "an hour in the future, far beyond the 300s skew")]
    [DataRow(DefaultSkewSeconds + 60, false, "360s in the future, just beyond the 300s skew")]
    [DataRow(60, true, "60s in the future, comfortably inside the 300s skew")]
    public void VerifyResponseSignature_WithACreatedInstantInTheFuture_AppliesTheSameSkewToTheOtherEnd_Test(
        int createdOffsetSeconds, bool expectedAccepted, string side)
    {
        var wire = Resigned(createdOffsetSeconds, createdOffsetSeconds + WindowSeconds);
        var result = Verify(wire);

        TestContext.WriteLine($"createdOffsetSeconds={createdOffsetSeconds} accepted={result.IsSuccess} ({side})");

        if (expectedAccepted)
        {
            WsSecurityAssert.Accepted(
                result,
                side);

            return;
        }

        WsSecurityAssert.Rejected(
            result,
            WsSecurityTestSupport.TimestampWindowCode,
            side);
    }

    [TestMethod]
    public void VerifyResponseSignature_WithACleanlyExpiredButCorrectlySignedTimestamp_NamesTheTimestamp_Test()
    {
        var result = Verify(Resigned(-7200, -3600));

        WsSecurityAssert.Rejected(
            result,
            WsSecurityTestSupport.TimestampWindowCode,
            "");

        StringAssert.Contains(NegativeTestSupport.FirstMessageInfo(result), "wsu:Timestamp", NegativeTestSupport.Describe(result));
    }

    [TestMethod]
    public void VerifyResponseSignature_WithAnExpiredTimestampAndATimestampCheckTurnedOff_IsAccepted_Test()
        => WsSecurityAssert.Accepted(
            _client.VerifyResponseSignature(
                Resigned(-7200, -3600),
                WsSecurityTestSupport.SigningCertificate,
                new SoapVerificationPolicyDto { RequireValidTimestamp = false }),
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithAnExpiredTimestampAndAGenerousClockSkew_IsAccepted_Test()
        => WsSecurityAssert.Accepted(
            _client.VerifyResponseSignature(
                Resigned(-7200, -3600),
                WsSecurityTestSupport.SigningCertificate,
                new SoapVerificationPolicyDto { ClockSkew = TimeSpan.FromHours(2) }),
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithAnInvertedWindow_IsRejected_Test()
        => WsSecurityAssert.Rejected(
            Verify(Resigned(WindowSeconds, -WindowSeconds)),
            WsSecurityTestSupport.TimestampWindowCode,
            "");

    [TestMethod]
    public void VerifyResponseSignature_WhenTheTimestampIsRewrittenWithoutResigning_IsRejectedAsASignatureFailure_Test()
    {
        var mutated = WsSecurityWireMutator.WithTimestampWindow(
            _signed, _anchor.AddSeconds(-WindowSeconds), _anchor.AddSeconds(WindowSeconds * 10));

        WsSecurityAssert.Rejected(
            Verify(mutated),
            WsSecurityTestSupport.SignatureVerificationCode,
            "");
    }

    [TestMethod]
    public void VerifyResponseSignature_WhenTheTimestampIsRewrittenWithoutResigning_StillCarriesTheOriginalWindow_Test()
    {
        var mutated = WsSecurityWireMutator.WithTimestampWindow(
            _signed, _anchor.AddSeconds(-WindowSeconds), _anchor.AddSeconds(WindowSeconds * 10));

        StringAssert.Contains(mutated, WsSecurityTestSupport.TimestampInstant(_anchor.AddSeconds(WindowSeconds * 10)));

        Assert.AreNotEqual(_signed, mutated);
    }

    private string Resigned(int createdOffsetSeconds, int expiresOffsetSeconds)
        => WsSecurityWireMutator.ResignedWithTimestampWindow(
            _signed,
            _anchor.AddSeconds(createdOffsetSeconds),
            _anchor.AddSeconds(expiresOffsetSeconds),
            WsSecurityTestSupport.SigningCertificate);

    private IResult<SoapSignatureVerificationResult> Verify(string wire)
        => _client.VerifyResponseSignature(wire, WsSecurityTestSupport.SigningCertificate);
}
