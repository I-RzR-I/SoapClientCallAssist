#region U S I N G

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Helpers;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssistTests.Tests.Auth
{
    [TestClass]
    public class AsmxWsSecurityTests
    {
        private const string PasswordTextMode = "UsernameToken/PasswordText";

        private const string PasswordDigestMode = "UsernameToken/PasswordDigest";

        private const string X509Mode = "X509";

        private static readonly XNamespace Wsse = AuthTestSupport.WsseNamespace;

        private static readonly XNamespace Wsu = AuthTestSupport.WsuNamespace;

        private static X509Certificate2 _trusted;

        private static X509Certificate2 _untrusted;

        private static readonly string[] Secrets =
        {
            AuthTestSupport.KnownPassword,
            AuthTestSupport.WrongPassword,
            AuthTestSupport.UnicodePassword,
            AuthTestSupport.TrustedThumbprint,
            AuthTestSupport.UntrustedThumbprint
        };

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _trusted = AuthTestSupport.LoadTrustedCertificate();
            _untrusted = AuthTestSupport.LoadUntrustedCertificate();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _trusted?.Dispose();
            _untrusted?.Dispose();
        }

        [TestMethod]
        public void WhoAmI_TextPassword_IsAuthenticatedAsThatUser_Test()
        {
            var identity = WhoAmIAccepted(AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword), "text password");

            Assert.AreEqual(AuthTestSupport.KnownUserName, AuthTestSupport.Field(identity, "UserName"));
            Assert.AreEqual(PasswordTextMode, AuthTestSupport.Field(identity, "Mode"));
        }

        [TestMethod]
        public void WhoAmI_DigestPassword_IsAuthenticatedAsThatUser_Test()
        {
            var identity = WhoAmIAccepted(
                AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword, SoapPasswordType.Digest),
                "digest password");

            Assert.AreEqual(AuthTestSupport.KnownUserName, AuthTestSupport.Field(identity, "UserName"));
            Assert.AreEqual(PasswordDigestMode, AuthTestSupport.Field(identity, "Mode"));
        }

        [TestMethod]
        public void Echo_DigestPassword_RoundTrips_Test()
        {
            var client = AuthTestSupport.CreateClient(SoapProtocolType.SOAP_1_1);
            const string value = "asmx-digest";

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.AsmxAddress,
                AuthTestSupport.EchoBody(value),
                AuthTestSupport.AsmxAction("Echo"),
                AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword, SoapPasswordType.Digest));

            var body = AuthTestSupport.SendAccepted(client, request, "Echo(digest)");

            Assert.AreEqual(value, AuthTestSupport.ReadResult(client, body, "EchoResult").Value);
        }

        [TestMethod]
        public void WhoAmI_NonAsciiPasswordAsText_ArrivesIntact_Test()
        {
            var identity = WhoAmIAccepted(AuthTestSupport.UsernameToken(AuthTestSupport.UnicodeUserName, AuthTestSupport.UnicodePassword), "non-ASCII text password");

            Assert.AreEqual(AuthTestSupport.UnicodeUserName, AuthTestSupport.Field(identity, "UserName"));
            Assert.AreEqual(PasswordTextMode, AuthTestSupport.Field(identity, "Mode"));
        }

        [TestMethod]
        public void WhoAmI_NonAsciiPasswordAsDigest_MatchesTheServerUtf8Digest_Test()
        {
            var identity = WhoAmIAccepted(
                AuthTestSupport.UsernameToken(AuthTestSupport.UnicodeUserName, AuthTestSupport.UnicodePassword, SoapPasswordType.Digest),
                "non-ASCII digest password");

            Assert.AreEqual(AuthTestSupport.UnicodeUserName, AuthTestSupport.Field(identity, "UserName"));
            Assert.AreEqual(PasswordDigestMode, AuthTestSupport.Field(identity, "Mode"));
        }

        [TestMethod]
        public void WhoAmI_SameDigestMessageSentTwice_SecondIsRejectedAsReplay_Test()
        {
            var client = AuthTestSupport.CreateClient(SoapProtocolType.SOAP_1_1);

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.AsmxAddress,
                AuthTestSupport.WhoAmIBody,
                AuthTestSupport.AsmxAction("WhoAmI"),
                AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword, SoapPasswordType.Digest));

            var replay = AuthTestSupport.Clone(request);

            var nonce = AuthTestSupport.UsernameTokenOf(XDocument.Parse(replay.Content.ReadAsStringAsync().GetAwaiter().GetResult())).Element(Wsse + "Nonce")?.Value;
            TestContext.WriteLine("Digest nonce sent twice: " + nonce);

            var first = AuthTestSupport.ReadResult(client, AuthTestSupport.SendAccepted(client, request, "first send"), "WhoAmIResult");
            Assert.AreEqual(PasswordDigestMode, AuthTestSupport.Field(first, "Mode"));

            var rejected = AuthTestSupport.SendRejected(client, replay, "replay");
            TestContext.WriteLine("Replay: " + rejected.Fault);

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedAuthentication", "replay");
            rejected.AssertNoSecret(Secrets, "replay");
        }

        [TestMethod]
        public void WhoAmI_TextTokenWithStaleCreated_IsRejected_Test()
        {
            var stale = AuthTestSupport.Instant(DateTime.UtcNow.AddMinutes(-10));

            var rejected = WhoAmIRejected(
                AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword),
                "stale Created",
                request => AuthTestSupport.RewireXml(request, document => AuthTestSupport.UsernameTokenOf(document).Element(Wsu + "Created").Value = stale));

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedAuthentication", "stale Created");
            rejected.AssertNoSecret(Secrets, "stale Created");
        }

        [TestMethod]
        public void WhoAmI_DigestTokenWithoutNonce_IsRejectedWithInvalidSecurity_Test()
        {
            var rejected = WhoAmIRejected(
                AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword, SoapPasswordType.Digest),
                "digest without nonce",
                request => AuthTestSupport.RewireXml(request, document => AuthTestSupport.UsernameTokenOf(document).Element(Wsse + "Nonce").Remove()));

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "InvalidSecurity", "digest without nonce");
            rejected.AssertNoSecret(Secrets, "digest without nonce");
        }

        [DataTestMethod]
        [DataRow(SoapPasswordType.Text, DisplayName = "PasswordText")]
        [DataRow(SoapPasswordType.Digest, DisplayName = "PasswordDigest")]
        public void WhoAmI_WrongPassword_IsRejectedWithFailedAuthentication_Test(SoapPasswordType passwordType)
        {
            var rejected = WhoAmIRejected(AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.WrongPassword, passwordType), "wrong password (" + passwordType + ")");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedAuthentication", "wrong password (" + passwordType + ")");
            rejected.AssertNoSecret(Secrets, "wrong password (" + passwordType + ")");
        }

        [TestMethod]
        public void WhoAmI_UnknownUser_IsRejectedWithFailedAuthentication_Test()
        {
            var rejected = WhoAmIRejected(AuthTestSupport.UsernameToken(AuthTestSupport.UnknownUserName, AuthTestSupport.KnownPassword, SoapPasswordType.Digest), "unknown user");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedAuthentication", "unknown user");
            rejected.AssertNoSecret(Secrets, "unknown user");
        }

        [TestMethod]
        public void WhoAmI_WithoutSecurityHeader_IsRejectedWithInvalidSecurity_Test()
        {
            var rejected = WhoAmIRejected(null, "no Security header");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "InvalidSecurity", "no Security header");
            rejected.AssertNoSecret(Secrets, "no Security header");
        }

        [TestMethod]
        public void Ping_MethodThatDoesNotClaimTheHeader_FaultsMustUnderstand_Pin_Test()
        {
            var client = AuthTestSupport.CreateClient(SoapProtocolType.SOAP_1_1);

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.AsmxAddress,
                new XElement(AuthTestSupport.ServiceNamespace + "Ping"),
                AuthTestSupport.AsmxAction("Ping"),
                AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword));

            var rejected = AuthTestSupport.SendRejected(client, request, "Ping");
            TestContext.WriteLine("Ping: " + rejected.Fault);

            rejected.AssertFault(AuthTestSupport.Soap11Namespace, "MustUnderstand", "Ping");
            rejected.AssertNoSecret(Secrets, "Ping");
        }

        [TestMethod]
        public void WhoAmISigned_TrustedCertificateOverBodyAndTimestamp_IsAuthenticatedAsThatCertificate_Test()
        {
            var identity = WhoAmIAccepted(AuthTestSupport.Signed(_trusted, signBody: true), "trusted signature", "WhoAmISigned");

            var signedParts = AuthTestSupport.Field(identity, "SignedParts");
            TestContext.WriteLine("SignedParts=" + signedParts);

            Assert.AreEqual(AuthTestSupport.TrustedThumbprint, AuthTestSupport.Field(identity, "CertificateThumbprint"));
            Assert.AreEqual(X509Mode, AuthTestSupport.Field(identity, "Mode"));
            Assert.AreEqual(string.Empty, AuthTestSupport.Field(identity, "UserName"));
            CollectionAssert.AreEquivalent(new[] { "Body", "Timestamp" }, signedParts.Split(','));
        }

        [TestMethod]
        public void WhoAmISigned_DigestTokenPlusTrustedSignature_ReportsBoth_Test()
        {
            var identity = WhoAmIAccepted(
                AuthTestSupport.SignedWithUsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword, _trusted, SoapPasswordType.Digest),
                "digest token + trusted signature",
                "WhoAmISigned");

            Assert.AreEqual(AuthTestSupport.KnownUserName, AuthTestSupport.Field(identity, "UserName"));
            Assert.AreEqual(AuthTestSupport.TrustedThumbprint, AuthTestSupport.Field(identity, "CertificateThumbprint"));
            Assert.AreEqual(PasswordDigestMode + "+" + X509Mode, AuthTestSupport.Field(identity, "Mode"));
        }

        [TestMethod]
        public void WhoAmISigned_UntrustedCertificate_IsRejectedWithFailedAuthentication_Test()
        {
            var rejected = WhoAmIRejected(AuthTestSupport.Signed(_untrusted, signBody: true), "untrusted signature", null, "WhoAmISigned");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedAuthentication", "untrusted signature");
            rejected.AssertNoSecret(Secrets, "untrusted signature");
        }

        [TestMethod]
        public void WhoAmISigned_WithoutSignature_IsRejectedWithInvalidSecurity_Test()
        {
            var rejected = WhoAmIRejected(
                AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword),
                "token but no signature",
                null,
                "WhoAmISigned");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "InvalidSecurity", "token but no signature");
            rejected.AssertNoSecret(Secrets, "token but no signature");
        }

        [TestMethod]
        public void Echo_SignedBodyTamperedAfterSigning_IsRejectedWithFailedCheck_Test()
        {
            var client = AuthTestSupport.CreateClient(SoapProtocolType.SOAP_1_1);

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.AsmxAddress,
                AuthTestSupport.EchoBody("original"),
                AuthTestSupport.AsmxAction("Echo"),
                AuthTestSupport.Signed(_trusted, signBody: true));

            var tampered = AuthTestSupport.RewireXml(request, document =>
                document.Descendants(AuthTestSupport.ServiceNamespace + "value").First().Value = "tampered");

            var rejected = AuthTestSupport.SendRejected(client, tampered, "tampered body");
            TestContext.WriteLine("Tampered: " + rejected.Fault);

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedCheck", "tampered body");
            rejected.AssertNoSecret(Secrets, "tampered body");
        }

        private XElement WhoAmIAccepted(SoapSecurityDto security, string scenario, string operation = "WhoAmI")
        {
            var client = AuthTestSupport.CreateClient(SoapProtocolType.SOAP_1_1);

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.AsmxAddress,
                new XElement(AuthTestSupport.ServiceNamespace + operation),
                AuthTestSupport.AsmxAction(operation),
                security);

            var body = AuthTestSupport.SendAccepted(client, request, scenario);
            var identity = AuthTestSupport.ReadResult(client, body, operation + "Result");

            TestContext.WriteLine(operation + "(" + scenario + ") => UserName=" + AuthTestSupport.Field(identity, "UserName")
                                  + " Mode=" + AuthTestSupport.Field(identity, "Mode")
                                  + " Thumbprint=" + AuthTestSupport.Field(identity, "CertificateThumbprint"));

            return identity;
        }

        private RejectedSoapCall WhoAmIRejected(
            SoapSecurityDto security,
            string scenario,
            Func<HttpRequestMessage, HttpRequestMessage> rewire = null,
            string operation = "WhoAmI")
        {
            var client = AuthTestSupport.CreateClient(SoapProtocolType.SOAP_1_1);

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.AsmxAddress,
                new XElement(AuthTestSupport.ServiceNamespace + operation),
                AuthTestSupport.AsmxAction(operation),
                security);

            if (rewire != null)
                request = rewire(request);

            var rejected = AuthTestSupport.SendRejected(client, request, scenario);

            TestContext.WriteLine(scenario + ": " + rejected.Fault);

            return rejected;
        }
    }
}
