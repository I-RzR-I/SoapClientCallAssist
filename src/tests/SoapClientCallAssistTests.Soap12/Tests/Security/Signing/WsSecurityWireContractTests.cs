#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class WsSecurityWireContractTests
{

    [TestMethod]
    public void SignedRequest_BodyDigest_MatchesAnIndependentExclusiveC14NComputation_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire();

        var document = WsSecurityTestSupport.ParseWire(wire);
        var namespaces = WsSecurityTestSupport.Namespaces(document);

        var body = WsSecurityTestSupport.RequireNode(
            document, "/*/*[local-name()='Body']", namespaces, "signed Body");

        var bodyId = WsSecurityTestSupport.WsuId(body);

        Assert.IsFalse(string.IsNullOrEmpty(bodyId), wire);

        var reference = WsSecurityTestSupport.RequireNode(
            document,
            $"//ds:Signature/ds:SignedInfo/ds:Reference[@URI='#{bodyId}']",
            namespaces,
            "ds:Reference");

        var digestValue = WsSecurityTestSupport.RequireNode(
            reference, "ds:DigestValue", namespaces, "ds:DigestValue");

        var independent = WsSecurityTestSupport.IndependentExclusiveC14NDigest(body);

        Assert.AreEqual(independent, digestValue.InnerText, $"{bodyId}");
    }

    [TestMethod]
    public void SignedRequest_TimestampDigest_MatchesAnIndependentExclusiveC14NComputation_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire();

        var document = WsSecurityTestSupport.ParseWire(wire);
        var namespaces = WsSecurityTestSupport.Namespaces(document);

        var timestamp = WsSecurityTestSupport.RequireNode(
            document, "//wsse:Security/wsu:Timestamp", namespaces, "signed wsu:Timestamp");

        var timestampId = WsSecurityTestSupport.WsuId(timestamp);

        var reference = WsSecurityTestSupport.RequireNode(
            document,
            $"//ds:Signature/ds:SignedInfo/ds:Reference[@URI='#{timestampId}']",
            namespaces,
            "ds:Reference");

        var digestValue = WsSecurityTestSupport.RequireNode(
            reference, "ds:DigestValue", namespaces, "ds:DigestValue");

        Assert.AreEqual(WsSecurityTestSupport.IndependentExclusiveC14NDigest(timestamp), digestValue.InnerText, $"{timestampId}");
    }

    [TestMethod]
    public void SignedRequest_WhenReparsedFromTheWire_VerifiesAndReportsBodyAndTimestampCoverage_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire();

        var coverage = WsSecurityAssert.Accepted(
            new WsSecurityMessageVerifier().Verify(wire, WsSecurityTestSupport.SigningCertificate),
            "");

        Assert.IsTrue(coverage.BodySigned, WsSecurityAssert.DescribeCoverage(coverage));

        Assert.IsTrue(coverage.TimestampSigned, WsSecurityAssert.DescribeCoverage(coverage));

        Assert.AreEqual(2, coverage.SignedElementIds.Count(), WsSecurityAssert.DescribeCoverage(coverage));
    }

    [TestMethod]
    public void BuildRequest_WithNullDisabledAndAbsentSecurity_ProducesByteIdenticalContent_Test()
    {
        var withNullSecurity = WsSecurityTestSupport.WireBytes(
            WsSecurityTestSupport.BuildPost(null), "Build");

        var withDisabledSecurity = WsSecurityTestSupport.WireBytes(
            WsSecurityTestSupport.BuildPost(new SoapSecurityDto { Enabled = false }),
            "Build");

        var withAbsentSecurity = WsSecurityTestSupport.WireBytes(
            WsSecurityTestSupport.Build(SoapProtocolType.SOAP_1_2, HttpMethod.Post, null),
            "Build");

        CollectionAssert.AreEqual(withNullSecurity, withDisabledSecurity, $"{withNullSecurity.Length} | {withDisabledSecurity.Length}");

        CollectionAssert.AreEqual(withNullSecurity, withAbsentSecurity, $"{withNullSecurity.Length} | {withAbsentSecurity.Length}");
    }

    [TestMethod]
    public void BuildRequest_WithDisabledSecurity_EmitsNoWsSecurityHeader_Test()
    {
        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(new SoapSecurityDto { Enabled = false }),
            "Build");

        Assert.IsFalse(wire.Contains("wsse:Security", StringComparison.Ordinal), wire);

        Assert.IsFalse(wire.Contains("Signature", StringComparison.Ordinal), wire);
    }

    [TestMethod]
    public void SignedRequest_Wire_CarriesNoLineBreakInsideTheSignedBodyOrTimestamp_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire();

        var body = Slice(wire, "<soap:Body", "</soap:Body>");
        var timestamp = Slice(wire, "<wsu:Timestamp", "</wsu:Timestamp>");

        AssertNoLineBreak(body, "signed Body");
        AssertNoLineBreak(timestamp, "signed wsu:Timestamp");
        AssertNoLineBreak(wire, "whole signed envelope");

        WsSecurityAssert.Accepted(
            new WsSecurityMessageVerifier().Verify(wire, WsSecurityTestSupport.SigningCertificate),
            "");
    }

    [TestMethod]
    public void BuildRequest_Unsigned_WritesTheEnvelopeAsCompactAsTheSignedOne_Test()
    {
        var unsigned = WsSecurityTestSupport.Wire(WsSecurityTestSupport.BuildPost(null), "Build");

        AssertNoLineBreak(unsigned, "unsigned envelope");

        var signedBody = Regex.Replace(
            Slice(WsSecurityTestSupport.SignedWire(), "<soap:Body", "</soap:Body>"),
            " (?:xmlns:wsu|wsu:Id)=\"[^\"]*\"",
            string.Empty);

        Assert.AreEqual(Slice(unsigned, "<soap:Body", "</soap:Body>"), signedBody);
    }

    private static string Slice(string wire, string start, string end)
    {
        var from = wire.IndexOf(start, StringComparison.Ordinal);
        var to = wire.IndexOf(end, StringComparison.Ordinal);

        Assert.IsTrue(from >= 0 && to > from, $"{start} | {end} | {wire}");

        return wire[from..(to + end.Length)];
    }

    private static void AssertNoLineBreak(string text, string what)
    {
        var carriageReturns = text.Count(character => character == '\r');
        var lineFeeds = text.Count(character => character == '\n');

        Assert.IsTrue(carriageReturns == 0 && lineFeeds == 0, $"{what} | {carriageReturns} | {lineFeeds} | {text}");
    }

    [TestMethod]
    public void SignedRequest_Content_CarriesNoByteOrderMark_Test()
    {
        var bytes = WsSecurityTestSupport.WireBytes(
            WsSecurityTestSupport.BuildPost(WsSecurityTestSupport.Security()), "Build");

        Assert.IsTrue(bytes.Length > 3, $"{bytes.Length}");

        var hasBom = bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

        Assert.IsFalse(hasBom, $"{bytes[0]:X2} | {bytes[1]:X2} | {bytes[2]:X2}");
    }
}
