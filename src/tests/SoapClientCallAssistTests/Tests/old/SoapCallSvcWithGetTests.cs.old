// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssistTests
//  Author           : RzR
//  Created On       : 2024-09-16 19:44
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-16 19:44
// ***********************************************************************
//  <copyright file="SoapCallSvcWithGetTests.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;

namespace SoapClientCallAssistTests
{
    [TestClass]
    public class SoapCallSvcWithGetTests
    {
        private readonly Uri _baseUri = new Uri("http://localhost:44338/ServiceSvc.svc");
        private Func<SoapProtocolType, ISoapClientEndpoint> _clientFactory;

        [TestInitialize]
        public void TestInit()
        {
            var services = new ServiceCollection();
            services.RegisterSoapClientsEndpoint();
            var sp = services.BuildServiceProvider();

            _clientFactory = sp.GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>();
        }

        //[TestMethod]
        public void CallIsValidInHttpGetWithNameInBodies()
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
        }
    }
}