#region U S I N G

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using RzR.ResultMessage.Models;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssistTests.Helpers
{
    public static class TransportTestSupport
    {
        public const string SendFailureCode = "ER-BEC-BSRM-SR";

        public const string UnauthorizedCode = "ER-BEC-HTTP-401";

        public const string ForbiddenCode = "ER-BEC-HTTP-403";

        private static readonly XNamespace Service = "http://SoapClientCallAssist.local/";

        private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(15);

        public static ISoapClientEndpoint CreateSoap11ClientSendingThrough(HttpClientHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var services = new ServiceCollection();

            services
                .AddHttpClient(SoapClientEndpointExtensions.SoapHttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => handler);

            services.RegisterSoapClientsEndpoint();

            var client = services.BuildServiceProvider()
                .GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>()(SoapProtocolType.SOAP_1_1);

            client.SetClientTimeout(NetworkTimeout);

            return client;
        }

        public static IResult<HttpResponseMessage> SendWhoAmI(ISoapClientEndpoint client, Uri endpoint)
        {
            var built = client.BuildRequest(HttpMethod.Post, endpoint, new List<XElement>
            {
                new XElement(Service + "WhoAmI")
            });

            Assert.IsTrue(built.IsSuccess, Describe(built));
            Assert.IsNotNull(built.Response);

            return client.SendRequest(built.Response);
        }

        public static XElement ReadWhoAmI(HttpResponseMessage response)
        {
            var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var result = XDocument.Parse(envelope).Descendants(Service + "WhoAmIResult").FirstOrDefault();

            Assert.IsNotNull(result, envelope);

            return result;
        }

        public static string Field(XElement whoAmI, string name)
        {
            return whoAmI.Element(Service + name)?.Value ?? string.Empty;
        }

        public static void AssertFailedUnder(IResult<HttpResponseMessage> result, string expectedCode, string what)
        {
            Assert.IsFalse(result.IsSuccess, what + " | " + Describe(result));
            Assert.IsNotNull(result.Messages, what);
            Assert.IsTrue(result.Messages.Count > 0, what);
            Assert.AreEqual(expectedCode, result.Messages.First().Key, what + " | " + Describe(result));
        }

        public static string Describe(IResult result)
        {
            return ResultRendering.Describe(result);
        }

        public static string Sweep(IResult result)
        {
            return ResultRendering.Flat(result);
        }
    }
}
