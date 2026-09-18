#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Net.Http;
using System.Text;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class WsSecuritySignThenEncodeDefectTests
{

    private const string AsciiPayload = "order-4711";

    private const string NonAsciiPayload = "order-€-日本";

    private const string NonAsciiPayloadAfterLatin1 = "order-?-??";

    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    public TestContext TestContext { get; set; }

    [TestMethod]
    public void BuildRequest_SignedAsUtf8WithANonAsciiPayload_ShipsThePayloadIntactAndStillVerifies_Test()
    {
        var wire = ShippedWire(Encoding.UTF8, NonAsciiPayload, "Sign");

        var intact = wire.Contains(NonAsciiPayload, StringComparison.Ordinal);

        Report("UTF-8", "non-ASCII", intact, wire);

        Assert.IsTrue(intact, $"DEFECT-SIGN-THEN-ENCODE {wire}");

        WsSecurityAssert.Accepted(
            new WsSecurityMessageVerifier().Verify(wire, WsSecurityTestSupport.SigningCertificate),
            "");
    }

    [TestMethod]
    public void BuildRequest_SignedAsIso88591WithAnAsciiOnlyPayload_ShipsThePayloadIntactAndStillVerifies_Test()
    {
        var wire = ShippedWire(Latin1, AsciiPayload, "Sign");

        var intact = wire.Contains(AsciiPayload, StringComparison.Ordinal);

        Report("ISO-8859-1", "ASCII", intact, wire);

        Assert.IsTrue(intact, $"DEFECT-SIGN-THEN-ENCODE {wire}");

        WsSecurityAssert.Accepted(
            new WsSecurityMessageVerifier().Verify(wire, WsSecurityTestSupport.SigningCertificate),
            "");
    }

    [TestMethod]
    public void BuildRequest_SignedAsIso88591WithANonAsciiPayload_ReportsSuccessButShipsAMangledPayload_Test()
    {
        var built = BuildSigned(Latin1, NonAsciiPayload);

        Assert.IsTrue(built.IsSuccess, $"DEFECT-SIGN-THEN-ENCODE {NegativeTestSupport.Describe(built)}");

        var wire = WsSecurityTestSupport.Wire(built, "Sign");

        Report("ISO-8859-1", "non-ASCII", wire.Contains(NonAsciiPayload, StringComparison.Ordinal), wire);

        Assert.IsFalse(wire.Contains(NonAsciiPayload, StringComparison.Ordinal), $"DEFECT-SIGN-THEN-ENCODE {wire}");

        Assert.IsTrue(
            wire.Contains(NonAsciiPayloadAfterLatin1, StringComparison.Ordinal),
            $"DEFECT-SIGN-THEN-ENCODE {NonAsciiPayload} | {NonAsciiPayloadAfterLatin1} | {wire}");
    }

    [TestMethod]
    public void BuildRequest_SignedAsIso88591WithANonAsciiPayload_ReportsSuccessButShipsAnUnverifiableSignature_Test()
    {
        var wire = ShippedWire(Latin1, NonAsciiPayload, "Sign");

        WsSecurityAssert.Rejected(
            new WsSecurityMessageVerifier().Verify(wire, WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.SignatureVerificationCode,
            "");
    }

    [TestMethod]
    [Ignore("DEFECT-SIGN-THEN-ENCODE: unpins when fixed")]
    public void BuildRequest_SignedAsIso88591WithANonAsciiPayload_ShouldEitherFailOrShipAVerifiableSignature_Test()
    {
        var built = BuildSigned(Latin1, NonAsciiPayload);

        if (built.IsSuccess is false)
        {
            Assert.IsNull(built.Response);

            return;
        }

        var wire = WsSecurityTestSupport.Wire(built, "Sign");

        Assert.IsTrue(wire.Contains(NonAsciiPayload, StringComparison.Ordinal), wire);

        WsSecurityAssert.Accepted(
            new WsSecurityMessageVerifier().Verify(wire, WsSecurityTestSupport.SigningCertificate),
            "");
    }

    private void Report(string encoding, string payload, bool intact, string wire)
        => TestContext.WriteLine(
            $"DEFECT-SIGN-THEN-ENCODE matrix: encoding={encoding,-10} payload={payload,-9} mangled={!intact,-5} "
            + $"verifies={new WsSecurityMessageVerifier().Verify(wire, WsSecurityTestSupport.SigningCertificate).IsSuccess}");

    private static string ShippedWire(Encoding bodyEncoding, string payload, string what)
        => WsSecurityTestSupport.Wire(BuildSigned(bodyEncoding, payload), what);

    private static IResult<HttpRequestMessage> BuildSigned(Encoding bodyEncoding, string payload)
        => SoapClientFactoryHelper.CreateSoap12Client().BuildRequest(
            HttpMethod.Post,
            new BuildSoapRequestDto
            {
                Client = new HttpClientDto(WsSecurityTestSupport.Endpoint, bodyEncoding),
                Envelope = new SoapEnvelopeDto(
                    new[] { WsSecurityTestSupport.Body(payload) }, null, WsSecurityTestSupport.Action),
                Security = WsSecurityTestSupport.Security()
            });

}
