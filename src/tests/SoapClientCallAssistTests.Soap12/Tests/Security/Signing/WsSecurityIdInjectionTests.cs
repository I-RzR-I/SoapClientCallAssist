#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class WsSecurityIdInjectionTests
{

    private const string SigningInputCode = "V-SEC-001";

    private const string ShadowedIdCode = "V-SEC-016";

    private const string BodyIdCode = "V-SEC-017";

    [DataTestMethod]
    [DataRow("it's-mine", DisplayName = "an id carrying an apostrophe")]
    [DataRow("x' or '1'='1", DisplayName = "an id shaped like an XPath predicate")]
    [DataRow("']|//*[local-name()='Body", DisplayName = "an id that would union in every element")]
    [DataRow("extra\"quoted", DisplayName = "an id carrying a double quote")]
    [DataRow("has space", DisplayName = "an id carrying whitespace")]
    [DataRow("]]>", DisplayName = "an id shaped like a CDATA terminator")]
    public void BuildRequest_WithAnAdditionalSignedElementIdOfAnyShape_FailsWithTheSigningInputCode_Test(string id)
    {
        var built = WsSecurityTestSupport.BuildPost(
            WsSecurityTestSupport.Security(security => security.AdditionalSignedElementIds = new[] { id }));

        Assert.IsFalse(built.IsSuccess, NegativeTestSupport.Describe(built));

        Assert.IsNull(built.Response);

        Assert.AreEqual(SigningInputCode, NegativeTestSupport.Messages(built)[0].Key, NegativeTestSupport.Describe(built));

        Assert.AreEqual(
            WsSecurityTestSupport.InsufficientSigningInputMessage,
            NegativeTestSupport.FirstMessageInfo(built),
            NegativeTestSupport.Describe(built));
    }

    [DataTestMethod]
    [DataRow("it's-mine", DisplayName = "an id carrying an apostrophe")]
    [DataRow("x' or '1'='1", DisplayName = "an id shaped like an XPath predicate")]
    [DataRow("']|//*[local-name()='Body", DisplayName = "an id that would union in every element")]
    public void BuildRequest_WithAnAdditionalSignedElementIdCarryingAQuote_ReportsNoXPathParseFailure_Test(string id)
    {
        var built = WsSecurityTestSupport.BuildPost(
            WsSecurityTestSupport.Security(security => security.AdditionalSignedElementIds = new[] { id }));

        var rendered = NegativeTestSupport.Describe(built);

        Assert.IsFalse(rendered.Contains("XPath", StringComparison.OrdinalIgnoreCase), rendered);

        Assert.AreEqual(1, NegativeTestSupport.Messages(built).Count, rendered);

        SecretLeakAssert.CarriesNoSecret(built, "signing");
    }

    [TestMethod]
    public void BuildRequest_WithAnAdditionalSignedElementIdCarryingAnApostrophe_DoesNotSelectUnrelatedElements_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(
            new SoapSecurityDto
            {
                Enabled = true,
                SigningCertificate = WsSecurityTestSupport.SigningCertificate,
                SignBody = false,
                IncludeTimestamp = false,
                AdditionalSignedElementIds = new[] { "']|//*[local-name()='Body" }
            },
            TaggedBody("Only", "extra"));

        Assert.IsFalse(built.IsSuccess, NegativeTestSupport.Describe(built));

        Assert.AreEqual(SigningInputCode, NegativeTestSupport.Messages(built)[0].Key, NegativeTestSupport.Describe(built));
    }

    [TestMethod]
    public void BuildRequest_WithAnAdditionalSignedElementIdCarryingAnApostrophe_StillResolvesWhenAnElementCarriesIt_Test()
    {
        const string awkwardId = "it's-mine";

        var wire = WsSecurityTestSupport.SignedWire(
            WsSecurityTestSupport.Security(security => security.AdditionalSignedElementIds = new[] { awkwardId }),
            TaggedBody("Only", awkwardId));

        var document = WsSecurityTestSupport.ParseWire(wire);
        var namespaces = WsSecurityTestSupport.Namespaces(document);

        var references = document.SelectNodes("//ds:Signature/ds:SignedInfo/ds:Reference", namespaces);

        Assert.AreEqual(3, references.Count, wire);

        Assert.IsTrue(wire.Contains("#it&apos;s-mine", StringComparison.Ordinal) || wire.Contains("#it's-mine", StringComparison.Ordinal), wire);
    }

    [DataTestMethod]
    [DataRow("Id")]
    [DataRow("id")]
    [DataRow("ID")]
    public void BuildRequest_WithAnAdditionalSignedElementIdShadowedByAPayloadElementsUnqualifiedId_RefusesByName_Test(string attributeName)
    {
        const string id = "hdr1";

        var header = new XElement(
            WsSecurityTestSupport.Service + "Marker",
            new XAttribute(XNamespace.Get(WsSecurityTestSupport.WsuNamespace) + "Id", id),
            "marker");

        var shadow = new XElement(WsSecurityTestSupport.Service + "x", new XAttribute(attributeName, id));

        var built = WsSecurityTestSupport.BuildWithHeaders(
            SoapProtocolType.SOAP_1_2,
            HttpMethod.Post,
            WsSecurityTestSupport.Security(security => security.AdditionalSignedElementIds = new[] { id }),
            new[] { header },
            shadow);

        WsSecurityFoundationTestSupport.RefusedWith(
            built,
            ShadowedIdCode,
            $"{attributeName} | {id}");
    }

    [TestMethod]
    public void BuildRequest_WithAnAdditionalSignedElementIdCarriedOnlyByTheHeader_SignsThatHeader_Test()
    {
        const string id = "hdr1";

        var header = new XElement(
            WsSecurityTestSupport.Service + "Marker",
            new XAttribute(XNamespace.Get(WsSecurityTestSupport.WsuNamespace) + "Id", id),
            "marker");

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildWithHeaders(
                SoapProtocolType.SOAP_1_2,
                HttpMethod.Post,
                WsSecurityTestSupport.Security(security => security.AdditionalSignedElementIds = new[] { id }),
                new[] { header },
                new XElement(WsSecurityTestSupport.Service + "x", new XAttribute("Id", "other"))),
            "Build");

        CollectionAssert.Contains(WsSecurityFoundationTestSupport.SignedReferenceUris(WsSecurityTestSupport.ParseWire(wire)).ToList(), "#" + id);
    }

    [TestMethod]
    public void Sign_WithAnAdditionalSignedElementIdNamingTheBodysOwnWsuId_RefusesByNameInsteadOfDangling_Test()
    {
        const string bodyId = "body-mine";

        var soap = XNamespace.Get("http://www.w3.org/2003/05/soap-envelope");
        var envelope = new XElement(
            soap + "Envelope",
            new XElement(soap + "Header"),
            new XElement(
                soap + "Body",
                new XAttribute(XNamespace.Get(WsSecurityTestSupport.WsuNamespace) + "Id", bodyId),
                WsSecurityTestSupport.DefaultBody()));

        var signed = new WsSecurityMessageSigner().Sign(envelope, WsSecurityTestSupport.Security(security => security.AdditionalSignedElementIds = new[] { bodyId }));

        WsSecurityFoundationTestSupport.RefusedWith(
            signed,
            BodyIdCode,
            "");
    }

    [TestMethod]
    public void Sign_WithAnAdditionalSignedElementIdNamingTheBodysOwnWsuIdAndSignBodyOff_SignsTheBodyUnderThatId_Test()
    {
        const string bodyId = "body-mine";

        var soap = XNamespace.Get("http://www.w3.org/2003/05/soap-envelope");
        var envelope = new XElement(
            soap + "Envelope",
            new XElement(soap + "Header"),
            new XElement(
                soap + "Body",
                new XAttribute(XNamespace.Get(WsSecurityTestSupport.WsuNamespace) + "Id", bodyId),
                WsSecurityTestSupport.DefaultBody()));

        var signed = new WsSecurityMessageSigner().Sign(envelope, WsSecurityTestSupport.Security(security =>
        {
            security.SignBody = false;
            security.AdditionalSignedElementIds = new[] { bodyId };
        }));

        Assert.IsTrue(signed.IsSuccess, NegativeTestSupport.Describe(signed));
        CollectionAssert.Contains(WsSecurityFoundationTestSupport.SignedReferenceUris(WsSecurityTestSupport.ParseWire(signed.Response)).ToList(), "#" + bodyId);
    }

    private static XElement TaggedBody(string name, string wsuId)
        => new(
            WsSecurityTestSupport.Service + name,
            new XAttribute(XNamespace.Get(WsSecurityTestSupport.WsuNamespace) + "Id", wsuId),
            new XElement(WsSecurityTestSupport.Service + "id", name));
}
