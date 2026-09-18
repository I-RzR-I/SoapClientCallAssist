#region U S I N G

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Helpers;

#endregion

namespace SoapClientCallAssistTests.Tests.Auth
{
    [TestClass]
    public class SvcUserNameOverTransportTests
    {
        private const string Mode = AuthTestSupport.SvcUserNameMode;

        private static readonly string[] Secrets = { AuthTestSupport.KnownPassword, AuthTestSupport.WrongPassword };

        public TestContext TestContext { get; set; }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_TextPasswordOfKnownUser_IsAuthenticatedAsThatUser_Test(SoapProtocolType protocol)
        {
            var client = AuthTestSupport.CreateClient(protocol);

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.SvcAddress(Mode, protocol),
                AuthTestSupport.WhoAmIBody,
                AuthTestSupport.SvcAction("WhoAmI"),
                AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword));

            var body = AuthTestSupport.SendAccepted(client, request, "text password");
            var identity = AuthTestSupport.ReadResult(client, body, "WhoAmIResult");

            var authenticationType = AuthTestSupport.Field(identity, "AuthenticationType");
            var name = AuthTestSupport.Field(identity, "Name");

            TestContext.WriteLine("WhoAmI(" + protocol + ") => AuthenticationType=" + authenticationType + " Name=" + name);

            Assert.AreEqual(AuthTestSupport.KnownUserName, name);
            Assert.AreNotEqual(string.Empty, authenticationType);
            Assert.AreNotEqual("Anonymous", authenticationType);
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void Echo_TextPasswordOfKnownUser_RoundTrips_Test(SoapProtocolType protocol)
        {
            var client = AuthTestSupport.CreateClient(protocol);
            var value = "user-" + protocol;

            var request = AuthTestSupport.BuildPost(
                client,
                AuthTestSupport.SvcAddress(Mode, protocol),
                AuthTestSupport.EchoBody(value),
                AuthTestSupport.SvcAction("Echo"),
                AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.KnownPassword));

            var body = AuthTestSupport.SendAccepted(client, request, "text password");

            Assert.AreEqual(value, AuthTestSupport.ReadResult(client, body, "EchoResult").Value);
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_WrongPassword_IsRejectedWithFailedAuthentication_Test(SoapProtocolType protocol)
        {
            var rejected = SendRejected(protocol, AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, AuthTestSupport.WrongPassword), "wrong password");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedAuthentication", "wrong password");
            rejected.AssertNoSecret(Secrets, "wrong password");
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_UnknownUser_IsRejectedWithFailedAuthentication_Test(SoapProtocolType protocol)
        {
            var rejected = SendRejected(protocol, AuthTestSupport.UsernameToken(AuthTestSupport.UnknownUserName, AuthTestSupport.KnownPassword), "unknown user");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedAuthentication", "unknown user");
            rejected.AssertNoSecret(Secrets, "unknown user");
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_WithoutAnyToken_IsRejectedWithInvalidSecurity_Test(SoapProtocolType protocol)
        {
            var rejected = SendRejected(protocol, null, "no Security header");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "InvalidSecurity", "no Security header");
            rejected.AssertNoSecret(Secrets, "no Security header");
        }

        [DataTestMethod]
        [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
        [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
        public void WhoAmI_TokenWithEmptyPassword_IsRejectedWithFailedAuthentication_Test(SoapProtocolType protocol)
        {
            var rejected = SendRejected(protocol, AuthTestSupport.UsernameToken(AuthTestSupport.KnownUserName, string.Empty), "empty password");

            rejected.AssertFault(AuthTestSupport.WsseNamespace, "FailedAuthentication", "empty password");
            rejected.AssertNoSecret(Secrets, "empty password");
        }

        private RejectedSoapCall SendRejected(SoapProtocolType protocol, SoapSecurityDto security, string scenario)
            => AuthTestSupport.SendRejectedWhoAmI(TestContext, Mode, protocol, security, scenario);
    }
}
