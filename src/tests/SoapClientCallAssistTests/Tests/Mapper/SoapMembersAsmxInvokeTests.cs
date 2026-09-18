using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Dto.SoapMembers;
using SoapClientCallAssistTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Tests.Mapper
{
    [TestClass]
    public class SoapMembersAsmxInvokeTests
    {
        private readonly Uri _baseUri = new Uri("http://localhost:44338/ServiceAsmx.asmx");
        private Func<SoapProtocolType, ISoapClientEndpoint> _clientFactory;
        private ISoapModelMapper _mapper;

        [TestInitialize]
        public void Initialize()
        {
            var sp = SoapClientFactory.BuildProvider();

            _clientFactory = sp.GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>();
            _mapper = sp.GetRequiredService<ISoapModelMapper>();
        }

        [TestMethod]
        public async Task CallHelloWorldInHttpPost_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var request = new SoapOperationRequest("HelloWorld", ns);
            var map = _mapper.ToBodies(request);

            Assert.IsTrue(map.IsSuccess);

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                new BuildSoapRequestDto(new HttpClientDto(_baseUri),
                    new SoapEnvelopeDto(map.Response))
            );

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

            var fromResponse = _mapper.FromResponse<SoapMemberHelloWorldResponse>(response, ns);

            Assert.IsNotNull(fromResponse);
            Assert.IsTrue(fromResponse.IsSuccess);
            Assert.IsNotNull(fromResponse.Response);
            Assert.AreEqual("Hello World", fromResponse.Response.Result);
        }

        [TestMethod]
        public async Task CallIsValidInHttpPostWithNameInBodies_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var request = new SoapOperationRequest("IsValid", ns)
                .AddParameter("id", "s1");
            var map = _mapper.ToBodies(request);

            Assert.IsTrue(map.IsSuccess);

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                new BuildSoapRequestDto(new HttpClientDto(_baseUri),
                    new SoapEnvelopeDto(map.Response))
            );

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

            var fromResponse = _mapper.FromResponse<SoapMemberIsValidResponse>(response, ns);

            Assert.IsNotNull(fromResponse);
            Assert.IsTrue(fromResponse.IsSuccess);
            Assert.IsNotNull(fromResponse.Response);
            Assert.AreEqual(1, fromResponse.Response.Result);
        }

        [TestMethod]
        public async Task CallIsValidInHttpPostWithNameInBodiesAndEmptyId_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var request = new SoapOperationRequest("IsValid", ns)
                .AddParameter("id", string.Empty);
            var map = _mapper.ToBodies(request);

            Assert.IsTrue(map.IsSuccess);

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                new BuildSoapRequestDto(new HttpClientDto(_baseUri),
                    new SoapEnvelopeDto(map.Response))
            );

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

            var fromResponse = _mapper.FromResponse<SoapMemberIsValidResponse>(response, ns);

            Assert.IsNotNull(fromResponse);
            Assert.IsTrue(fromResponse.IsSuccess);
            Assert.IsNotNull(fromResponse.Response);
            Assert.AreEqual(-1, fromResponse.Response.Result);
        }

        [TestMethod]
        public async Task CallIsValidInHttpGetWithNameInBodiesAndQNameDefect_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var request = new SoapOperationRequest("IsValid", ns)
                .AddParameter("id", "s1");
            var map = _mapper.ToBodies(request);

            Assert.IsTrue(map.IsSuccess);
            var soapRequest = client.BuildRequest(
                HttpMethod.Get,
                new BuildSoapRequestDto(new HttpClientDto(_baseUri),
                    new SoapEnvelopeDto(map.Response))
            );

            Assert.IsNotNull(soapRequest);
            Assert.IsTrue(soapRequest.IsSuccess);
            Assert.IsNotNull(soapRequest.Response);

            var soapCall = await client.SendRequestAsync(soapRequest.Response);

            Assert.IsNotNull(soapCall);
            Assert.IsFalse(soapCall.IsSuccess, "DEFECT-SOAP12-GET-QNAME");
            Assert.AreEqual("ER-BEC-HTTP-4XX", soapCall.Messages.First().Key, "DEFECT-SOAP12-GET-QNAME");
            Assert.IsNotNull(soapCall.Response);
            Assert.AreEqual(HttpStatusCode.BadRequest, soapCall.Response.StatusCode, "DEFECT-SOAP12-GET-QNAME");
        }

        [TestMethod]
        public async Task CallAddRecordWithDetailInHttpPost_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var product = new SoapMemberProduct
            {
                Id = 7,
                Code = "C1",
                Name = "N1",
                IsActive = true,
                Detail = new SoapMemberProductDetail
                {
                    PartnerId = 1,
                    ManufacturerId = 2,
                    SupplierId = 3
                }
            };

            var request = new SoapOperationRequest("AddRecordWithDetail", ns)
                .AddParameter("product", product);
            var map = _mapper.ToBodies(request);

            Assert.IsTrue(map.IsSuccess);

            var bodyXml = map.Response.Single().ToString(SaveOptions.DisableFormatting);
            Assert.AreEqual(
                "<AddRecordWithDetail xmlns=\"http://SoapClientCallAssist.local/\"><product><Id>7</Id><Code>C1</Code><Name>N1</Name><IsActive>true</IsActive><Detail><PartnerId>1</PartnerId><ManufacturerId>2</ManufacturerId><SupplierId>3</SupplierId></Detail></product></AddRecordWithDetail>",
                bodyXml);

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                new BuildSoapRequestDto(new HttpClientDto(_baseUri),
                    new SoapEnvelopeDto(map.Response))
            );

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

            var fromResponse = _mapper.FromResponse<SoapMemberAddRecordWithDetailResponse>(response, ns);

            Assert.IsNotNull(fromResponse);
            Assert.IsTrue(fromResponse.IsSuccess);
            Assert.IsNotNull(fromResponse.Response);
            Assert.IsTrue(fromResponse.Response.Result);
        }

        [TestMethod]
        public async Task CallAddRecordWithDetailWithLocationsInHttpPost_Test()
        {
            var client = _clientFactory(SoapProtocolType.SOAP_1_1);
            var ns = XNamespace.Get("http://SoapClientCallAssist.local/");

            var product = new SoapMemberProduct
            {
                Id = 7,
                Code = "C1",
                Name = "N1",
                IsActive = true,
                Detail = new SoapMemberProductDetail
                {
                    PartnerId = 1,
                    ManufacturerId = 2,
                    SupplierId = 3
                }
            };

            var request = new SoapOperationRequest("AddRecordWithDetailWithLocations", ns)
                .AddParameter("product", product)
                .AddParameter("associatedLocationIds", new List<int> { 11, 22 });
            var map = _mapper.ToBodies(request);

            Assert.IsTrue(map.IsSuccess);

            var bodyXml = map.Response.Single().ToString(SaveOptions.DisableFormatting);
            Assert.AreEqual(
                "<AddRecordWithDetailWithLocations xmlns=\"http://SoapClientCallAssist.local/\"><product><Id>7</Id><Code>C1</Code><Name>N1</Name><IsActive>true</IsActive><Detail><PartnerId>1</PartnerId><ManufacturerId>2</ManufacturerId><SupplierId>3</SupplierId></Detail></product><associatedLocationIds><int>11</int><int>22</int></associatedLocationIds></AddRecordWithDetailWithLocations>",
                bodyXml);

            var soapRequest = client.BuildRequest(
                HttpMethod.Post,
                new BuildSoapRequestDto(new HttpClientDto(_baseUri),
                    new SoapEnvelopeDto(map.Response))
            );

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

            var fromResponse = _mapper.FromResponse<SoapMemberAddRecordWithDetailWithLocationsResponse>(response, ns);

            Assert.IsNotNull(fromResponse);
            Assert.IsTrue(fromResponse.IsSuccess);
            Assert.IsNotNull(fromResponse.Response);
            Assert.IsTrue(fromResponse.Response.Result);
        }
    }
}
