#region U S I N G

using System;

#endregion

namespace SoapClientCallAssistTests.Infrastructure
{
    public sealed class IisExpressHostingProfile
    {
        public const string SiteName = "SoapClientCallAssistTestService";

        public const string WindowsAuthApplicationPath = "/WinAuth";

        public const string ClientCertificateApplicationPath = "/ClientCert";

        public IisExpressHostingProfile(int httpPort, int httpsPort, string contentRoot)
        {
            if (contentRoot == null)
                throw new ArgumentNullException(nameof(contentRoot));

            HttpPort = httpPort;
            HttpsPort = httpsPort;
            ContentRoot = contentRoot;
        }

        public int HttpPort { get; }

        public int HttpsPort { get; }

        public string ContentRoot { get; }
    }
}
