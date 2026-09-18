using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Helpers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Tests.Asmx
{
    [TestClass]
    public class SoapCallAsmxWithGetTests
    {
        private readonly Uri _baseUri = new Uri("http://localhost:44338/ServiceAsmx.asmx");
        private readonly Func<SoapProtocolType, ISoapClientEndpoint> _clientFactory = SoapClientFactory.CreateClientFactory();

        [TestMethod]
        public void CallIsValidInHttpGetWithNameInBodies_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Get,
                _baseUri,
                bodies: new List<XElement>()
                {
                    new XElement(
                        "IsValid",
                        new XElement("id", "s1"),
                        new XElement("idV2", "s12")
                        )
                });
            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = client.SendRequest(soapRequest.Response);

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.OK, soapCall.Response.StatusCode);

            var response = soapCall.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(response);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<int xmlns=\"http://SoapClientCallAssist.local/\">1</int>", response);
        }

        [TestMethod]
        public void CallIsValidInHttpGetWithNameInBodies_v2_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Get,
                new BuildSoapRequestDto(new HttpClientDto(_baseUri),
                    new SoapEnvelopeDto(
                        new List<XElement>()
                        {
                            new XElement("IsValid",
                                new XElement("id", "s1"),
                                new XElement("idV2", "s12"))
                        }
                    ))
            );

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = client.SendRequest(soapRequest.Response);

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.OK, soapCall.Response.StatusCode);

            var response = soapCall.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(response);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<int xmlns=\"http://SoapClientCallAssist.local/\">1</int>", response);
        }

        [TestMethod]
        public async Task CallIsValidInHttpGetWithNameInBodiesAsync_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Get,
                _baseUri,
                bodies: new List<XElement>()
                {
                    new XElement(
                        "IsValid",
                        new XElement("id", "s1"),
                        new XElement("idV2", "s12")
                        )
                });
            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = await client.SendRequestAsync(soapRequest.Response);

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.OK, soapCall.Response.StatusCode);

            var response = await soapCall.Response.Content.ReadAsStringAsync();
            Assert.IsNotNull(response);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<int xmlns=\"http://SoapClientCallAssist.local/\">1</int>", response);
        }

        [TestMethod]
        public void CallIsValidInHttpGetWithNameInBodiesAndIdWithNs_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Get,
                _baseUri,
                bodies: new List<XElement>()
                {
                    new XElement(
                        "IsValid",
                        new XElement(ns.GetName("id"), "s1"),
                        new XElement(ns.GetName("idV2"), "s12")
                        )
                });

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = client.SendRequest(soapRequest.Response);

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.OK, soapCall.Response.StatusCode);

            var response = soapCall.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(response);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<int xmlns=\"http://SoapClientCallAssist.local/\">1</int>", response);
        }

        [TestMethod]
        public async Task CallIsValidInHttpGetWithNameInBodiesAndIdWithNsAsync_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Get,
                _baseUri,
                bodies: new List<XElement>()
                {
                    new XElement(
                        "IsValid",
                        new XElement(ns.GetName("id"), "s1"),
                        new XElement(ns.GetName("idV2"), "s12")
                        )
                });

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = await client.SendRequestAsync(soapRequest.Response);

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.OK, soapCall.Response.StatusCode);

            var response = await soapCall.Response.Content.ReadAsStringAsync();
            Assert.IsNotNull(response);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<int xmlns=\"http://SoapClientCallAssist.local/\">1</int>", response);
        }
    }
}