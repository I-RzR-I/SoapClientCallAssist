#region U S I N G

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Infrastructure;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

#endregion

namespace SoapClientCallAssistTests
{
    [TestClass]
    public sealed class SoapTestServiceFixture
    {
        public const string AllowEnvironmentSkipVariable = EnvironmentFlag.AllowEnvironmentSkipVariable;

        private const string ServiceBaseAddress = "http://localhost:44338";

        private const int HttpsPort = 44339;

        private const string HttpsBaseAddress = "https://localhost:44339";

        public static readonly Uri WindowsAuthWhoAmIAddress =
            new Uri(ServiceBaseAddress + IisExpressHostingProfile.WindowsAuthApplicationPath + "/WhoAmI.ashx");

        public static readonly Uri ClientCertificateWhoAmIAddress =
            new Uri(HttpsBaseAddress + IisExpressHostingProfile.ClientCertificateApplicationPath + "/WhoAmI.ashx");

        public static readonly Uri HttpsAsmxAddress = new Uri(HttpsBaseAddress + "/ServiceAsmx.asmx");

        private const string HelloWorldIdentityMarker =
            "<string xmlns=\"http://SoapClientCallAssist.local/\">Hello World</string>";

        private static readonly SoapServiceProbe[] ReadinessProbes =
        {
            new SoapServiceProbe(
                new Uri(ServiceBaseAddress + "/ServiceAsmx.asmx/HelloWorld"), HelloWorldIdentityMarker),
            new SoapServiceProbe(
                new Uri(ServiceBaseAddress + "/ServiceSvc.svc"), null),
            new SoapServiceProbe(
                new Uri(HttpsBaseAddress + "/ServiceAsmx.asmx/HelloWorld"), HelloWorldIdentityMarker),
            new SoapServiceProbe(WindowsAuthWhoAmIAddress, null, HttpStatusCode.Unauthorized)
        };

        private const string LogPrefix = "[SoapTestServiceFixture] ";

        private static IisExpressServer _server;

        private static string _serverCertificateThumbprint;

        public static string ServerCertificateThumbprint
        {
            get
            {
                return _serverCertificateThumbprint ?? throw new InvalidOperationException(
                    "The server certificate thumbprint is unavailable because the SOAP test service is not " +
                    "running. Check the failure reported by SoapTestServiceFixture.Initialize.");
            }
        }

        public static bool ServerCertificateIsPinned(
            HttpRequestMessage request, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return IsPinned(certificate);
        }

        private static bool ServerCertificateIsPinned(
            object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return IsPinned(certificate);
        }

        private static bool IsPinned(X509Certificate certificate)
        {
            var pinned = _serverCertificateThumbprint;

            return certificate != null && pinned != null &&
                   string.Equals(certificate.GetCertHashString(), pinned, StringComparison.OrdinalIgnoreCase);
        }

        [AssemblyInitialize]
        public static async Task Initialize(TestContext testContext)
        {
            IisExpressServer server = null;

            try
            {
                _serverCertificateThumbprint = IisExpressSslReservation.ReadServerCertificateThumbprint(HttpsPort);

                Log("Pinning the http.sys certificate reservation for port " + HttpsPort + ": thumbprint " +
                    _serverCertificateThumbprint + ".");

                server = new IisExpressServer(BuildProfile(), ReadinessProbes, ServerCertificateIsPinned);
                _server = server;

                await server.EnsureRunningAsync(Log);
            }
            catch (TestServiceUnavailableException exception)
            {
                _server = null;
                _serverCertificateThumbprint = null;
                server?.Dispose();

                if (IisExpressPathResolver.ReadEnvironmentFlag(AllowEnvironmentSkipVariable))
                    Assert.Inconclusive(exception.Message);

                throw new InvalidOperationException(
                    exception.Message + Environment.NewLine +
                    "This run failed rather than being skipped, so that a machine which cannot host the " +
                    "test service cannot report a green build in which no test ran. Set " +
                    AllowEnvironmentSkipVariable + "=1 to downgrade this to a skip.");
            }
        }

        [AssemblyCleanup]
        public static async Task Cleanup()
        {
            var server = _server;
            if (server == null)
                return;

            _server = null;

            try
            {
                await server.ShutdownAsync(Log);
            }
            finally
            {
                server.Dispose();
            }
        }

        private static IisExpressHostingProfile BuildProfile()
        {
            var contentRoot = IisExpressPathResolver.ResolveSiteContentRoot();
            if (contentRoot == null)
                throw new TestServiceUnavailableException(
                    "The content root of the in-repository test service could not be resolved from " +
                    IisExpressPathResolver.Quote(AppContext.BaseDirectory) + ", so no site can be declared.");

            return new IisExpressHostingProfile(new Uri(ServiceBaseAddress).Port, HttpsPort, contentRoot);
        }

        private static void Log(string message)
        {
            Console.WriteLine(LogPrefix + message);
        }
    }
}
