#region U S I N G

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Helpers;
using System.Security.Cryptography.X509Certificates;

#endregion

namespace SoapClientCallAssistTests.Tests.Auth
{
    [TestClass]
    public class SvcUserNameWithEndorsingCertificateTests
    {
        private const string Mode = AuthTestSupport.SvcBothMode;

        private static X509Certificate2 _trusted;

        private static X509Certificate2 _untrusted;

        private static readonly string[] Secrets =
        {
            AuthTestSupport.KnownPassword,
            AuthTestSupport.WrongPassword,
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

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_ValidTokenAndTrustedEndorsement_IsAuthenticatedAsBoth_Test(SoapProtocolType protocol)
        {
            var client = AuthTestSupport.CreateClient(protocol);

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.SvcAddress(Mode, protocol),
                AuthTestSupport.WhoAmIBody,
                AuthTestSupport.SvcAction("WhoAmI"),
                AuthTestSupport.SignedWithUsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword, _trusted));

            var body = AuthTestSupport.SendAccepted(client, request, "token + endorsement");
            var identity = AuthTestSupport.ReadResult(client, body, "WhoAmIResult");

            var authenticationType = AuthTestSupport.Field(identity, "AuthenticationType");
            var name = AuthTestSupport.Field(identity, "Name");
            var thumbprint = AuthTestSupport.Field(identity, "CertificateThumbprint");

            TestContext.WriteLine("WhoAmI(" + protocol + ") => AuthenticationType=" + authenticationType + " Name=" + name + " Thumbprint=" + thumbprint);

            Assert.AreEqual(AuthTestSupport.KnownUserName, name);
            Assert.AreEqual(AuthTestSupport.TrustedThumbprint, thumbprint);
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void Echo_ValidTokenAndTrustedEndorsement_RoundTrips_Test(SoapProtocolType protocol)
        {
            var client = AuthTestSupport.CreateClient(protocol);
            var value = "both-" + protocol;

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.SvcAddress(Mode, protocol),
                AuthTestSupport.EchoBody(value),
                AuthTestSupport.SvcAction("Echo"),
                AuthTestSupport.SignedWithUsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword, _trusted));

            var body = AuthTestSupport.SendAccepted(client, request, "token + endorsement");

            Assert.AreEqual(value, AuthTestSupport.ReadResult(client, body, "EchoResult").Value);
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_TrustedEndorsementButWrongPassword_IsRejected_Test(SoapProtocolType protocol)
        {
            var rejected = SendRejected(
                protocol,
                AuthTestSupport.SignedWithUsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.WrongPassword, _trusted),
                "valid certificate, wrong password");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedAuthentication", "valid certificate, wrong password");
            rejected.AssertNoSecret(Secrets, "valid certificate, wrong password");
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_ValidPasswordButUntrustedEndorsement_IsRejected_Test(SoapProtocolType protocol)
        {
            var rejected = SendRejected(
                protocol,
                AuthTestSupport.SignedWithUsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword, _untrusted),
                "valid password, untrusted certificate");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedAuthentication", "valid password, untrusted certificate");
            rejected.AssertNoSecret(Secrets, "valid password, untrusted certificate");
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_TokenOnlyWithoutEndorsement_IsRejected_Test(SoapProtocolType protocol)
        {
            var rejected = SendRejected(
                protocol,
                AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword),
                "valid token, no endorsement");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "InvalidSecurity", "valid token, no endorsement");
            rejected.AssertNoSecret(Secrets, "valid token, no endorsement");
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_EndorsementOnlyWithoutToken_IsRejected_Test(SoapProtocolType protocol)
        {
            var rejected = SendRejected(protocol, AuthTestSupport.Signed(_trusted), "trusted endorsement, no token");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "InvalidSecurity", "trusted endorsement, no token");
            rejected.AssertNoSecret(Secrets, "trusted endorsement, no token");
        }

        private RejectedSoapCall SendRejected(SoapProtocolType protocol, SoapSecurityDto security, string scenario)
            => AuthTestSupport.SendRejectedWhoAmI(TestContext, Mode, protocol, security, scenario);
    }
}
