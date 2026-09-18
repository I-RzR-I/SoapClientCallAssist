#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Saml;

[TestClass]
public sealed class SamlStructuralGateTests
{

    [TestMethod]
    public void BuildRequest_WithASecurityHeaderWrappingTheAssertion_RefusesAsNotAnAssertion_Test()
    {
        var assertion = SamlTestSupport.Bearer20();

        Refused(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(SamlTestSupport.WrapInSecurityHeader(assertion))),
            SamlTestSupport.NotAnAssertionCode,
            "",
            assertion);
    }

    [TestMethod]
    public void BuildRequest_WithAnEnvelopeAsTheAssertion_RefusesAsNotAnAssertion_Test()
    {
        var envelope = WsSecurityTestSupport.ParseWire(WsSecurityTestSupport.SignedWire()).DocumentElement;

        Refused(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(envelope)),
            SamlTestSupport.NotAnAssertionCode,
            "");
    }

    [TestMethod]
    public void BuildRequest_WithAnAssertionOutsideBothSamlNamespaces_RefusesAsNotAnAssertion_Test()
    {
        var foreign = SamlTestSupport.ForeignElement("Assertion", "urn:oasis:names:tc:SAML:3.0:assertion", "_x1");

        Refused(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(foreign)),
            SamlTestSupport.NotAnAssertionCode,
            "");
    }

    [DataTestMethod]
    [DataRow(true, DisplayName = "SAML 2.0")]
    [DataRow(false, DisplayName = "SAML 1.1")]
    public void BuildRequest_WithAnAssertionMissingItsIdentifier_RefusesUnderTheIdentifierRule_Test(bool saml20)
    {
        var assertion = SamlTestSupport.Assertion(issuance =>
        {
            issuance.Saml20 = saml20;
            issuance.Sign = false;
        });

        assertion.RemoveAttribute(saml20 ? "ID" : "AssertionID");

        Refused(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            SamlTestSupport.MissingIdCode,
            "",
            assertion);
    }

    [TestMethod]
    public void BuildRequest_WithASaml20AssertionCarryingOnlyAnAssertionIdAttribute_RefusesUnderTheIdentifierRule_Test()
    {
        var assertion = SamlTestSupport.Bearer20(issuance => issuance.Sign = false);
        var id = assertion.GetAttribute("ID");

        assertion.RemoveAttribute("ID");
        assertion.SetAttribute("AssertionID", id);

        Refused(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            SamlTestSupport.MissingIdCode,
            "",
            assertion);
    }

    [TestMethod]
    public void BuildRequest_WithABlankIdentifier_RefusesUnderTheIdentifierRule_Test()
    {
        var assertion = SamlTestSupport.Bearer20(issuance => issuance.Sign = false);

        assertion.SetAttribute("ID", "   ");

        Refused(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            SamlTestSupport.MissingIdCode,
            "",
            assertion);
    }

    [DataTestMethod]
    [DataRow(true, DisplayName = "SAML 2.0")]
    [DataRow(false, DisplayName = "SAML 1.1")]
    public void BuildRequest_WithAnExpiredAssertion_RefusesAsLapsed_Test(bool saml20)
    {
        var assertion = SamlTestSupport.Assertion(issuance =>
        {
            issuance.Saml20 = saml20;
            issuance.NotBefore = DateTime.UtcNow.AddMinutes(-30);
            issuance.NotOnOrAfter = DateTime.UtcNow.AddMinutes(-10);
        });

        Refused(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            SamlTestSupport.LapsedCode,
            "",
            assertion);
    }

    [TestMethod]
    public void BuildRequest_WithAnAssertionExpiredWithinTheSkew_IsCarried_Test()
    {
        var assertion = SamlTestSupport.Bearer20(issuance =>
        {
            issuance.NotBefore = DateTime.UtcNow.AddMinutes(-10);
            issuance.NotOnOrAfter = DateTime.UtcNow.AddMinutes(-1);
        });

        WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            "Build");
    }

    [TestMethod]
    public void BuildRequest_WithAnAssertionExpiredBeyondACallerNarrowedSkew_RefusesAsLapsed_Test()
    {
        var assertion = SamlTestSupport.Bearer20(issuance =>
        {
            issuance.NotBefore = DateTime.UtcNow.AddMinutes(-10);
            issuance.NotOnOrAfter = DateTime.UtcNow.AddMinutes(-1);
        });

        var security = SamlTestSupport.BearerOnlySecurity(assertion);
        security.SamlToken.ClockSkew = TimeSpan.FromSeconds(10);

        Refused(WsSecurityTestSupport.BuildPost(security), SamlTestSupport.LapsedCode, "", assertion);
    }

    [TestMethod]
    public void BuildRequest_WithAnUnreadableNotOnOrAfter_RefusesAsLapsed_Test()
    {
        var assertion = SamlTestSupport.Bearer20(issuance => issuance.NotOnOrAfterOverride = "tomorrow-ish");

        Refused(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            SamlTestSupport.LapsedCode,
            "",
            assertion);
    }

    [TestMethod]
    public void BuildRequest_WithAnAssertionNotYetValid_IsCarriedBecauseTheReceiverAppliesItsOwnTolerance_Test()
    {
        var assertion = SamlTestSupport.Bearer20(issuance =>
        {
            issuance.NotBefore = DateTime.UtcNow.AddMinutes(30);
            issuance.NotOnOrAfter = DateTime.UtcNow.AddMinutes(60);
        });

        WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            "Build");
    }

    [TestMethod]
    public void BuildRequest_WithASaml20SubjectConfirmationAlreadyLapsed_RefusesAsLapsed_Test()
    {
        var assertion = SamlTestSupport.Bearer20(issuance => issuance.Sign = false);

        var subject = (XmlElement)assertion.GetElementsByTagName("Subject", SamlTestSupport.Saml20Namespace)[0];
        var confirmation = (XmlElement)subject.GetElementsByTagName("SubjectConfirmation", SamlTestSupport.Saml20Namespace)[0];
        var data = assertion.OwnerDocument.CreateElement("saml", "SubjectConfirmationData", SamlTestSupport.Saml20Namespace);

        data.SetAttribute("NotOnOrAfter", XmlConvert.ToString(DateTime.UtcNow.AddMinutes(-20), XmlDateTimeSerializationMode.Utc));
        confirmation.AppendChild(data);

        Refused(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            SamlTestSupport.LapsedCode,
            "",
            assertion);
    }

    [TestMethod]
    public void BuildRequest_WithABearerAssertionForAnHttpEndpoint_RefusesUnderTheTransportRule_Test()
    {
        var assertion = SamlTestSupport.Bearer20();

        Refused(
            BuildForInsecureEndpoint(SamlTestSupport.BearerOnlySecurity(assertion)),
            SamlTestSupport.BearerOverInsecureTransportCode,
            "",
            assertion);
    }

    [TestMethod]
    public void BuildRequest_WithABearerAssertionForAnHttpEndpointAndTheOptIn_IsCarried_Test()
    {
        var assertion = SamlTestSupport.Bearer20();

        var wire = WsSecurityTestSupport.Wire(
            BuildForInsecureEndpoint(SamlTestSupport.BearerOnlySecurity(assertion, allowInsecureTransport: true)),
            "Build");

        Assert.AreEqual(assertion.OuterXml, SamlTestSupport.AssertionIn(WsSecurityTestSupport.ParseWire(wire)).OuterXml);
    }

    [TestMethod]
    public void Sign_WithABearerAssertionAndNoEndpoint_IsNotRefusedBecauseTheTransportIsUnknown_Test()
    {
        var assertion = SamlTestSupport.Bearer20();
        var soap = XNamespace.Get("http://www.w3.org/2003/05/soap-envelope");
        var envelope = new XElement(soap + "Envelope", new XElement(soap + "Body", WsSecurityTestSupport.DefaultBody()));

        var signed = new WsSecurityMessageSigner().Sign(envelope, SamlTestSupport.BearerOnlySecurity(assertion));

        Assert.IsTrue(signed.IsSuccess, NegativeTestSupport.Describe(signed));
        Assert.IsTrue(signed.Response.Contains(SamlTestSupport.IdOf(assertion)));
    }

    [TestMethod]
    public void BuildRequest_WithASamlTokenOnTheSymmetricBinding_RefusesRatherThanDroppingTheAssertion_Test()
    {
        var assertion = SamlTestSupport.Bearer20();

        Refused(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security => security.SamlToken = SamlTestSupport.Token(assertion))),
            SamlTestSupport.SymmetricFamilyCode,
            "",
            assertion);
    }

    [TestMethod]
    public void BuildRequest_WithAHolderOfKeyAssertionButNoSigningCertificate_StillRefusesUnderTheHolderOfKeyRule_Test()
    {
        var assertion = SamlTestSupport.HolderOfKey20(WsSecurityTestSupport.SigningCertificate);

        Refused(
            WsSecurityTestSupport.BuildPost(new SoapSecurityDto { Enabled = true, SamlToken = SamlTestSupport.Token(assertion, SoapSamlConfirmationType.HolderOfKey) }),
            "V-SEC-082",
            "",
            assertion);
    }

    [TestMethod]
    public void BuildRequest_WithAHolderOfKeyAssertionOverHttp_DoesNotApplyTheBearerTransportRule_Test()
    {
        var assertion = SamlTestSupport.HolderOfKey20(WsSecurityTestSupport.SigningCertificate);

        WsSecurityTestSupport.Wire(
            BuildForInsecureEndpoint(SamlTestSupport.HolderOfKeySecurity(assertion)),
            "Build");
    }

    [TestMethod]
    public void BuildRequest_WithAnAssertionCarryingAdviceAssertions_CarriesItAsOneDirectChild_Test()
    {
        var assertion = SamlTestSupport.Bearer20(issuance => issuance.Sign = false);
        var inner = SamlTestSupport.Bearer20(issuance => issuance.Sign = false);

        var advice = assertion.OwnerDocument.CreateElement("saml", "Advice", SamlTestSupport.Saml20Namespace);
        advice.AppendChild(assertion.OwnerDocument.ImportNode(inner, true));

        var conditions = SamlTestSupport.Conditions(assertion);
        assertion.InsertAfter(advice, conditions);

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            "Build");

        var carried = SamlTestSupport.AssertionIn(WsSecurityTestSupport.ParseWire(wire));

        Assert.AreEqual(1, carried.GetElementsByTagName("Assertion", SamlTestSupport.Saml20Namespace).Count, wire);
    }

    [TestMethod]
    public void BuildRequest_WithAnAdviceAssertionSharingTheOuterIdentifier_RefusesAsAmbiguous_Test()
    {
        var assertion = SamlTestSupport.Bearer20(issuance => issuance.Sign = false);
        var inner = SamlTestSupport.Bearer20(issuance => issuance.Sign = false);

        inner.SetAttribute("ID", assertion.GetAttribute("ID"));

        var advice = assertion.OwnerDocument.CreateElement("saml", "Advice", SamlTestSupport.Saml20Namespace);
        advice.AppendChild(assertion.OwnerDocument.ImportNode(inner, true));
        assertion.InsertAfter(advice, SamlTestSupport.Conditions(assertion));

        Refused(
            WsSecurityTestSupport.BuildPost(SamlTestSupport.BearerOnlySecurity(assertion)),
            SamlTestSupport.AmbiguousIdCode,
            "",
            assertion);
    }

    private static IResult<HttpRequestMessage> BuildForInsecureEndpoint(SoapSecurityDto security)
        => WsSecurityTestSupport.Client(SoapProtocolType.SOAP_1_2).BuildRequest(
            HttpMethod.Post,
            new BuildSoapRequestDto
            {
                Client = new HttpClientDto(SamlTestSupport.InsecureEndpoint),
                Envelope = new SoapEnvelopeDto(new[] { WsSecurityTestSupport.DefaultBody() }, null, WsSecurityTestSupport.Action),
                Security = security
            });

    private static void Refused(IResult built, string expectedCode, string because, XmlElement assertion = null)
        => WsSecurityFoundationTestSupport.RefusedWith(
            built,
            expectedCode,
            because,
            assertion is null ? Array.Empty<ForbiddenSecret>() : SamlTestSupport.Secrets(assertion));
}
