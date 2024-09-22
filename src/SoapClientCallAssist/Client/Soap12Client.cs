// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssist
//  Author           : RzR
//  Created On       : 2024-09-12 19:05
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-15 19:25
// ***********************************************************************
//  <copyright file="Soap12Client.cs" company="">
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
using DomainCommonExtensions.CommonExtensions;
using DomainCommonExtensions.DataTypeExtensions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Helper;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Client
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     SOAP 1.2 client.
    /// </summary>
    /// <seealso cref="T:SoapClientCallAssist.Client.BaseEndpointClient"/>
    /// <seealso cref="T:SoapClientCallAssist.Abstractions.ISoapClientEndpoint"/>
    /// =================================================================================================
    public sealed class Soap12Client : BaseEndpointClient, ISoapClientEndpoint
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     The client time out.
        /// </summary>
        /// =================================================================================================
        private TimeSpan _clientTimeOut = TimeSpan.FromMinutes(2);

        /// <inheritdoc/>
        public Soap12Client(IHttpClientFactory clientFactory) : base(clientFactory) { }

        /// <inheritdoc/>
        public Soap12Client() { }

        /// <inheritdoc/>
        public IResult<HttpRequestMessage> BuildRequest(
            HttpMethod method,
            Uri endpoint,
            IEnumerable<XElement> bodies,
            IEnumerable<XElement> headers = null,
            Encoding bodyEncoding = null,
            string action = null,
            IEnumerable<XAttribute> ownSoapEnvelopeAttributes = null,
            Dictionary<string, IEnumerable<string>> httpClientHeaders = null,
            bool buildGetRequestAsSlashUrl = false)
        {
            try
            {
                var requestMessage = BuildRequest(
                    method,
                    new BuildSoapRequestDto(
                        new HttpClientDto()
                        {
                            BodyEncoding = bodyEncoding,
                            BuildGetRequestAsSlashUrl = buildGetRequestAsSlashUrl,
                            Endpoint = endpoint,
                            HttpClientHeaders = httpClientHeaders
                        },
                        new SoapEnvelopeDto()
                        {
                            Bodies = bodies,
                            Headers = headers,
                            OwnSoapEnvelopeAttributes = ownSoapEnvelopeAttributes,
                            Action = action
                        }
                    ));

                return requestMessage.IsSuccess.IsFalse() 
                    ? Result<HttpRequestMessage>.Failure(requestMessage.GetFirstMessage()) 
                    : Result<HttpRequestMessage>.Success(requestMessage.Response);
            }
            catch (Exception e)
            {
                return Result<HttpRequestMessage>
                    .Failure(MessageCodesType.ER_S12_BSR.GetDescription(), DefaultResultMessageHelper.ErrorMessages[MessageCodesType.ER_S12_BSR])
                    .WithError(e);
            }
        }

        /// <inheritdoc/>
        public IResult<HttpRequestMessage> BuildRequest(
            HttpMethod method,
            BuildSoapRequestDto soapRequest)
        {
            try
            {
                var requestMessage = BuildSoapRequestMessage(
                    new BaseSoapRequestDto
                    {
                        Method = method,
                        MediaType = SoapMediaType.Soap12.GetDescription(),
                        Action = soapRequest.Envelope.Action,
                        Bodies = soapRequest.Envelope.Bodies,
                        BodyEncoding = soapRequest.Client.BodyEncoding,
                        Headers = soapRequest.Envelope.Headers,
                        SoapNameSpaceEnvelope = SoapNamespaceType.Soap12.GetDescription(),
                        SoapProtocol = SoapProtocolType.SOAP_1_2,
                        SoapUri = soapRequest.Client.Endpoint,
                        OwnSoapEnvelopeAttributes = soapRequest.Envelope.OwnSoapEnvelopeAttributes,
                        HttpClientHeaders = soapRequest.Client.HttpClientHeaders,
                        BuildGetRequestAsSlashUrl = soapRequest.Client.BuildGetRequestAsSlashUrl
                    });

                return requestMessage.IsSuccess.IsFalse()
                    ? Result<HttpRequestMessage>.Failure(requestMessage.GetFirstMessage())
                    : Result<HttpRequestMessage>.Success(requestMessage.Response);
            }
            catch (Exception e)
            {
                return Result<HttpRequestMessage>
                    .Failure(MessageCodesType.ER_S12_BSR.GetDescription(), DefaultResultMessageHelper.ErrorMessages[MessageCodesType.ER_S12_BSR])
                    .WithError(e);
            }
        }

        /// <inheritdoc />
        public IResult<HttpResponseMessage> SendRequest(HttpRequestMessage request)
        {
            try
            {
                var soapResult = base.SendRequest(request, _clientTimeOut);

                return soapResult.IsSuccess.IsFalse()
                    ? Result<HttpResponseMessage>.Failure(soapResult.GetFirstMessage()) 
                    : Result<HttpResponseMessage>.Success(soapResult.Response);
            }
            catch (Exception e)
            {
                return Result<HttpResponseMessage>
                    .Failure(MessageCodesType.ER_S11_SR.GetDescription(), DefaultResultMessageHelper.ErrorMessages[MessageCodesType.ER_S11_SR])
                    .WithError(e);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<HttpResponseMessage>> SendRequestAsync(HttpRequestMessage request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var soapResult = await base.SendRequestAsync(request, _clientTimeOut, cancellationToken);
                
                return soapResult.IsSuccess.IsFalse() 
                    ? Result<HttpResponseMessage>.Failure(soapResult.GetFirstMessage()) 
                    : Result<HttpResponseMessage>.Success(soapResult.Response);
            }
            catch (Exception e)
            {
                return Result<HttpResponseMessage>
                    .Failure(MessageCodesType.ER_S11_SRA.GetDescription(), DefaultResultMessageHelper.ErrorMessages[MessageCodesType.ER_S11_SRA])
                    .WithError(e);
            }
        }
        /// <inheritdoc />
        public IResult SetClientTimeout(TimeSpan clientTimeout)
        {
            if (clientTimeout.IsNotNull())
                _clientTimeOut = clientTimeout;

            return Result.Success();
        }

        /// <inheritdoc />
        public IResult CheckBodyForFaultCode(string soapResponse)
            => base.CheckBodyForFaultCode(soapResponse, SoapNamespaceType.Soap12.GetDescription());
    }
}