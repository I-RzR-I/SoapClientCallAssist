#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal static class WsSecurityAssert
{

    internal static SoapSignatureVerificationResult Accepted(
        IResult<SoapSignatureVerificationResult> result, string because)
    {
        Assert.IsTrue(result.IsSuccess, $"{because} | {NegativeTestSupport.Describe(result)}");

        Assert.IsNotNull(result.Response, because);

        return result.Response;
    }

    internal static void Rejected(IResult<SoapSignatureVerificationResult> result, string expectedCode, string because)
    {
        Assert.IsFalse(result.IsSuccess, $"{because} | {DescribeCoverage(result.Response)}");

        var codes = Codes(result);

        Assert.IsTrue(codes.Contains(expectedCode), $"{because} | {expectedCode} | {NegativeTestSupport.Describe(result)}");
    }

    internal static void RejectedWithAnyCode(IResult<SoapSignatureVerificationResult> result, string because)
    {
        Assert.IsFalse(result.IsSuccess, $"{because} | {DescribeCoverage(result.Response)}");

        Assert.IsTrue(Codes(result).Any(code => !string.IsNullOrWhiteSpace(code)), $"{because} | {NegativeTestSupport.Describe(result)}");
    }

    internal static string[] Codes(IResult result)
        => result?.Messages == null
            ? new string[0]
            : result.Messages.Select(message => message.Key).ToArray();

    internal static string DescribeCoverage(SoapSignatureVerificationResult coverage)
    {
        if (coverage == null)
            return "<no coverage reported>";

        var ids = coverage.SignedElementIds == null ? "<none>" : string.Join(",", coverage.SignedElementIds);
        var names = coverage.SignedElementLocalNames == null ? "<none>" : string.Join(",", coverage.SignedElementLocalNames);

        return $"BodySigned={coverage.BodySigned} TimestampSigned={coverage.TimestampSigned} "
            + $"Created={coverage.Created} Expires={coverage.Expires} Ids=[{ids}] Names=[{names}]";
    }
}
