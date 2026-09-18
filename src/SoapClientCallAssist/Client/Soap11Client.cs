// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-12 19:05
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="Soap11Client.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using RzR.ResultMessage.Extensions.Result;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helpers;
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
    /// <summary>
    ///     The SOAP 1.1 client, sending <c>text/xml</c> envelopes in the SOAP 1.1 namespace.
    /// </summary>
    public sealed class Soap11Client : BaseEndpointClient, ISoapClientEndpoint
    {
        /// <inheritdoc/>
        public Soap11Client(IHttpClientFactory clientFactory) : base(clientFactory) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Soap11Client" /> class with the verifier
        ///     response signatures are checked with when the security options of a call name none.
        /// 
        /// </summary>
        /// <param name="clientFactory">HTTP client factory.</param>
        /// <param name="responseVerifier">The response verifier, or null for the library's own.</param>
        public Soap11Client(IHttpClientFactory clientFactory, ISoapMessageVerifier responseVerifier)
            : base(clientFactory, responseVerifier) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Soap11Client" /> class with the verifier and
        ///     the response security service.
        /// </summary>
        /// <param name="clientFactory">HTTP client factory.</param>
        /// <param name="responseVerifier">The response verifier, or null for the library's own.</param>
        /// <param name="responseSecurity">The response security service, or null to build one.</param>
        public Soap11Client(IHttpClientFactory clientFactory, ISoapMessageVerifier responseVerifier, 
            ISoapResponseSecurity responseSecurity)
            : base(clientFactory, responseVerifier, responseSecurity) { }

        /// <inheritdoc/>
        public Soap11Client() { }

        /// <inheritdoc/>
        public IResult<HttpRequestMessage> BuildRequest(HttpMethod method, Uri endpoint,
            IEnumerable<XElement> bodies, IEnumerable<XElement> headers = null,
            Encoding bodyEncoding = null, string action = null,
            IEnumerable<XAttribute> ownSoapEnvelopeAttributes = null,
            Dictionary<string, IEnumerable<string>> httpClientHeaders = null,
            bool buildGetRequestAsSlashUrl = false)
        {
            try
            {
                var requestMessage = BuildRequest(
                    method,
                    new BuildSoapRequestDto(
                        new HttpClientDto
                        {
                            BodyEncoding = bodyEncoding,
                            BuildGetRequestAsSlashUrl = buildGetRequestAsSlashUrl,
                            Endpoint = endpoint, 
                            HttpClientHeaders = httpClientHeaders
                        },
                        new SoapEnvelopeDto
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
                    .Failure(MessageCodesType.ER_S11_BSR.GetDescription(), DefaultResultMessageHelper.GetErrorMessage(MessageCodesType.ER_S11_BSR))
                    .WithError(e);
            }
        }

        /// <inheritdoc/>
        public IResult<HttpRequestMessage> BuildRequest(HttpMethod method, BuildSoapRequestDto soapRequest)
        {
            try
            {
                var requestMessage = BuildSoapRequestMessage(
                    new BaseSoapRequestDto
                    {
                        Method = method,
                        MediaType = SoapMediaType.Soap11.GetDescription(),
                        Action = soapRequest.Envelope.Action,
                        Bodies = soapRequest.Envelope.Bodies,
                        BodyEncoding = soapRequest.Client.BodyEncoding,
                        Headers = soapRequest.Envelope.Headers,
                        SoapNameSpaceEnvelope = SoapNamespaceType.Soap11.GetDescription(),
                        SoapProtocol = SoapProtocolType.SOAP_1_1,
                        SoapUri = soapRequest.Client.Endpoint,
                        OwnSoapEnvelopeAttributes = soapRequest.Envelope.OwnSoapEnvelopeAttributes,
                        HttpClientHeaders = soapRequest.Client.HttpClientHeaders,
                        BuildGetRequestAsSlashUrl = soapRequest.Client.BuildGetRequestAsSlashUrl,
                        Security = soapRequest.Security
                    });

                return requestMessage.IsSuccess.IsFalse()
                    ? requestMessage.Propagate<HttpRequestMessage>()
                    : Result<HttpRequestMessage>.Success(requestMessage.Response);
            }
            catch (Exception e)
            {
                return Result<HttpRequestMessage>
                    .Failure(MessageCodesType.ER_S11_BSR.GetDescription(), DefaultResultMessageHelper.GetErrorMessage(MessageCodesType.ER_S11_BSR))
                    .WithError(e);
            }
        }

        /// <inheritdoc/>
        public IResult<HttpResponseMessage> SendRequest(HttpRequestMessage request)
        {
            try
            {
                return base.SendRequest(request, ClientTimeout);
            }
            catch (Exception e)
            {
                return Result<HttpResponseMessage>
                    .Failure(MessageCodesType.ER_S11_SR.GetDescription(), DefaultResultMessageHelper.GetErrorMessage(MessageCodesType.ER_S11_SR))
                    .WithError(e);
            }
        }

        /// <inheritdoc/>
        public async Task<IResult<HttpResponseMessage>> SendRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SendRequestAsync(request, ClientTimeout, cancellationToken);
            }
            catch (Exception e)
            {
                return Result<HttpResponseMessage>
                    .Failure(MessageCodesType.ER_S11_SRA.GetDescription(), DefaultResultMessageHelper.GetErrorMessage(MessageCodesType.ER_S11_SRA))
                    .WithError(e);
            }
        }

        /// <inheritdoc/>
        public IResult SetClientTimeout(TimeSpan clientTimeout) => ApplyClientTimeout(clientTimeout);

        /// <inheritdoc/>
        public IResult CheckBodyForFaultCode(string soapResponse)
            => base.CheckBodyForFaultCode(soapResponse, SoapNamespaceType.Soap11.GetDescription());
    }
}