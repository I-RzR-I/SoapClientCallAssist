using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Tests.Asmx
{
    [TestClass]
    public class SoapCallAsmxWithPostTests
    {
        private readonly Uri _baseUri = new Uri("http://localhost:44338/ServiceAsmx.asmx");
        private readonly Func<SoapProtocolType, ISoapClientEndpoint> _clientFactory = SoapClientFactory.CreateClientFactory();

        [TestMethod]
        public void CallWithNoBodyPost_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);

            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                new List<XElement>() { new XElement(ns.GetName("HelloWorld")) });

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            client.SetClientTimeout(TimeSpan.FromSeconds(15));

            var soapCall = client.SendRequest(soapRequest.Response);

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.OK, soapCall.Response.StatusCode);

            var response = soapCall.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(response);
            Assert.AreEqual(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><soap:Body><HelloWorldResponse xmlns=\"http://SoapClientCallAssist.local/\"><HelloWorldResult>Hello World</HelloWorldResult></HelloWorldResponse></soap:Body></soap:Envelope>",
                response);
        }

        [TestMethod]
        public async Task CallWithNoBodyPostAsync_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);

            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                new List<XElement>() { new XElement(ns.GetName("HelloWorld")) });

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            client.SetClientTimeout(TimeSpan.FromSeconds(15));

            var soapCall = await client.SendRequestAsync(soapRequest.Response);

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.OK, soapCall.Response.StatusCode);

            var response = await soapCall.Response.Content.ReadAsStringAsync();
            Assert.IsNotNull(response);
            Assert.AreEqual(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><soap:Body><HelloWorldResponse xmlns=\"http://SoapClientCallAssist.local/\"><HelloWorldResult>Hello World</HelloWorldResult></HelloWorldResponse></soap:Body></soap:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidInHttpPostWithNameInBodies_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");
            var uri = new Uri("http://localhost:44338/ServiceAsmx.asmx");

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                uri,
                bodies: new List<XElement>()
                {
                    new XElement(
                        ns.GetName("IsValid"),
                        new XElement(ns.GetName("id"), "s1"),
                        new XElement(ns.GetName("idV2"), "s12")
                        )
                });

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = client.SendRequest(soapRequest.Response);
            var response = soapCall.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.OK, soapCall.Response.StatusCode);

            Assert.IsNotNull(response);
            Assert.AreEqual(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><soap:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></soap:Body></soap:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidInHttpPostWithNameInBodies1_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                bodies: new List<XElement>()
                {
                    new XElement(
                        ns.GetName("IsValid"),
                        new XElement(ns.GetName("id"), "s1"),
                        new XElement(ns.GetName("idV2"), "s12")
                        )
                });

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = client.SendRequest(soapRequest.Response);
            var response = soapCall.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.OK, soapCall.Response.StatusCode);

            Assert.IsNotNull(response);
            Assert.AreEqual(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><soap:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></soap:Body></soap:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidInHttpPostWithNameInBodiesAndParamFromNs_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                bodies: new List<XElement>()
                {
                    new XElement(
                        ns.GetName("IsValid"),
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
            Assert.AreEqual(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><soap:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></soap:Body></soap:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidInHttpPostWithNameInBodiesAndParamFromNs2_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var rootBody = new XElement(ns.GetName("IsValid"));
            rootBody.Add(new XElement(ns.GetName("id"), "s1"));
            rootBody.Add(new XElement(ns.GetName("idV2"), "s12"));

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                bodies: new List<XElement>()
                {
                    rootBody
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
            Assert.AreEqual(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><soap:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></soap:Body></soap:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidInHttpPostWithNameInBodiesAndParamFromNs3_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var bodyParams = new List<XElement>()
            {
                new XElement(ns.GetName("id"), "s1"),
                new XElement(ns.GetName("idV2"), "s12")
            };

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                bodies: new List<XElement>()
                {
                    new XElement(ns.GetName("IsValid"), bodyParams)
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
            Assert.AreEqual(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><soap:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></soap:Body></soap:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidWithBodyResult_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                bodies: new List<XElement>()
                {
                    new XElement(
                        ns.GetName("IsValid"),
                        new XElement(ns.GetName("id"), "s1"),
                        new XElement(ns.GetName("idV2"), "s12")
                    )
                });

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = client.SendRequest(soapRequest.Response);
            var response = soapCall.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.OK, soapCall.Response.StatusCode);

            Assert.IsNotNull(response);
            Assert.AreEqual(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><soap:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></soap:Body></soap:Envelope>",
                response);

            var faultCodes = client.CheckBodyForFaultCode(response);
            Assert.IsNotNull(faultCodes);
            Assert.IsTrue(faultCodes.IsSuccess);

            var xResponse = client.GetXmlNodeResponseBody(response);
            Assert.IsNotNull(xResponse);
            Assert.IsTrue(xResponse.IsSuccess);
            Assert.IsNotNull(xResponse.Response);
        }

        [TestMethod]
        public void CallAddRecordWithDetail_With_No_Data_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                bodies: new List<XElement>()
                {
                    new XElement(
                        ns.GetName("AddRecordWithDetail")
                    )
                });

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = client.SendRequest(soapRequest.Response);
            var response = soapCall.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            Assert.IsNotNull(soapCall);
            Assert.IsFalse(soapCall.IsSuccess);
            Assert.AreEqual("ER-BEC-HTTP-FLT", soapCall.Messages.First().Key);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.InternalServerError, soapCall.Response.StatusCode);

            Assert.IsNotNull(response);

            var faultCodes = client.CheckBodyForFaultCode(response);
            Assert.IsNotNull(faultCodes);
            Assert.IsFalse(faultCodes.IsSuccess);
            Assert.IsTrue(faultCodes.GetFirstMessage().Contains("System.ArgumentNullException: Value cannot be null."));
        }

        [TestMethod]
        public void AddRecord_Success_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                bodies: new List<XElement>()
                {
                    new XElement(
                        ns.GetName("AddRecordWithDetail"),
                        new XElement(ns.GetName("product"),
                            new XElement(ns.GetName("Id"), "1"),
                            new XElement(ns.GetName("Code"), "Code-001"),
                            new XElement(ns.GetName("Name"), "Name-001"),
                            new XElement(ns.GetName("IsActive"), "true"),
                            new XElement(ns.GetName("Detail"),
                                new XElement(ns.GetName("ManufacturerId"), "1"),
                                new XElement(ns.GetName("SupplierId"), "2"),
                                new XElement(ns.GetName("PartnerId"), "3")
                                )
                            )
                    )
                });

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = client.SendRequest(soapRequest.Response);
            var response = soapCall.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.OK, soapCall.Response.StatusCode);

            Assert.IsNotNull(response);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><soap:Body><AddRecordWithDetailResponse xmlns=\"http://SoapClientCallAssist.local/\"><AddRecordWithDetailResult>true</AddRecordWithDetailResult></AddRecordWithDetailResponse></soap:Body></soap:Envelope>", response);

            var faultCodes = client.CheckBodyForFaultCode(response);
            Assert.IsNotNull(faultCodes);
            Assert.IsTrue(faultCodes.IsSuccess);

            var xResponse = client.GetXmlNodeResponseBody(response);
            Assert.IsNotNull(xResponse);
            Assert.IsTrue(xResponse.IsSuccess);
            Assert.IsNotNull(xResponse.Response);
            Assert.AreEqual(xResponse.Response.OuterXml, "<AddRecordWithDetailResponse xmlns=\"http://SoapClientCallAssist.local/\"><AddRecordWithDetailResult>true</AddRecordWithDetailResult></AddRecordWithDetailResponse>");
        }
    }
}