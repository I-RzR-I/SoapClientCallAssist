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
    public class SvcCertificateOverTransportTests
    {
        private const string Mode = AuthTestSupport.SvcCertificateMode;

        private static X509Certificate2 _trusted;

        private static X509Certificate2 _untrusted;

        private static readonly string[] Secrets = { AuthTestSupport.TrustedThumbprint, AuthTestSupport.UntrustedThumbprint };

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
        public void WhoAmI_SignedTimestampWithTrustedCertificate_IsAuthenticatedAsThatCertificate_Test(SoapProtocolType protocol)
        {
            var client = AuthTestSupport.CreateClient(protocol);

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.SvcAddress(Mode, protocol),
                AuthTestSupport.WhoAmIBody,
                AuthTestSupport.SvcAction("WhoAmI"),
                AuthTestSupport.Signed(_trusted));

            var body = AuthTestSupport.SendAccepted(client, request, "signed timestamp");
            var identity = AuthTestSupport.ReadResult(client, body, "WhoAmIResult");

            var authenticationType = AuthTestSupport.Field(identity, "AuthenticationType");
            var name = AuthTestSupport.Field(identity, "Name");
            var thumbprint = AuthTestSupport.Field(identity, "CertificateThumbprint");

            TestContext.WriteLine("WhoAmI(" + protocol + ") => AuthenticationType=" + authenticationType + " Name=" + name + " Thumbprint=" + thumbprint);

            Assert.AreEqual(AuthTestSupport.TrustedThumbprint, thumbprint);
            Assert.AreEqual("X509", authenticationType);
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void Echo_SignedTimestampWithTrustedCertificate_RoundTrips_Test(SoapProtocolType protocol)
        {
            var client = AuthTestSupport.CreateClient(protocol);
            var value = "cert-" + protocol;

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.SvcAddress(Mode, protocol),
                AuthTestSupport.EchoBody(value),
                AuthTestSupport.SvcAction("Echo"),
                AuthTestSupport.Signed(_trusted));

            var body = AuthTestSupport.SendAccepted(client, request, "signed timestamp");

            Assert.AreEqual(value, AuthTestSupport.ReadResult(client, body, "EchoResult").Value);
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_SignedWithUntrustedCertificate_IsRejectedWithFailedAuthentication_Test(SoapProtocolType protocol)
        {
            var rejected = SendRejected(protocol, AuthTestSupport.Signed(_untrusted), "untrusted certificate");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedAuthentication", "untrusted certificate");
            rejected.AssertNoSecret(Secrets, "untrusted certificate");
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_WithTheBodySigned_IsRejectedWithInvalidSecurity_Pin_Test(SoapProtocolType protocol)
        {
            var rejected = SendRejected(protocol, AuthTestSupport.Signed(_trusted, signBody: true), "signed body");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "InvalidSecurity", "signed body");
            rejected.AssertNoSecret(Secrets, "signed body");
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_Unsigned_IsRejectedWithInvalidSecurity_Test(SoapProtocolType protocol)
        {
            var rejected = SendRejected(protocol, null, "unsigned");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "InvalidSecurity", "unsigned");
            rejected.AssertNoSecret(Secrets, "unsigned");
        }

        private RejectedSoapCall SendRejected(SoapProtocolType protocol, SoapSecurityDto security, string scenario)
            => AuthTestSupport.SendRejectedWhoAmI(TestContext, Mode, protocol, security, scenario);
    }
}
