#region U S I N G

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssistTests.Helpers
{
    public static class AuthTestSupport
    {
        public const string ServiceBaseAddress = "http://localhost:44338";

        public const string SvcUserNameMode = "user";

        public const string SvcCertificateMode = "cert";

        public const string SvcBothMode = "both";

        public const string HttpSoapFaultCode = "ER-BEC-HTTP-FLT";

        public const string BodyFaultCode = "ER-BEC-FLT";

        public const string KnownUserName = "alice";

        public const string KnownPassword = "secret";

        public const string UnicodeUserName = "unicode-user";

        public const string UnicodePassword = "pässwörd€-日本";

        public const string WrongPassword = "not-the-password";

        public const string UnknownUserName = "mallory";

        public const string TrustedThumbprint = "0690EB0DA269DBE6B24A98E1047B1E3A427D4DEE";

        public const string UntrustedThumbprint = "25478EDD6A1F05B63AA8A0A61E347638C4F9516C";

        public const string PfxPassword = "soapclientcallassist";

        public static readonly XNamespace ServiceNamespace = "http://SoapClientCallAssist.local/";

        public const string WsseNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        public const string WsuNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

        public const string Soap11Namespace = "http://schemas.xmlsoap.org/soap/envelope/";

        private const string TrustedPfxFileName = "SoapClientCallAssist.N45.TrustedClient.pfx";

        private const string UntrustedPfxFileName = "SoapClientCallAssist.N45.UntrustedClient.pfx";

        private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(20);

        public static Uri SvcAddress(string mode, SoapProtocolType protocol)
        {
            return new Uri(ServiceBaseAddress + "/ServiceSecured.svc/" + mode + "/" + (protocol == SoapProtocolType.SOAP_1_1 ? "soap11" : "soap12"));
        }

        public static Uri AsmxAddress => new Uri(ServiceBaseAddress + "/ServiceSecuredAsmx.asmx");

        public static string SvcAction(string operation)
        {
            return ServiceNamespace.NamespaceName + "IServiceSecured/" + operation;
        }

        public static string AsmxAction(string operation)
        {
            return ServiceNamespace.NamespaceName + operation;
        }

        public static ISoapClientEndpoint CreateClient(SoapProtocolType protocol)
        {
            var services = new ServiceCollection();
            services.RegisterSoapClientsEndpoint();

            var factory = services.BuildServiceProvider().GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>();
            var client = factory(protocol);

            Unwrap(client.SetClientTimeout(NetworkTimeout), "SetClientTimeout");

            return client;
        }

        public static X509Certificate2 LoadTrustedCertificate()
        {
            return LoadPfx(TrustedPfxFileName, TrustedThumbprint);
        }

        public static X509Certificate2 LoadUntrustedCertificate()
        {
            return LoadPfx(UntrustedPfxFileName, UntrustedThumbprint);
        }

        public static SoapSecurityDto UsernameToken(string userName, string password, SoapPasswordType passwordType = SoapPasswordType.Text)
        {
            return new SoapSecurityDto
            {
                IncludeTimestamp = true,
                UsernameToken = new SoapUsernameTokenDto
                {
                    Username = userName,
                    Password = password,
                    PasswordType = passwordType,
                    AllowTextPasswordOverInsecureTransport = true
                }
            };
        }

        public static SoapSecurityDto Signed(X509Certificate2 certificate, bool signBody = false)
        {
            return new SoapSecurityDto
            {
                SigningCertificate = certificate,
                SignBody = signBody,
                IncludeTimestamp = true,
                SignTimestamp = true
            };
        }

        public static SoapSecurityDto SignedWithUsernameToken(string userName, string password, X509Certificate2 certificate, SoapPasswordType passwordType = SoapPasswordType.Text)
        {
            var security = Signed(certificate);
            security.UsernameToken = new SoapUsernameTokenDto
            {
                Username = userName,
                Password = password,
                PasswordType = passwordType,
                AllowTextPasswordOverInsecureTransport = true
            };

            return security;
        }

        public static XElement WhoAmIBody => new XElement(ServiceNamespace + "WhoAmI");

        public static XElement EchoBody(string value)
        {
            return new XElement(ServiceNamespace + "Echo", new XElement(ServiceNamespace + "value", value));
        }

        public static HttpRequestMessage BuildPost(ISoapClientEndpoint client, Uri endpoint, XElement body, string action, SoapSecurityDto security = null)
        {
            return Unwrap(
                client.BuildRequest(
                    HttpMethod.Post,
                    new BuildSoapRequestDto
                    {
                        Client = new HttpClientDto(endpoint),
                        Envelope = new SoapEnvelopeDto(new[] { body }, null, action),
                        Security = security
                    }),
                endpoint.AbsolutePath);
        }

        public static string SendAccepted(ISoapClientEndpoint client, HttpRequestMessage request, string what)
        {
            using (request)
            {
                var sent = client.SendRequest(request);

                using (var response = Unwrap(sent, what))
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, what);

                    return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
            }
        }

        public static RejectedSoapCall SendRejected(ISoapClientEndpoint client, HttpRequestMessage request, string what)
        {
            using (request)
            {
                var sent = client.SendRequest(request);

                Assert.IsFalse(sent.IsSuccess, what);
                Assert.AreEqual(HttpSoapFaultCode, FirstKey(sent), what + " | " + Render(sent));
                Assert.IsNotNull(sent.Response, what);

                using (var response = sent.Response)
                {
                    var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var checkedBody = client.CheckBodyForFaultCode(body);

                    Assert.IsFalse(checkedBody.IsSuccess, what);
                    Assert.AreEqual(BodyFaultCode, FirstKey(checkedBody), what);

                    return new RejectedSoapCall(sent, checkedBody, SoapFaultReader.Read(body));
                }
            }
        }

        public static RejectedSoapCall SendRejectedWhoAmI(TestContext testContext, string mode, SoapProtocolType protocol, SoapSecurityDto security, string scenario)
        {
            var client = CreateClient(protocol);

            var request = BuildPost(
                client,
                SvcAddress(mode, protocol),
                WhoAmIBody,
                SvcAction("WhoAmI"),
                security);

            var rejected = SendRejected(client, request, scenario);

            testContext.WriteLine(protocol + " " + scenario + ": " + rejected.Fault);

            return rejected;
        }

        public static XElement ReadResult(ISoapClientEndpoint client, string responseBody, string resultName)
        {
            var node = Unwrap(client.GetXNodeResponseBody(responseBody), "GetXNodeResponseBody");
            var payload = (node as XDocument)?.Root ?? node as XElement
                          ?? throw new AssertFailedException("The response payload is a " + node.GetType().Name + ", not an element.");

            return payload.Element(ServiceNamespace + resultName)
                   ?? throw new AssertFailedException("The response carries no " + resultName + ". Payload: " + payload);
        }

        public static string Field(XElement result, string childName)
        {
            return result.Element(ServiceNamespace + childName)?.Value ?? string.Empty;
        }

        public static HttpRequestMessage Clone(HttpRequestMessage request)
        {
            return Rewire(request, text => text);
        }

        public static HttpRequestMessage Rewire(HttpRequestMessage request, Func<string, string> mutate)
        {
            var original = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var mutated = mutate(original);

            var copy = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version,
                Content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(mutated))
            };

            foreach (var header in request.Headers)
                copy.Headers.TryAddWithoutValidation(header.Key, header.Value);

            copy.Content.Headers.ContentLength = null;

            foreach (var header in request.Content.Headers)
                if (!string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                    copy.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return copy;
        }

        public static HttpRequestMessage RewireXml(HttpRequestMessage request, Action<XDocument> edit)
        {
            return Rewire(request, text =>
            {
                var document = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
                edit(document);

                return document.Declaration == null
                    ? document.ToString(SaveOptions.DisableFormatting)
                    : document.Declaration + document.ToString(SaveOptions.DisableFormatting);
            });
        }

        public static XElement UsernameTokenOf(XDocument document)
        {
            return document.Descendants(XNamespace.Get(WsseNamespace) + "UsernameToken").FirstOrDefault()
                   ?? throw new AssertFailedException("The envelope carries no wsse:UsernameToken.");
        }

        public static string Instant(DateTime instant)
        {
            return instant.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        }

        public static T Unwrap<T>(IResult<T> result, string what)
        {
            Assert.IsTrue(result.IsSuccess, what + " | " + Render(result));

            return result.Response;
        }

        public static void Unwrap(IResult result, string what)
        {
            Assert.IsTrue(result.IsSuccess, what + " | " + Render(result));
        }

        public static void AssertNoSecret(IResult result, IEnumerable<string> secrets, string what)
        {
            var rendered = Render(result);

            foreach (var secret in secrets)
                Assert.IsFalse(rendered.IndexOf(secret, StringComparison.OrdinalIgnoreCase) >= 0, what + " | " + rendered);
        }

        public static string Render(IResult result)
        {
            return ResultRendering.Flat(result);
        }

        public static string FirstKey(IResult result)
        {
            return ResultRendering.FirstKey(result);
        }

        private static X509Certificate2 LoadPfx(string fileName, string expectedThumbprint)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

            Assert.IsTrue(File.Exists(path), path);

            var certificate = new X509Certificate2(path, PfxPassword, X509KeyStorageFlags.EphemeralKeySet);

            Assert.AreEqual(expectedThumbprint, certificate.Thumbprint, fileName);
            Assert.IsTrue(certificate.HasPrivateKey, fileName);

            return certificate;
        }
    }
}
