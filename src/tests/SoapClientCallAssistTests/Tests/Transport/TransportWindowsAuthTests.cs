#region U S I N G

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Helpers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;

#endregion

namespace SoapClientCallAssistTests.Tests.Transport
{
    [TestClass]
    public class TransportWindowsAuthTests
    {
        private const string NegotiateScheme = "Negotiate";

        private const string NtlmAuthenticationType = "NTLM";

        [TestMethod]
        public void WhoAmI_Anonymously_FailsUnderThe401CodeNamingNegotiate_Test()
        {
            var client = TransportTestSupport.CreateSoap11ClientSendingThrough(AnonymousHandler());

            var sent = TransportTestSupport.SendWhoAmI(client, SoapTestServiceFixture.WindowsAuthWhoAmIAddress);

            TransportTestSupport.AssertFailedUnder(sent, TransportTestSupport.UnauthorizedCode, "anonymous");

            Assert.IsNotNull(sent.Response);
            Assert.AreEqual(HttpStatusCode.Unauthorized, sent.Response.StatusCode);

            var info = sent.Messages.First().Message.Info;

            StringAssert.Contains(info, NegotiateScheme, info);

            Assert.IsTrue(sent.Response.Headers.WwwAuthenticate.Count > 0);
        }

        [TestMethod]
        public void WhoAmI_WithDefaultCredentialsCachedForTheExactOrigin_AuthenticatesAsTheCurrentUserOverNtlm_Test()
        {
            var origin = new Uri(SoapTestServiceFixture.WindowsAuthWhoAmIAddress.GetLeftPart(UriPartial.Authority));
            var client = TransportTestSupport.CreateSoap11ClientSendingThrough(HandlerWithCredentialsFor(origin));

            var sent = TransportTestSupport.SendWhoAmI(client, SoapTestServiceFixture.WindowsAuthWhoAmIAddress);

            Assert.IsTrue(sent.IsSuccess, TransportTestSupport.Describe(sent));
            Assert.AreEqual(HttpStatusCode.OK, sent.Response.StatusCode);

            var whoAmI = TransportTestSupport.ReadWhoAmI(sent.Response);
            var identity = TransportTestSupport.Field(whoAmI, "IdentityName");
            var authenticationType = TransportTestSupport.Field(whoAmI, "AuthenticationType");

            Console.WriteLine("[TransportWindowsAuthTests] identity=" + identity + " authenticationType=" + authenticationType);

            Assert.AreEqual("true", TransportTestSupport.Field(whoAmI, "IsAuthenticated"));

            Assert.AreEqual(WindowsIdentity.GetCurrent().Name, identity, true);

            Assert.IsTrue(
                string.Equals(authenticationType, NtlmAuthenticationType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(authenticationType, NegotiateScheme, StringComparison.OrdinalIgnoreCase),
                authenticationType);

            Assert.AreEqual(Uri.UriSchemeHttp, TransportTestSupport.Field(whoAmI, "Scheme"));
        }

        [TestMethod]
        public void WhoAmI_WithCredentialsCachedForADifferentOrigin_IsChallengedAndSendsNothing_Test()
        {
            var otherOrigin = new Uri(SoapTestServiceFixture.HttpsAsmxAddress.GetLeftPart(UriPartial.Authority));
            var client = TransportTestSupport.CreateSoap11ClientSendingThrough(HandlerWithCredentialsFor(otherOrigin));

            var sent = TransportTestSupport.SendWhoAmI(client, SoapTestServiceFixture.WindowsAuthWhoAmIAddress);

            TransportTestSupport.AssertFailedUnder(sent, TransportTestSupport.UnauthorizedCode, otherOrigin.ToString());

            Assert.AreEqual(HttpStatusCode.Unauthorized, sent.Response.StatusCode);
        }

        private static HttpClientHandler HandlerWithCredentialsFor(Uri exactOrigin)
        {
            var credentials = new CredentialCache { { exactOrigin, NegotiateScheme, CredentialCache.DefaultNetworkCredentials } };

            return new HttpClientHandler
            {
                Credentials = credentials,
                UseDefaultCredentials = false,
                AllowAutoRedirect = false,
                PreAuthenticate = false,
                UseProxy = false
            };
        }

        private static HttpClientHandler AnonymousHandler()
        {
            return new HttpClientHandler
            {
                UseDefaultCredentials = false,
                AllowAutoRedirect = false,
                UseProxy = false
            };
        }
    }
}
