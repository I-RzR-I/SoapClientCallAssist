// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssist
//  Author           : RzR
//  Created On       : 2024-09-12 19:14
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-15 19:26
// ***********************************************************************
//  <copyright file="BaseEndpointClient.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using AggregatedGenericResultMessage;
using AggregatedGenericResultMessage.Abstractions;
using AggregatedGenericResultMessage.Extensions.Result;
using DomainCommonExtensions.ArraysExtensions;
using DomainCommonExtensions.CommonExtensions;
using DomainCommonExtensions.CommonExtensions.TypeParam;
using DomainCommonExtensions.DataTypeExtensions;
using Microsoft.Extensions.DependencyInjection;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Messages = SoapClientCallAssist.Helper.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;
using System.Xml;
using SoapClientCallAssist.Dto;

#endregion

// ReSharper disable PossibleMultipleEnumeration

namespace SoapClientCallAssist.Client
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A base endpoint client.
    /// </summary>
    /// =================================================================================================
    public abstract class BaseEndpointClient
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable)
        ///     HTTP client factory.
        /// </summary>
        /// =================================================================================================
        private readonly IHttpClientFactory _clientFactory;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable)
        ///     The client time out.
        /// </summary>
        /// =================================================================================================
        private readonly TimeSpan _clientTimeOut = TimeSpan.FromMinutes(2);

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseEndpointClient" /> class.
        /// </summary>
        /// <param name="clientFactory">HTTP client factory.</param>
        /// =================================================================================================
        protected BaseEndpointClient(IHttpClientFactory clientFactory) => _clientFactory = clientFactory;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseEndpointClient" /> class.
        /// </summary>
        /// =================================================================================================
        protected BaseEndpointClient() => _clientFactory = DefaultHttpClientFactory();

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sends a request.
        /// </summary>
        /// <param name="requestMessage">Message describing the request.</param>
        /// <param name="clientTimeOut">
        ///     (Optional)
        ///     The client time out.
        /// </param>
        /// <returns>
        ///     An IResult&lt;HttpResponseMessage&gt;
        /// </returns>
        /// =================================================================================================
        protected IResult<HttpResponseMessage> SendRequest(HttpRequestMessage requestMessage, TimeSpan clientTimeOut = default)
        {
            try
            {
                var client = _clientFactory.CreateClient();
                client.Timeout = clientTimeOut.IfIsNull(_clientTimeOut);

                var sendRequest = client.SendAsync(requestMessage).GetAwaiter().GetResult();

                return Result<HttpResponseMessage>.Success(sendRequest);
            }
            catch (Exception e)
            {
                return Result<HttpResponseMessage>
                    .Failure(MessageCodes.ER_BEC_BSRM_SR.GetDescription(), Messages.ErrorMessages[MessageCodes.ER_BEC_BSRM_SR])
                    .WithError(e);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sends a request asynchronous.
        /// </summary>
        /// <param name="requestMessage">Message describing the request.</param>
        /// <param name="clientTimeOut">
        ///     (Optional)
        ///     The client time out.
        /// </param>
        /// <param name="cancellationToken">
        ///     (Optional) A token that allows processing to be cancelled.
        /// </param>
        /// <returns>
        ///     The send request.
        /// </returns>
        /// =================================================================================================
        protected async Task<IResult<HttpResponseMessage>> SendRequestAsync(
            HttpRequestMessage requestMessage, TimeSpan clientTimeOut = default,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _clientFactory.CreateClient();
                client.Timeout = clientTimeOut.IfIsNull(_clientTimeOut);

                var sendRequest = await client.SendAsync(requestMessage, cancellationToken);

                return Result<HttpResponseMessage>.Success(sendRequest);
            }
            catch (Exception e)
            {
                return Result<HttpResponseMessage>
                    .Failure(MessageCodes.ER_BEC_BSRM_SRA.GetDescription(), Messages.ErrorMessages[MessageCodes.ER_BEC_BSRM_SRA])
                    .WithError(e);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Builds SOAP request message.
        /// </summary>
        /// <param name="soapRequest">The SOAP request.</param>
        /// <returns>
        ///     An IResult&lt;HttpRequestMessage&gt;
        /// </returns>
        /// =================================================================================================
        protected IResult<HttpRequestMessage> BuildSoapRequestMessage(BaseSoapRequestDto soapRequest)
        {
            try
            {
                var requestValidate = ValidateRequest(soapRequest);
                if (requestValidate.IsSuccess.IsFalse())
                    return Result<HttpRequestMessage>.Failure(requestValidate.GetFirstMessageWithDetails());

                var httpRequestMessage = new HttpRequestMessage();

                if (soapRequest.Method == HttpMethod.Post)
                    httpRequestMessage = new HttpRequestMessage(soapRequest.Method, soapRequest.SoapUri);

                if (soapRequest.Method == HttpMethod.Get)
                {
                    var getRequest = SoapXmlHelper.VerifyAndBuildGetSegment(soapRequest.Method, soapRequest.SoapUri,
                        soapRequest.Bodies, soapRequest.BuildGetRequestAsSlashUrl);
                    if (getRequest.IsSuccess.IsFalse())
                        return Result<HttpRequestMessage>.Failure(getRequest.GetFirstMessageWithDetails());

                    httpRequestMessage = getRequest.Response;
                }

                var soapEnvelopeAttributes = new List<XAttribute>()
                {
                    new XAttribute(XNamespace.Xmlns + "soap", soapRequest.SoapNameSpaceEnvelope.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XAttribute(XNamespace.Xmlns + "i", "http://www.w3.org/2001/XMLSchema-instance")
                };

                if (soapRequest.OwnSoapEnvelopeAttributes.IsNullOrEmptyEnumerable().IsFalse())
                    soapEnvelopeAttributes.AddRange(soapRequest.OwnSoapEnvelopeAttributes);

                var soapEnvelope = new XElement(soapRequest.SoapNameSpaceEnvelope + "Envelope", soapEnvelopeAttributes);
                SoapXmlHelper.BuildSoapHeader(ref soapEnvelope, soapRequest.Headers, soapRequest.SoapNameSpaceEnvelope, soapRequest.Action);

                if (soapRequest.Method == HttpMethod.Post)
                {
                    var soapBodies = SoapXmlHelper.CheckAndValidateSoapBodies(soapRequest.Bodies);
                    soapEnvelope.Add(new XElement(soapRequest.SoapNameSpaceEnvelope + "Body", soapBodies));
                }

                var content = new StringContent(
                    soapEnvelope.ToString(),
                    soapRequest.BodyEncoding.IfIsNull(Encoding.UTF8),
                    soapRequest.MediaType);

                if (soapRequest.Action.IsNullOrEmpty().IsFalse())
                {
                    content.Headers.Add("SOAPAction", soapRequest.Action);
                    content.Headers.Add("Action", soapRequest.Action);
                }

                if (soapRequest.Action.IsNullOrEmpty().IsFalse() && soapRequest.SoapProtocol == SoapProtocolType.SOAP_1_2)
                    content.Headers.ContentType!.Parameters.Add(new NameValueHeaderValue("ActionParameter", $"\"{soapRequest.Action}\""));

                if (soapRequest.HttpClientHeaders.IsNullOrEmptyEnumerable().IsFalse())
                    foreach (var clientHeader in soapRequest.HttpClientHeaders)
                        content.Headers.TryAddWithoutValidation(clientHeader.Key, clientHeader.Value);

                httpRequestMessage.Content = content;

                return Result<HttpRequestMessage>.Success(httpRequestMessage);
            }
            catch (Exception e)
            {
                return Result<HttpRequestMessage>
                    .Failure(MessageCodes.ER_BEC_BSRM.GetDescription(), Messages.ErrorMessages[MessageCodes.ER_BEC_BSRM])
                    .WithError(e);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Check body for fault code.
        /// </summary>
        /// <param name="soapResponseBody">The SOAP response body.</param>
        /// <param name="soapNamespace">The SOAP namespace.</param>
        /// <returns>
        ///     An IResult.
        /// </returns>
        /// =================================================================================================
        protected IResult CheckBodyForFaultCode(string soapResponseBody, string soapNamespace)
        {
            try
            {
                var doc = XDocument.Parse(soapResponseBody);
                XNamespace xmlns = soapNamespace;
                var orderNode = doc.Descendants(xmlns + "Fault");

                var faultError = "";
                foreach (var element in orderNode)
                {
                    if (element.Name.LocalName == "Fault")
                    {
                        faultError = element.Value;
                        break;
                    }
                }

                return faultError.IsPresent()
                    ? Result.Failure(faultError)
                    : Result.Success();
            }
            catch (Exception ex)
            {
                return Result
                    .Failure(MessageCodesType.ER_BEC_CBFFC.GetDescription(), DefaultResultMessageHelper.ErrorMessages[MessageCodesType.ER_BEC_CBFFC])
                    .WithError(ex);
            }
        }

        /// <inheritdoc cref="ISoapClientEndpoint.GetXmlNodeResponseBody"/>
        public IResult<XmlNode> GetXmlNodeResponseBody(string soapResponseBody, string soapNamespace = null, string soapXmlBodyTag = null)
        {
            try
            {
                var xmlDocument = new XmlDocument { PreserveWhitespace = true };
                xmlDocument.LoadXml(soapResponseBody);
                var bodyNodes = SoapXmlHelper.ParseGetContentBody(soapXmlBodyTag, xmlDocument, soapNamespace);

                if (bodyNodes.Count != 1)
                    return Result<XmlNode>.Failure(MessageCodesType.ER_BEC_GRB_01.GetDescription(), DefaultResultMessageHelper.ErrorMessages[MessageCodesType.ER_BEC_GRB_01]);

                var body = bodyNodes[0];

                return body.FirstChild.IsNull()
                    ? Result<XmlNode>.Failure(MessageCodesType.ER_BEC_GRB_02.GetDescription(), DefaultResultMessageHelper.ErrorMessages[MessageCodesType.ER_BEC_GRB_02])
                    : Result<XmlNode>.Success(body.FirstChild);
            }
            catch (Exception ex)
            {
                return Result<XmlNode>
                    .Failure(MessageCodesType.ER_BEC_GRB_03.GetDescription(), DefaultResultMessageHelper.ErrorMessages[MessageCodesType.ER_BEC_GRB_03])
                    .WithError(ex);
            }
        }

        /// <inheritdoc cref="ISoapClientEndpoint.GetXNodeResponseBody"/>
        public IResult<XNode> GetXNodeResponseBody(string soapResponseBody, string soapNamespace = null, string soapXmlBodyTag = null)
        {
            try
            {
                var xmlNode = GetXmlNodeResponseBody(soapResponseBody, soapNamespace, soapXmlBodyTag);

                return xmlNode.IsSuccess.IsFalse()
                    ? Result<XNode>.Failure(xmlNode.GetFirstMessageWithDetails())
                    : Result<XNode>.Success(XDocument.Parse(xmlNode.Response.OuterXml));
            }
            catch (Exception ex)
            {
                return Result<XNode>
                    .Failure(MessageCodesType.ER_BEC_GRB_03.GetDescription(), DefaultResultMessageHelper.ErrorMessages[MessageCodesType.ER_BEC_GRB_03])
                    .WithError(ex);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Default HTTP client factory.
        /// </summary>
        /// <returns>
        ///     An IHttpClientFactory.
        /// </returns>
        /// =================================================================================================
        private static IHttpClientFactory DefaultHttpClientFactory()
        {
            var serviceProvider = new ServiceCollection();

            serviceProvider
                .AddHttpClient(nameof(BaseEndpointClient))
                .ConfigurePrimaryHttpMessageHandler(_ =>
                    new HttpClientHandler { AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip });

            return serviceProvider.BuildServiceProvider()
                .GetService<IHttpClientFactory>()!;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Validates the request described by soapRequest.
        /// </summary>
        /// <param name="soapRequest">The SOAP request.</param>
        /// <returns>
        ///     An IResult.
        /// </returns>
        /// =================================================================================================
        private static IResult ValidateRequest(BaseSoapRequestDto soapRequest)
        {
            try
            {
                if (soapRequest.IsNull())
                    return Result.Failure(MessageCodes.V_BEC_VR_001.GetDescription(),
                        Messages.ValidationMessages[MessageCodes.V_BEC_VR_001]);

                if (new List<HttpMethod> { HttpMethod.Post, HttpMethod.Get }
                    .Any(x => x == soapRequest.Method).IsFalse())
                    return Result.Failure(MessageCodes.V_BEC_VR_002.GetDescription(),
                        string.Format(Messages.ValidationMessages[MessageCodes.V_BEC_VR_002], soapRequest.Method));

                if (soapRequest.SoapUri.IsNull())
                    return Result.Failure(MessageCodes.V_BEC_VR_003.GetDescription(),
                        Messages.ValidationMessages[MessageCodes.V_BEC_VR_003]);

                if (soapRequest.SoapProtocol.IsNull())
                    return Result.Failure(MessageCodes.V_BEC_VR_004.GetDescription(),
                        Messages.ValidationMessages[MessageCodes.V_BEC_VR_004]);

                if (soapRequest.SoapNameSpaceEnvelope.IsNull())
                    return Result.Failure(MessageCodes.V_BEC_VR_005.GetDescription(),
                        Messages.ValidationMessages[MessageCodes.V_BEC_VR_005]);

                if (soapRequest.MediaType.IsNullOrEmpty())
                    return Result.Failure(MessageCodes.V_BEC_VR_006.GetDescription(),
                        Messages.ValidationMessages[MessageCodes.V_BEC_VR_006]);

                if (soapRequest.BodyEncoding.IsNull())
                    return Result.Failure(MessageCodes.V_BEC_VR_007.GetDescription(),
                        Messages.ValidationMessages[MessageCodes.V_BEC_VR_007]);

                return Result.Success();
            }
            catch (Exception e)
            {
                return Result
                    .Failure(MessageCodes.ER_BEC_VR.GetDescription(), Messages.ErrorMessages[MessageCodes.ER_BEC_VR])
                    .WithError(e);
            }
        }
    }
}