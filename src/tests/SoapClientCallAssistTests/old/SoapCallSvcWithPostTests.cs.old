// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssistTests
//  Author           : RzR
//  Created On       : 2024-09-16 22:31
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-16 22:31
// ***********************************************************************
//  <copyright file="SoapCallSvcWithPostTests.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Xml.Linq;
using System;
using SoapClientCallAssistTests.Dto.Result;

namespace SoapClientCallAssistTests
{
    [TestClass]
    public class SoapCallSvcWithPostTests
    {
        private Func<SoapProtocolType, ISoapClientEndpoint> _clientFactory;
        private readonly Uri _baseUri = new Uri("http://localhost:44338/ServiceSvc.svc");

        [TestInitialize]
        public void TestInit()
        {
            var services = new ServiceCollection();
            services.RegisterSoapClientsEndpoint();
            var sp = services.BuildServiceProvider();

            _clientFactory = sp.GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>();
        }

        [TestMethod]
        public void CallWithNoBodyPost()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);

            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                new List<XElement>() { new XElement(ns.GetName("HelloWorld")) },
                action: "http://SoapClientCallAssist.local/IServiceSvc/HelloWorld"
                );

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
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><HelloWorldResponse xmlns=\"http://SoapClientCallAssist.local/\"><HelloWorldResult>Hello World</HelloWorldResult></HelloWorldResponse></s:Body></s:Envelope>",
                response);
        }

        [TestMethod]
        public void CallWithNoBodyPostAndHttpHeader()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);

            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                new List<XElement>() { new XElement(ns.GetName("HelloWorld")) },
                action: "http://SoapClientCallAssist.local/IServiceSvc/HelloWorld",
                httpClientHeaders: new Dictionary<string, IEnumerable<string>>()
                {
                    {"UserAgent", new List<string>(){"test"}}
                });

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
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><HelloWorldResponse xmlns=\"http://SoapClientCallAssist.local/\"><HelloWorldResult>Hello World</HelloWorldResult></HelloWorldResponse></s:Body></s:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidInHttpPostWithNameInBodies()
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
                        new XElement("id", "s1"),
                        new XElement(ns.GetName("idV2"), "s12")
                        )
                },
                action: "http://SoapClientCallAssist.local/IServiceSvc/IsValid");

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
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></s:Body></s:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidInHttpPostWithNameInBodies1()
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
                        new XElement("id", "s1"),
                        new XElement("idV2", "s12")
                        )
                },
                action: "http://SoapClientCallAssist.local/IServiceSvc/IsValid");

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
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></s:Body></s:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidInHttpPostWithNameInBodiesAndParamFromNs()
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
                }, action: "http://SoapClientCallAssist.local/IServiceSvc/IsValid");

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
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></s:Body></s:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidInHttpPostWithNameInBodiesAndParamFromNs2()
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
                }, action: "http://SoapClientCallAssist.local/IServiceSvc/IsValid");

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
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></s:Body></s:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidInHttpPostWithNameInBodiesAndParamFromNs3()
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
                }, action: "http://SoapClientCallAssist.local/IServiceSvc/IsValid");

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
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></s:Body></s:Envelope>",
                response);
        }

        [TestMethod]
        public void CallIsValidWithBodyResult()
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
                }, action: "http://SoapClientCallAssist.local/IServiceSvc/IsValid");

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
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><IsValidResponse xmlns=\"http://SoapClientCallAssist.local/\"><IsValidResult>1</IsValidResult></IsValidResponse></s:Body></s:Envelope>",
                response);

            var faultCodes = client.CheckBodyForFaultCode(response);
            Assert.IsNotNull(faultCodes);
            Assert.IsTrue(faultCodes.IsSuccess);

            var xResponse = client.GetXmlNodeResponseBody(response, soapXmlBodyTag: "s:Body");
            Assert.IsNotNull(xResponse);
            Assert.IsTrue(xResponse.IsSuccess);
            Assert.IsNotNull(xResponse.Response);
        }

        [TestMethod]
        public void CallAddRecordWithDetail_With_No_Data()
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
                }, action: "http://SoapClientCallAssist.local/IServiceSvc/AddRecordWithDetail");

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = client.SendRequest(soapRequest.Response);
            var response = soapCall.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            Assert.IsNotNull(soapCall);
            Assert.IsTrue(soapCall.IsSuccess);
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.InternalServerError, soapCall.Response.StatusCode);

            Assert.IsNotNull(response);

            var faultCodes = client.CheckBodyForFaultCode(response);
            Assert.IsNotNull(faultCodes);
            Assert.IsFalse(faultCodes.IsSuccess);
            Assert.IsTrue(faultCodes.GetFirstMessage().Contains("Value cannot be null."));
        }

        [TestMethod]
        public void AddRecord_Success()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");
            var nsObject = XNamespace.Get("http://schemas.datacontract.org/2004/07/TestSoapServiceN45.Dto");
            var action = "http://SoapClientCallAssist.local/IServiceSvc/AddRecordWithDetail";

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                _baseUri,
                bodies: new List<XElement>()
                {
                    new XElement(
                        ns.GetName("AddRecordWithDetail"),
                        new XElement(ns.GetName("product"),
                            new XElement(nsObject.GetName("Code"), "Code-001"),
                            new XElement(nsObject.GetName("Detail"),
                                new XElement(nsObject.GetName("ManufacturerId"), 1),
                                new XElement(nsObject.GetName("PartnerId"), 3),
                                new XElement(nsObject.GetName("SupplierId"), 2)
                            ),
                            new XElement(nsObject.GetName("Id"), 1),
                            new XElement(nsObject.GetName("IsActive"), true),
                            new XElement(nsObject.GetName("Name"), "Name-001")
                        )
                    )
                },
                ownSoapEnvelopeAttributes: new List<XAttribute>()
                {
                    new XAttribute(XNamespace.Xmlns + "tes", nsObject),
                    new XAttribute(XNamespace.Xmlns + "externalNs", ns)
                },
                action: action);

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
            Assert.AreEqual("<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><AddRecordWithDetailResponse xmlns=\"http://SoapClientCallAssist.local/\"><AddRecordWithDetailResult>true</AddRecordWithDetailResult></AddRecordWithDetailResponse></s:Body></s:Envelope>", response);

            var faultCodes = client.CheckBodyForFaultCode(response);
            Assert.IsNotNull(faultCodes);
            Assert.IsTrue(faultCodes.IsSuccess);

            var xmlNodeResponse = client.GetXmlNodeResponseBody(response, soapXmlBodyTag: "s:Body");
            Assert.IsNotNull(xmlNodeResponse);
            Assert.IsTrue(xmlNodeResponse.IsSuccess);
            Assert.IsNotNull(xmlNodeResponse.Response);
            Assert.AreEqual(xmlNodeResponse.Response.OuterXml, "<AddRecordWithDetailResponse xmlns=\"http://SoapClientCallAssist.local/\"><AddRecordWithDetailResult>true</AddRecordWithDetailResult></AddRecordWithDetailResponse>");

            var xNodeResponse = client.GetXNodeResponseBody(response, soapXmlBodyTag: "s:Body");
            Assert.IsNotNull(xNodeResponse);
            Assert.IsTrue(xNodeResponse.IsSuccess);
            Assert.IsNotNull(xNodeResponse.Response);
            Assert.AreEqual(xNodeResponse.Response.ToString(), "<AddRecordWithDetailResponse xmlns=\"http://SoapClientCallAssist.local/\">\r\n  <AddRecordWithDetailResult>true</AddRecordWithDetailResult>\r\n</AddRecordWithDetailResponse>");
        }
    }
}