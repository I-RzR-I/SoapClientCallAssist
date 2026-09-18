#region U S I N G

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Helpers;
using SoapClientCallAssistTests.Infrastructure;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#endregion

namespace SoapClientCallAssistTests.Tests.Transport
{
    [TestClass]
    public class TransportClientCertificateTests
    {
        public const string EnableVariable = "SOAPCLIENTCALLASSIST_TESTS_IIS_CLIENT_CERT";

        public const string PfxPathVariable = "SOAPCLIENTCALLASSIST_TESTS_IIS_CLIENT_CERT_PFX";

        public const string PfxPasswordVariable = "SOAPCLIENTCALLASSIST_TESTS_IIS_CLIENT_CERT_PFX_PASSWORD";

        [TestMethod]
        public void WhoAmI_OverTheClientCertificatePathWithoutACertificate_IsRefusedUnderThe403Code_Test()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = SoapTestServiceFixture.ServerCertificateIsPinned,
                ClientCertificateOptions = ClientCertificateOption.Manual,
                AllowAutoRedirect = false,
                UseProxy = false
            };

            var client = TransportTestSupport.CreateSoap11ClientSendingThrough(handler);

            var sent = TransportTestSupport.SendWhoAmI(client, SoapTestServiceFixture.ClientCertificateWhoAmIAddress);

            TransportTestSupport.AssertFailedUnder(sent, TransportTestSupport.ForbiddenCode, "no client certificate");

            Assert.IsNotNull(sent.Response);
            Assert.AreEqual(HttpStatusCode.Forbidden, sent.Response.StatusCode);

            var swept = TransportTestSupport.Sweep(sent);

            Assert.IsFalse(swept.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0, swept);
        }

        [TestMethod]
        public void WhoAmI_OverTheClientCertificatePathWithATrustedCertificate_EchoesItsFingerprint_Test()
        {
            if (!IisExpressPathResolver.ReadEnvironmentFlag(EnableVariable))
                Assert.Inconclusive(
                    "Accepting a client certificate on IIS Express needs its issuer in the machine's trusted root " +
                    "store, which is an elevated, one-time step this run will not perform. To enable this test:" + Environment.NewLine +
                    "  1. Create a client certificate (or a CA and a client certificate issued by it) and export the " +
                    "client certificate with its private key to a PFX." + Environment.NewLine +
                    "  2. From an elevated prompt, import the issuer (or the self-signed client certificate) into " +
                    "LocalMachine\\Root: certutil -addstore Root <issuer.cer>. Without this IIS answers 403.16." + Environment.NewLine +
                    "  3. Set " + EnableVariable + "=1, " + PfxPathVariable + "=<path to the PFX> and, when it has one, " +
                    PfxPasswordVariable + "=<password>, then run again." + Environment.NewLine +
                    "The generated applicationhost.config already declares sslFlags=\"Ssl,SslNegotiateCert,SslRequireCert\" " +
                    "for " + IisExpressHostingProfile.ClientCertificateApplicationPath + ", so no hosting change is needed.");

            var pfxPath = Environment.GetEnvironmentVariable(PfxPathVariable);

            Assert.IsFalse(string.IsNullOrWhiteSpace(pfxPath) || !File.Exists(pfxPath), pfxPath ?? "<unset>");

            using (var certificate = new X509Certificate2(
                pfxPath,
                Environment.GetEnvironmentVariable(PfxPasswordVariable) ?? string.Empty,
                X509KeyStorageFlags.UserKeySet))
            {
                Assert.IsTrue(certificate.HasPrivateKey);

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = SoapTestServiceFixture.ServerCertificateIsPinned,
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    AllowAutoRedirect = false,
                    UseProxy = false
                };

                handler.ClientCertificates.Add(certificate);

                var client = TransportTestSupport.CreateSoap11ClientSendingThrough(handler);

                var sent = TransportTestSupport.SendWhoAmI(client, SoapTestServiceFixture.ClientCertificateWhoAmIAddress);

                Assert.IsTrue(sent.IsSuccess, TransportTestSupport.Describe(sent));
                Assert.AreEqual(HttpStatusCode.OK, sent.Response.StatusCode);

                var whoAmI = TransportTestSupport.ReadWhoAmI(sent.Response);

                Assert.AreEqual(Uri.UriSchemeHttps, TransportTestSupport.Field(whoAmI, "Scheme"));

                Assert.AreEqual(Sha256Hex(certificate.RawData), TransportTestSupport.Field(whoAmI, "ClientCertificateSha256"));
            }
        }

        private static string Sha256Hex(byte[] rawData)
        {
            using (var algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(rawData)).Replace("-", string.Empty);
        }
    }
}
