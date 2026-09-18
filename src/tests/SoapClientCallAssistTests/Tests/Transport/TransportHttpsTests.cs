#region U S I N G

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Helpers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssistTests.Tests.Transport
{
    [TestClass]
    public class TransportHttpsTests
    {
        private const string HelloWorldMarker = "<HelloWorldResult>Hello World</HelloWorldResult>";

        private const string ForeignThumbprint = "0000000000000000000000000000000000000000";

        private static readonly XNamespace Service = "http://SoapClientCallAssist.local/";

        [TestMethod]
        public void HelloWorldOverHttps_WithTheReservationThumbprintPinned_Succeeds_Test()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = SoapTestServiceFixture.ServerCertificateIsPinned,
                AllowAutoRedirect = false,
                UseProxy = false
            };

            var client = TransportTestSupport.CreateSoap11ClientSendingThrough(handler);

            var built = client.BuildRequest(
                HttpMethod.Post,
                SoapTestServiceFixture.HttpsAsmxAddress,
                new List<XElement> { new XElement(Service + "HelloWorld") });

            Assert.IsTrue(built.IsSuccess, TransportTestSupport.Describe(built));

            var sent = client.SendRequest(built.Response);

            Assert.IsTrue(sent.IsSuccess, TransportTestSupport.Describe(sent));
            Assert.IsNotNull(sent.Response);
            Assert.AreEqual(HttpStatusCode.OK, sent.Response.StatusCode);

            var body = sent.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            StringAssert.Contains(body, HelloWorldMarker, body);

            Assert.AreEqual(Uri.UriSchemeHttps, sent.Response.RequestMessage.RequestUri.Scheme);
        }

        [TestMethod]
        public void HelloWorldOverHttps_WhenTheClientPinsADifferentThumbprint_IsRefusedAsASendFailure_Test()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) =>
                    certificate != null &&
                    string.Equals(certificate.GetCertHashString(), ForeignThumbprint, StringComparison.OrdinalIgnoreCase),
                AllowAutoRedirect = false,
                UseProxy = false
            };

            var client = TransportTestSupport.CreateSoap11ClientSendingThrough(handler);

            var built = client.BuildRequest(
                HttpMethod.Post,
                SoapTestServiceFixture.HttpsAsmxAddress,
                new List<XElement> { new XElement(Service + "HelloWorld") });

            Assert.IsTrue(built.IsSuccess, TransportTestSupport.Describe(built));

            var sent = client.SendRequest(built.Response);

            TransportTestSupport.AssertFailedUnder(sent, TransportTestSupport.SendFailureCode, "wrong pin");

            Assert.IsNull(sent.Response);

            var swept = TransportTestSupport.Sweep(sent);

            Assert.IsFalse(swept.Contains("   at ") || swept.Contains(".cs:line"), swept);

            Assert.IsFalse(swept.IndexOf(SoapTestServiceFixture.ServerCertificateThumbprint, StringComparison.OrdinalIgnoreCase) >= 0, swept);
        }
    }
}
