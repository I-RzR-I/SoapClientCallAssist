#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class UsernameTokenTests
{

    private const string HostilePassword = "p<a&b\"c'd]]>e";

    private const int NonceBuilds = 1000;

    private const string InsecureTransportCode = "V-SEC-094";

    private static readonly ForbiddenSecret PasswordSecret = ForbiddenSecret.OfText("the username token password", WsSecurityFoundationTestSupport.Password);

    [TestMethod]
    public void BuildRequest_WithATextUsernameToken_EmitsTheProfileShapeInsideTheSignedHeader_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire(WsSecurityTestSupport.Security(security =>
            security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken()));

        var document = WsSecurityTestSupport.ParseWire(wire);
        var token = WsSecurityFoundationTestSupport.UsernameTokenElement(document);

        var children = token.ChildNodes.Cast<XmlNode>().Select(node => node.LocalName).ToArray();

        CollectionAssert.AreEqual(new[] { "Username", "Password", "Nonce", "Created" }, children, $"{string.Join(", ", children)} | {wire}");

        Assert.IsFalse(string.IsNullOrEmpty(WsSecurityTestSupport.WsuId(token)), wire);

        Assert.AreEqual(WsSecurityFoundationTestSupport.Username, WsSecurityFoundationTestSupport.ChildText(token, "Username"));
        Assert.AreEqual(WsSecurityFoundationTestSupport.Password, WsSecurityFoundationTestSupport.ChildText(token, "Password"));

        Assert.AreEqual(
            WsSecurityFoundationTestSupport.PasswordTextType,
            WsSecurityFoundationTestSupport.Child(token, "Password").GetAttribute("Type"),
            wire);

        Assert.AreEqual(
            WsSecurityFoundationTestSupport.Base64BinaryEncodingType,
            WsSecurityFoundationTestSupport.Child(token, "Nonce").GetAttribute("EncodingType"),
            wire);

        var security = WsSecurityFoundationTestSupport.SecurityHeader(document);
        var headerChildren = security.ChildNodes.Cast<XmlNode>().Select(node => node.LocalName).ToArray();

        CollectionAssert.AreEqual(
            new[] { "BinarySecurityToken", "Timestamp", "UsernameToken", "Signature" },
            headerChildren,
            string.Join(", ", headerChildren));
    }

    [TestMethod]
    public void BuildRequest_WithADigestUsernameToken_MatchesAnIndependentDigestVector_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire(WsSecurityTestSupport.Security(security =>
            security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken(token => token.PasswordType = SoapPasswordType.Digest)));

        var document = WsSecurityTestSupport.ParseWire(wire);
        var token = WsSecurityFoundationTestSupport.UsernameTokenElement(document);

        var password = WsSecurityFoundationTestSupport.Child(token, "Password");

        Assert.AreEqual(WsSecurityFoundationTestSupport.PasswordDigestType, password.GetAttribute("Type"), wire);

        var expected = IndependentDigest(
            WsSecurityFoundationTestSupport.ChildText(token, "Nonce"),
            WsSecurityFoundationTestSupport.ChildText(token, "Created"),
            WsSecurityFoundationTestSupport.Password);

        Assert.AreEqual(expected, password.InnerText, wire);

        Assert.IsFalse(wire.Contains(WsSecurityFoundationTestSupport.Password, StringComparison.Ordinal), wire);
    }

    [TestMethod]
    public void BuildRequest_WithADigestUsernameTokenAskingForNoNonceOrCreated_ForcesBoth_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire(WsSecurityTestSupport.Security(security =>
            security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken(token =>
            {
                token.PasswordType = SoapPasswordType.Digest;
                token.IncludeNonce = false;
                token.IncludeCreated = false;
            })));

        var token = WsSecurityFoundationTestSupport.UsernameTokenElement(WsSecurityTestSupport.ParseWire(wire));

        Assert.IsNotNull(WsSecurityFoundationTestSupport.Child(token, "Nonce"), wire);
        Assert.IsNotNull(WsSecurityFoundationTestSupport.Child(token, "Created"), wire);
    }

    [TestMethod]
    public void BuildRequest_WithATextUsernameTokenAskingForNoNonceOrCreated_OmitsBoth_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire(WsSecurityTestSupport.Security(security =>
            security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken(token =>
            {
                token.IncludeNonce = false;
                token.IncludeCreated = false;
            })));

        var token = WsSecurityFoundationTestSupport.UsernameTokenElement(WsSecurityTestSupport.ParseWire(wire));

        Assert.IsNull(WsSecurityFoundationTestSupport.Child(token, "Nonce"), wire);
        Assert.IsNull(WsSecurityFoundationTestSupport.Child(token, "Created"), wire);
    }

    [TestMethod]
    public void BuildRequest_AThousandTokenOnlyBuilds_MintNoncesThatAreDistinctAndAtLeastSixteenBytes_Test()
    {
        var nonces = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < NonceBuilds; index++)
        {
            var wire = WsSecurityTestSupport.Wire(
                WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity()),
                "Build");

            var token = WsSecurityFoundationTestSupport.UsernameTokenElement(WsSecurityTestSupport.ParseWire(wire));
            var nonce = WsSecurityFoundationTestSupport.ChildText(token, "Nonce");

            Assert.IsTrue(Convert.FromBase64String(nonce).Length >= 16, $"{index} | {nonce}");

            Assert.IsTrue(nonces.Add(nonce), $"{index} | {nonce}");
        }

        Assert.AreEqual(NonceBuilds, nonces.Count);
    }

    [TestMethod]
    public void BuildRequest_WithAUsernameToken_WritesCreatedAsUtcWithinTwoSecondsOfNow_Test()
    {
        var before = DateTimeOffset.UtcNow;

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity()),
            "Build");

        var after = DateTimeOffset.UtcNow;

        var created = WsSecurityFoundationTestSupport.ChildText(
            WsSecurityFoundationTestSupport.UsernameTokenElement(WsSecurityTestSupport.ParseWire(wire)), "Created");

        Assert.IsTrue(created.EndsWith("Z", StringComparison.Ordinal), $"{created}");

        var instant = DateTimeOffset.ParseExact(created, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        Assert.IsTrue(instant >= before.AddSeconds(-2) && instant <= after.AddSeconds(2), $"{created} | {before:O} | {after:O}");
    }

    [TestMethod]
    public void BuildRequest_WithAHostileTextPassword_EscapesItOnTheWireAndRoundTripsItExactly_Test()
    {
        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security =>
                security.UsernameToken.Password = HostilePassword)),
            "Build");

        Assert.IsFalse(wire.Contains(HostilePassword, StringComparison.Ordinal), wire);
        Assert.IsFalse(wire.Contains("]]>", StringComparison.Ordinal), wire);

        var token = WsSecurityFoundationTestSupport.UsernameTokenElement(WsSecurityTestSupport.ParseWire(wire));

        Assert.AreEqual(HostilePassword, WsSecurityFoundationTestSupport.ChildText(token, "Password"));
    }

    [TestMethod]
    public void BuildRequest_WithAHostileDigestPassword_DigestsTheRawPasswordNotItsEscapedForm_Test()
    {
        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security =>
            {
                security.UsernameToken.Password = HostilePassword;
                security.UsernameToken.PasswordType = SoapPasswordType.Digest;
            })),
            "Build");

        var token = WsSecurityFoundationTestSupport.UsernameTokenElement(WsSecurityTestSupport.ParseWire(wire));

        var expected = IndependentDigest(
            WsSecurityFoundationTestSupport.ChildText(token, "Nonce"),
            WsSecurityFoundationTestSupport.ChildText(token, "Created"),
            HostilePassword);

        Assert.AreEqual(expected, WsSecurityFoundationTestSupport.ChildText(token, "Password"));
    }

    [TestMethod]
    public void BuildRequest_WithAnIllegalXmlCharacterInThePassword_RefusesByCodeWithoutQuotingIt_Test()
    {
        var illegal = "pass\u0001word";
        var built = WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security =>
            security.UsernameToken.Password = illegal));

        WsSecurityFoundationTestSupport.RefusedWith(
            built,
            "V-SEC-093",
            "",
            ForbiddenSecret.OfText("the illegal password", illegal),
            ForbiddenSecret.OfText("the hexadecimal rendering the serializer would quote", "0x01"),
            ForbiddenSecret.OfText("an ArgumentException name", "ArgumentException"));
    }

    [TestMethod]
    public void BuildRequest_WithAnIllegalXmlCharacterInTheUsername_RefusesByCode_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security =>
                security.UsernameToken.Username = "ali\u0001ce")),
            "V-SEC-093",
            "",
            PasswordSecret);

    [TestMethod]
    public void BuildRequest_WithSecurityDisabledAndAUsernameToken_EmitsNoSecurityHeaderAtAll_Test()
    {
        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security => security.Enabled = false)),
            "Build");

        Assert.IsFalse(wire.Contains("Security", StringComparison.Ordinal), wire);
        Assert.IsFalse(wire.Contains(WsSecurityFoundationTestSupport.Password, StringComparison.Ordinal), wire);
    }

    [TestMethod]
    public void BuildRequest_WithAUsernameTokenOnAGet_RefusesUnderThePostOnlyCode_Test()
        => WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.Build(SoapProtocolType.SOAP_1_2, HttpMethod.Get, WsSecurityFoundationTestSupport.TokenOnlySecurity()),
            "V-SEC-003",
            "",
            PasswordSecret);

    [TestMethod]
    public void BuildRequest_WithATokenOnlyHeader_EmitsTimestampAndTokenAndNoSignature_Test()
    {
        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity()),
            "Build");

        var security = WsSecurityFoundationTestSupport.SecurityHeader(WsSecurityTestSupport.ParseWire(wire));
        var children = security.ChildNodes.Cast<XmlNode>().Select(node => node.LocalName).ToArray();

        CollectionAssert.AreEqual(new[] { "Timestamp", "UsernameToken" }, children, $"{string.Join(", ", children)} | {wire}");

        Assert.IsFalse(wire.Contains("BinarySecurityToken", StringComparison.Ordinal), wire);
    }

    [TestMethod]
    public void BuildRequest_WithSignTokenOnTheAsymmetricBinding_CoversTheTokenWithTheSignature_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire(WsSecurityTestSupport.Security(security =>
            security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken(token => token.SignToken = true)));

        var document = WsSecurityTestSupport.ParseWire(wire);
        var tokenId = WsSecurityTestSupport.WsuId(WsSecurityFoundationTestSupport.UsernameTokenElement(document));

        CollectionAssert.Contains(WsSecurityFoundationTestSupport.SignedReferenceUris(document).ToList(), "#" + tokenId, wire);

        WsSecurityAssert.Accepted(
            new WsSecurityMessageVerifier().Verify(wire, WsSecurityTestSupport.SigningCertificate),
            "");
    }

    [TestMethod]
    public void BuildRequest_WithoutSignTokenOnTheAsymmetricBinding_LeavesTheTokenOutsideTheSignature_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire(WsSecurityTestSupport.Security(security =>
            security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken()));

        var document = WsSecurityTestSupport.ParseWire(wire);
        var tokenId = WsSecurityTestSupport.WsuId(WsSecurityFoundationTestSupport.UsernameTokenElement(document));

        CollectionAssert.DoesNotContain(WsSecurityFoundationTestSupport.SignedReferenceUris(document).ToList(), "#" + tokenId, wire);
    }

    [TestMethod]
    public void BuildRequest_EveryUsernameTokenFailurePath_LeaksNeitherThePasswordNorCertificateMaterial_Test()
    {
        var failures = new[]
        {
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security => security.UsernameToken.Username = null)),
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security => security.UsernameToken.SignToken = true)),
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security => security.UsernameToken.Password = "x\u0001y")),
            WsSecurityTestSupport.Build(SoapProtocolType.SOAP_1_2, HttpMethod.Get, WsSecurityFoundationTestSupport.TokenOnlySecurity()),
            WsSecurityTestSupport.BuildPost(WsSecurityTestSupport.Security(security =>
            {
                security.SigningCertificate = WsSecurityTestSupport.PublicOnlyCertificate;
                security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken();
            }))
        };

        foreach (var failure in failures)
        {
            Assert.IsFalse(failure.IsSuccess);

            SecretLeakAssert.CarriesNoSecret(failure, "username token failure path", PasswordSecret);
        }
    }

    [TestMethod]
    public void BuildRequest_WithATextPasswordForAnHttpEndpoint_RefusesUnderTheTransportRuleOnEveryModeThatCarriesTheTokenInClear_Test()
    {
        var cases = new (string Name, SoapSecurityDto Security)[]
        {
            ("token-only", WsSecurityFoundationTestSupport.TokenOnlySecurity()),
            ("asymmetric", WsSecurityTestSupport.Security(security => security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken())),
            ("asymmetric, signed token", WsSecurityTestSupport.Security(security => security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken(token => token.SignToken = true)))
        };

        foreach (var (name, security) in cases)
        {
            WsSecurityFoundationTestSupport.RefusedWith(
                BuildFor(SamlTestSupport.InsecureEndpoint, security),
                InsecureTransportCode,
                $"{name}",
                PasswordSecret);
        }
    }

    [TestMethod]
    public void BuildRequest_WithATextPasswordForAnHttpEndpointAndTheOptOut_Builds_Test()
    {
        var wire = WsSecurityTestSupport.Wire(
            BuildFor(SamlTestSupport.InsecureEndpoint, WsSecurityFoundationTestSupport.TokenOnlySecurity(security => security.UsernameToken.AllowTextPasswordOverInsecureTransport = true)),
            "Build");

        Assert.IsTrue(wire.Contains(WsSecurityFoundationTestSupport.Password, StringComparison.Ordinal));
    }

    [TestMethod]
    public void BuildRequest_WithADigestPasswordOrAnEncryptedTokenOrAnHttpsEndpoint_IsNotRefusedByTheTransportRule_Test()
    {
        var cases = new (string Name, Uri Endpoint, SoapSecurityDto Security)[]
        {
            ("digest over http", SamlTestSupport.InsecureEndpoint, WsSecurityFoundationTestSupport.TokenOnlySecurity(security => security.UsernameToken.PasswordType = SoapPasswordType.Digest)),
            ("no password over http", SamlTestSupport.InsecureEndpoint, WsSecurityFoundationTestSupport.TokenOnlySecurity(security => security.UsernameToken.Password = null)),
            ("encrypted token over http", SamlTestSupport.InsecureEndpoint, SymmetricTestSupport.UserNameSecurity()),
            ("text over https", WsSecurityTestSupport.Endpoint, WsSecurityFoundationTestSupport.TokenOnlySecurity())
        };

        foreach (var (name, endpoint, security) in cases)
        {
            var built = BuildFor(endpoint, security);

            Assert.IsTrue(built.IsSuccess, $"{name} | {NegativeTestSupport.Describe(built)}");

            built.Response.Dispose();
        }
    }

    [TestMethod]
    public void Sign_WithATextPasswordAndNoKnownEndpoint_IsNotRefusedByTheTransportRule_Test()
    {
        var soap = System.Xml.Linq.XNamespace.Get("http://www.w3.org/2003/05/soap-envelope");
        var envelope = new System.Xml.Linq.XElement(soap + "Envelope", new System.Xml.Linq.XElement(soap + "Header"), new System.Xml.Linq.XElement(soap + "Body", WsSecurityTestSupport.DefaultBody()));

        var signed = new WsSecurityMessageSigner().Sign(envelope, WsSecurityFoundationTestSupport.TokenOnlySecurity());

        Assert.IsTrue(signed.IsSuccess, NegativeTestSupport.Describe(signed));
    }

    private static RzR.ResultMessage.Abstractions.IResult<HttpRequestMessage> BuildFor(Uri endpoint, SoapSecurityDto security)
        => WsSecurityTestSupport.Client(SoapProtocolType.SOAP_1_2).BuildRequest(
            HttpMethod.Post,
            new BuildSoapRequestDto
            {
                Client = new HttpClientDto(endpoint),
                Envelope = new SoapEnvelopeDto(new[] { WsSecurityTestSupport.DefaultBody() }, null, WsSecurityTestSupport.Action),
                Security = security
            });

    private static string IndependentDigest(string nonceBase64, string created, string password)
    {
        var nonce = Convert.FromBase64String(nonceBase64);
        var input = nonce.Concat(Encoding.UTF8.GetBytes(created)).Concat(Encoding.UTF8.GetBytes(password)).ToArray();

        return Convert.ToBase64String(SHA1.HashData(input));
    }
}
