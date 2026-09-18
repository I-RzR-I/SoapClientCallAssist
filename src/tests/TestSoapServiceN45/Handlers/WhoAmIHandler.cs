#region U S I N G

using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Linq;

#endregion

namespace TestSoapServiceN45.Handlers
{
    public sealed class WhoAmIHandler : IHttpHandler
    {
        private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";

        private static readonly XNamespace Service = "http://SoapClientCallAssist.local/";

        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
        {
            var identity = context.User?.Identity;
            var clientCertificate = context.Request.ClientCertificate;
            var certificatePresent = clientCertificate != null && clientCertificate.IsPresent;

            var envelope = new XElement(
                Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", Soap.NamespaceName),
                new XElement(
                    Soap + "Body",
                    new XElement(
                        Service + "WhoAmIResponse",
                        new XElement(
                            Service + "WhoAmIResult",
                            new XElement(Service + "Scheme", context.Request.Url.Scheme),
                            new XElement(Service + "ClientCertificateSha256", certificatePresent ? Sha256Hex(clientCertificate.Certificate) : string.Empty),
                            new XElement(Service + "ClientCertificateSubject", certificatePresent ? clientCertificate.Subject : string.Empty),
                            new XElement(Service + "IdentityName", identity?.Name ?? string.Empty),
                            new XElement(Service + "AuthenticationType", identity?.AuthenticationType ?? string.Empty),
                            new XElement(Service + "IsAuthenticated", identity != null && identity.IsAuthenticated ? "true" : "false")))));

            var body = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + envelope.ToString(SaveOptions.DisableFormatting));

            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/xml; charset=utf-8";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.BinaryWrite(body);
        }

        private static string Sha256Hex(byte[] certificate)
        {
            using (var algorithm = SHA256.Create())
            {
                var digest = algorithm.ComputeHash(certificate);
                var text = new StringBuilder(digest.Length * 2);

                foreach (var octet in digest)
                    text.Append(octet.ToString("X2"));

                return text.ToString();
            }
        }
    }
}
