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
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Hygiene;

[TestClass]
public sealed class SecurityPlannerRefusalTests
{

    private static readonly ForbiddenSecret PasswordSecret = ForbiddenSecret.OfText("the username token password", WsSecurityFoundationTestSupport.Password);

    [TestMethod]
    public void BuildRequest_WithSymmetricBindingButNoAddressing_RefusesUnderTheAddressingRule_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security => security.Addressing = null)),
            "V-SEC-033",
            "");

    [TestMethod]
    public void BuildRequest_WithSecureConversationButNoSession_RefusesUnderTheSessionRule_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SecureConversationSecurity(null)),
            "V-SEC-061",
            "");

    [TestMethod]
    public void BuildRequest_WithAnExpiredSecureConversationSession_RefusesUnderTheExpiryRule_Test()
    {
        using var session = WsSecurityFoundationTestSupport.Session(TimeSpan.FromMinutes(-1));

        WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SecureConversationSecurity(session)),
            "V-SEC-062",
            "");
    }

    [TestMethod]
    public void BuildRequest_WithADisposedSecureConversationSession_RefusesUnderTheSecretRule_Test()
    {
        var session = WsSecurityFoundationTestSupport.Session(TimeSpan.FromMinutes(5));
        session.Dispose();

        Assert.IsTrue(session.IsDisposed);

        WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SecureConversationSecurity(session)),
            "V-SEC-063",
            "");
    }

    [TestMethod]
    public void BuildRequest_WithEncryptBodyButNoAllowDecryption_RefusesUnderTheDecryptionRule_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
                security.Encryption = new SoapEncryptionDto { EncryptBody = true })),
            "V-SEC-052",
            "");

    [TestMethod]
    public void BuildRequest_WithEncryptBodyAndAllowDecryption_PassesTheDecryptionRule_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
        {
            security.Encryption = new SoapEncryptionDto { EncryptBody = true };
            security.ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true };
        }));

        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));

        built.Response.Dispose();
    }

    [TestMethod]
    public void BuildRequest_WithAllowDecryptionOnTheAsymmetricBinding_RefusesByName_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityTestSupport.Security(security =>
                security.ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true })),
            "V-SEC-053",
            "");

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void BuildRequest_WithANonPositivePlaintextCap_RefusesUnderTheCapRule_Test(int cap)
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
                security.ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true, MaxPlaintextBytes = cap })),
            "V-SEC-054",
            "");

    [TestMethod]
    public void BuildRequest_WithAPlaintextUsernameTokenOnASymmetricBinding_RefusesUnderTheEncryptedTokenRule_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
            {
                security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken();
                security.Encryption = new SoapEncryptionDto { EncryptUsernameToken = false };
            })),
            "V-SEC-038",
            "",
            PasswordSecret);

    [TestMethod]
    public void BuildRequest_WithAHolderOfKeySamlTokenButNoSigningCertificate_RefusesUnderTheHolderOfKeyRule_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(new SoapSecurityDto
            {
                Enabled = true,
                SamlToken = new SoapSamlTokenDto
                {
                    Assertion = WsSecurityFoundationTestSupport.SamlAssertion(),
                    Confirmation = SoapSamlConfirmationType.HolderOfKey
                }
            }),
            "V-SEC-082",
            "");

    [TestMethod]
    public void BuildRequest_WithACustomSignerOnASymmetricBinding_RefusesTheSigner_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
                security.Signer = new StubSoapMessageSigner(null))),
            "V-SEC-035",
            "");

    [TestMethod]
    public void BuildRequest_WithACustomVerifierOnASymmetricBinding_RefusesTheVerifier_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
                security.ResponseVerifier = new StubSoapMessageVerifier(() => null))),
            "V-SEC-035",
            "");

    [TestMethod]
    public void BuildRequest_WithAnExpectedResponseCertificateOnASymmetricBinding_RefusesItAsAmbiguous_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
                security.ExpectedResponseCertificate = WsSecurityTestSupport.OtherCertificate)),
            "V-SEC-036",
            "");

    [TestMethod]
    public void BuildRequest_WithAServiceCertificateSharingTheSigningKey_RefusesTheBinding_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
            {
                security.SigningCertificate = WsSecurityTestSupport.SigningCertificate;
                security.SymmetricBinding.ServiceCertificate = WsSecurityTestSupport.PublicOnlyCertificate;
            })),
            "V-SEC-037",
            "");

    [TestMethod]
    public void BuildRequest_WithSecurityEnabledAndNothingConfigured_KeepsFailingUnderTheSigningKeyCode_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(new SoapSecurityDto { Enabled = true }),
            "V-SEC-002",
            "");

    [TestMethod]
    public void BuildRequest_WithBothSymmetricBindingAndSecureConversation_RefusesTheAmbiguousMode_Test()
    {
        using var session = WsSecurityFoundationTestSupport.Session(TimeSpan.FromMinutes(5));

        WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
                security.SecureConversation = new SoapSecureConversationDto { Session = session })),
            "V-SEC-090",
            "");
    }

    [TestMethod]
    public void BuildRequest_WithEncryptionOnTheAsymmetricBinding_RefusesIt_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityTestSupport.Security(security =>
                security.Encryption = new SoapEncryptionDto())),
            "V-SEC-051",
            "");

    [TestMethod]
    public void BuildRequest_WithASymmetricBindingMissingItsServiceCertificate_RefusesIt_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
                security.SymmetricBinding.ServiceCertificate = null)),
            "V-SEC-039",
            "");

    [DataTestMethod]
    [DataRow(8, 32)]
    [DataRow(24, 20)]
    [DataRow(128, 32)]
    public void BuildRequest_WithDerivedKeyLengthsOutOfRange_RefusesThem_Test(int signatureKeyLength, int encryptionKeyLength)
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity(security =>
            {
                security.SymmetricBinding.SignatureKeyLength = signatureKeyLength;
                security.SymmetricBinding.EncryptionKeyLength = encryptionKeyLength;
            })),
            "V-SEC-043",
            $"{signatureKeyLength} | {encryptionKeyLength}");

    [TestMethod]
    public void BuildRequest_WithAValidSymmetricBinding_BuildsAnEncryptedKeyHeaderRatherThanDowngrading_Test()
    {
        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SymmetricSecurity()),
            "Build");

        var document = WsSecurityTestSupport.ParseWire(wire);
        var children = WsSecurityFoundationTestSupport.SecurityHeader(document).ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Select(element => element.LocalName).ToList();

        CollectionAssert.AreEqual(new[] { "Timestamp", "EncryptedKey", "DerivedKeyToken", "Signature" }, children);
    }

    [TestMethod]
    public void BuildRequest_WithAValidSecureConversation_EmitsTheSecurityContextTokenRatherThanRefusingAsNotAvailable_Test()
    {
        using var session = WsSecurityFoundationTestSupport.Session(TimeSpan.FromMinutes(5));

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.SecureConversationSecurity(session)),
            "Build");

        var document = WsSecurityTestSupport.ParseWire(wire);
        var children = WsSecurityFoundationTestSupport.SecurityHeader(document).ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Select(element => element.LocalName).ToList();

        CollectionAssert.AreEqual(new[] { "Timestamp", "SecurityContextToken", "DerivedKeyToken", "Signature" }, children, wire);

        Assert.IsTrue(wire.Contains(session.ContextIdentifier));
    }

    [TestMethod]
    public void BuildRequest_WithABearerSamlToken_IsCarriedRatherThanRefusedAsNotAvailable_Test()
    {
        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(WsSecurityTestSupport.Security(security =>
                security.SamlToken = new SoapSamlTokenDto { Assertion = WsSecurityFoundationTestSupport.SamlAssertion() })),
            "Build");

        Assert.IsTrue(wire.Contains("ID=\"_a1\""), wire);
    }

    [TestMethod]
    public void BuildRequest_WithASamlTokenMissingItsAssertion_RefusesIt_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityTestSupport.Security(security =>
                security.SamlToken = new SoapSamlTokenDto())),
            "V-SEC-081",
            "");

    [TestMethod]
    public void BuildRequest_WithTheAddressingActionContradictingTheRequestAction_RefusesUnderThePrecedenceRule_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityTestSupport.Security(security =>
                security.Addressing = WsSecurityFoundationTestSupport.Addressing(addressing => addressing.Action = "urn:some-other-action"))),
            "V-SEC-020",
            "");

    [TestMethod]
    public void Sign_WithAddressingButNoActionAnywhere_RefusesUnderTheMandatoryActionRule_Test()
    {
        var security = WsSecurityTestSupport.Security(security =>
            security.Addressing = WsSecurityFoundationTestSupport.Addressing(addressing => addressing.To = WsSecurityTestSupport.Endpoint));

        WsSecurityFoundationTestSupport.RefusedWith(
            new WsSecurityMessageSigner().Sign(Envelope(), security),
            "V-SEC-021",
            "");
    }

    [TestMethod]
    public void Sign_WithAddressingButNoDestinationAnywhere_RefusesUnderTheMandatoryDestinationRule_Test()
    {
        var security = WsSecurityTestSupport.Security(security =>
            security.Addressing = WsSecurityFoundationTestSupport.Addressing(addressing => addressing.Action = WsSecurityTestSupport.Action));

        WsSecurityFoundationTestSupport.RefusedWith(
            new WsSecurityMessageSigner().Sign(Envelope(), security),
            "V-SEC-022",
            "");
    }

    [TestMethod]
    public void BuildRequest_WithSignTokenOnATokenOnlyHeader_RefusesUnderTheSignatureRule_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security =>
                security.UsernameToken.SignToken = true)),
            "V-SEC-091",
            "",
            PasswordSecret);

    [TestMethod]
    public void BuildRequest_WithAUsernameTokenNamingNobody_RefusesIt_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security =>
                security.UsernameToken.Username = " ")),
            "V-SEC-092",
            "",
            PasswordSecret);

    [TestMethod]
    public void BuildRequest_WithADigestTokenAndNoPassword_RefusesIt_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security =>
            {
                security.UsernameToken.PasswordType = SoapPasswordType.Digest;
                security.UsernameToken.Password = null;
            })),
            "V-SEC-092",
            "");

    private static XElement Envelope()
    {
        XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";

        return new XElement(soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", soap.NamespaceName),
            new XElement(soap + "Body", WsSecurityTestSupport.DefaultBody()));
    }
}
