// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-12 19:14
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 02:30
//  ***********************************************************************
//  <copyright file="BaseEndpointClient.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Collections;
using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Reflection.TypeParam;
using RzR.Extensions.Domain.Text;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using RzR.ResultMessage.Extensions.Result;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helpers;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

// ReSharper disable PossibleMultipleEnumeration

namespace SoapClientCallAssist.Client
{
    /// <summary>
    ///     The shared base of the SOAP clients: envelope assembly, WS-Security signing, sending,
    ///     timeout handling and response reading, once for both protocols.
    /// </summary>
    public abstract class BaseEndpointClient
    {
        /// <summary>
        ///     (Immutable)
        ///     The factory every send draws its <see cref="HttpClient" /> from.
        /// </summary>
        private readonly IHttpClientFactory _clientFactory;

        /// <summary>
        ///     (Immutable)
        ///     The verifier a response signature is checked with when the security options name none, or
        ///     null to fall back to the library's own implementation.
        /// </summary>
        private readonly ISoapMessageVerifier _responseVerifier;

        /// <summary>
        ///     (Immutable)
        ///     The response security service the verification shims delegate to, or null to build one
        ///     over <see cref="_responseVerifier" /> on demand.
        /// </summary>
        private readonly ISoapResponseSecurity _responseSecurity;

        /// <summary>
        ///     (Immutable)
        ///     The timeout applied to a send when the caller has configured none, two minutes.
        /// </summary>
        private static readonly TimeSpan DefaultClientTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        ///     (Immutable)
        ///     The longest timeout <see cref="HttpClient" /> accepts; a larger value is refused.
        /// </summary>
        private static readonly TimeSpan MaxClientTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        /// <summary>
        ///     (Immutable)
        ///     The same bound as <see cref="MaxClientTimeout" />, in milliseconds.
        /// </summary>
        private const double MaxClientTimeoutMilliseconds = int.MaxValue;

        /// <summary>
        ///     The configured client timeout, held as ticks. It is read and written atomically.
        /// </summary>
        private long _clientTimeoutTicks = DefaultClientTimeout.Ticks;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseEndpointClient" /> class.
        /// </summary>
        /// <param name="clientFactory">HTTP client factory.</param>
        protected BaseEndpointClient(IHttpClientFactory clientFactory) 
            : this(clientFactory, null) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseEndpointClient" /> class with the
        ///     verifier response signatures are checked with.
        /// </summary>
        /// <param name="clientFactory">HTTP client factory.</param>
        /// <param name="responseVerifier">The fallback verifier, or null for the library's own.</param>
        protected BaseEndpointClient(IHttpClientFactory clientFactory, ISoapMessageVerifier responseVerifier)
            : this(clientFactory, responseVerifier, null) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseEndpointClient" /> class with the
        ///     verifier and the response security service.
        /// </summary>
        /// <param name="clientFactory">HTTP client factory.</param>
        /// <param name="responseVerifier">The fallback verifier, or null for the library's own.</param>
        /// <param name="responseSecurity">The response security service, or null to build one.</param>
        protected BaseEndpointClient(IHttpClientFactory clientFactory, 
            ISoapMessageVerifier responseVerifier, ISoapResponseSecurity responseSecurity)
        {
            _clientFactory = clientFactory;
            _responseVerifier = responseVerifier;
            _responseSecurity = responseSecurity;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseEndpointClient" /> class over the
        ///     library's shared fallback factory.
        /// </summary>
        protected BaseEndpointClient()
            : this(SoapHttpClientRegistration.FallbackFactory, null) { }

        /// <summary>
        ///     The timeout applied to every send this client makes. It is read atomically and shared by
        ///     every call in flight on this instance.
        /// </summary>
        /// <value>
        ///     The client timeout.
        /// </value>
        protected TimeSpan ClientTimeout 
            => TimeSpan.FromTicks(Interlocked.Read(ref _clientTimeoutTicks));

        /// <summary>
        ///     Sends a request and succeeds only on a 2xx status. Any other status fails under a code
        ///     naming its class, with the <see cref="HttpResponseMessage" /> still carried in the result.
        /// 
        /// </summary>
        /// <param name="requestMessage">Message describing the request.</param>
        /// <param name="clientTimeOut">
        ///     (Optional) The timeout for this send, or default for the client's own.
        /// </param>
        /// <returns>
        ///     The response on a 2xx status, or a failed result for any other outcome.
        /// </returns>
        protected IResult<HttpResponseMessage> SendRequest(HttpRequestMessage requestMessage, 
            TimeSpan clientTimeOut = default)
        {
            try
            {
                var client = CreateConfiguredClient(clientTimeOut);

                var sendRequest = client.SendAsync(requestMessage).GetAwaiter().GetResult();

                return SoapHttpStatusClassifier.Classify(sendRequest);
            }
            catch (Exception e)
            {
                return Result<HttpResponseMessage>
                    .Failure(MessageCodes.ER_BEC_BSRM_SR.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_BEC_BSRM_SR))
                    .WithOptionalError(e, "sending the SOAP request");
            }
        }

        /// <summary>
        ///     Sends a request asynchronously under the same status contract as
        ///     <see cref="SendRequest" />.
        /// </summary>
        /// <param name="requestMessage">Message describing the request.</param>
        /// <param name="clientTimeOut">
        ///     (Optional) The timeout for this send, or default for the client's own.
        /// </param>
        /// <param name="cancellationToken">(Optional) A token that cancels the send.</param>
        /// <returns>
        ///     The response on a 2xx status, or the failure described on <see cref="SendRequest" />.
        /// </returns>
        protected async Task<IResult<HttpResponseMessage>> SendRequestAsync(
            HttpRequestMessage requestMessage, TimeSpan clientTimeOut = default,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateConfiguredClient(clientTimeOut);

                var sendRequest = await client.SendAsync(requestMessage, cancellationToken);

                return SoapHttpStatusClassifier.Classify(sendRequest);
            }
            catch (Exception e)
            {
                return Result<HttpResponseMessage>
                    .Failure(MessageCodes.ER_BEC_BSRM_SRA.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_BEC_BSRM_SRA))
                    .WithOptionalError(e, "sending the SOAP request asynchronously");
            }
        }

        /// <summary>
        ///     Assembles the envelope of a validated SOAP request, signs it when security is enabled,
        ///     applies the caller's HTTP headers and stores the key material on the message.
        /// </summary>
        /// <param name="soapRequest">The SOAP request.</param>
        /// <returns>
        ///     The request ready to send, or the validation, signing, header or build failure.
        /// </returns>
        protected IResult<HttpRequestMessage> BuildSoapRequestMessage(BaseSoapRequestDto soapRequest)
        {
            RequestKeyMaterial keyMaterial = null;

            try
            {
                var requestValidate = ValidateRequest(soapRequest);
                if (requestValidate.IsSuccess.IsFalse())
                    return requestValidate.Propagate<HttpRequestMessage>();

                var httpRequestMessage = new HttpRequestMessage();

                if (soapRequest.Method == HttpMethod.Post)
                    httpRequestMessage = new HttpRequestMessage(soapRequest.Method, soapRequest.SoapUri);

                if (soapRequest.Method == HttpMethod.Get)
                {
                    var getRequest = SoapXmlHelper.VerifyAndBuildGetSegment(soapRequest.Method, soapRequest.SoapUri,
                        soapRequest.Bodies, soapRequest.BuildGetRequestAsSlashUrl);
                    if (getRequest.IsSuccess.IsFalse())
                        return getRequest.Propagate<HttpRequestMessage>();

                    httpRequestMessage = getRequest.Response;
                }

                var soapEnvelopeAttributes = new List<XAttribute>
                {
                    new(XNamespace.Xmlns + "soap", soapRequest.SoapNameSpaceEnvelope.NamespaceName),
                    new(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new(XNamespace.Xmlns + "i", "http://www.w3.org/2001/XMLSchema-instance")
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

                var wireResult = SoapEnvelopeWireWriter.Write(
                    soapEnvelope, soapRequest.Security, soapRequest.Action, soapRequest.SoapUri, out keyMaterial);
                if (wireResult.IsSuccess.IsFalse())
                    return wireResult.Propagate<HttpRequestMessage>();

                var content = new StringContent(
                    wireResult.Response,
                    soapRequest.BodyEncoding.IfIsNull(Encoding.UTF8),
                    soapRequest.MediaType);

                if (soapRequest.Action.IsNullOrEmpty().IsFalse())
                {
                    content.Headers.Add("SOAPAction", soapRequest.Action);
                    content.Headers.Add("Action", soapRequest.Action);
                }

                if (soapRequest.Action.IsNullOrEmpty().IsFalse() && soapRequest.SoapProtocol == SoapProtocolType.SOAP_1_2)
                    content.Headers.ContentType!.Parameters.Add(new NameValueHeaderValue("ActionParameter", $"\"{soapRequest.Action}\""));

                httpRequestMessage.Content = content;

                var headersApplied = ApplyClientHeaders(httpRequestMessage, soapRequest.HttpClientHeaders);
                if (headersApplied.IsSuccess.IsFalse())
                {
                    keyMaterial?.Dispose();

                    return headersApplied.Propagate<HttpRequestMessage>();
                }

                if (keyMaterial.IsNotNull())
                    httpRequestMessage.Properties[SoapClientEndpointExtensions.RequestSecurityStateKey] = keyMaterial;

                return Result<HttpRequestMessage>.Success(httpRequestMessage);
            }
            catch (Exception e)
            {
                keyMaterial?.Dispose();

                return Result<HttpRequestMessage>
                    .Failure(MessageCodes.ER_BEC_BSRM.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_BEC_BSRM))
                    .WithError(e);
            }
        }

        /// <summary>
        ///     Checks the Body directly under the envelope for a SOAP fault. A non-envelope document
        ///     fails, and an envelope in the other protocol's namespace passes untouched.
        /// </summary>
        /// <param name="soapResponseBody">The raw SOAP response.</param>
        /// <param name="soapNamespace">The envelope namespace of this client's protocol.</param>
        /// <returns>
        ///     Success when the Body carries no fault, or a failed result.
        /// </returns>
        protected IResult CheckBodyForFaultCode(string soapResponseBody, string soapNamespace)
        {
            try
            {
                var loaded = SoapXmlDocumentLoader.Load(soapResponseBody, false, MessageCodes.ER_BEC_CBFFC);
                if (loaded.IsSuccess.IsFalse())
                    return loaded.ToBase();

                var envelope = SoapXmlHelper.LocateSoapEnvelope(loaded.Response);
                if (envelope.IsNull())
                    return NoSingleBodyFailure();

                if (string.Equals(envelope!.NamespaceURI, soapNamespace, StringComparison.Ordinal).IsFalse())
                    return Result.Success();

                var body = SoapXmlHelper.SingleChildElement(envelope, SoapContracts.BodyLocalName, soapNamespace);
                if (body.IsNull())
                    return NoSingleBodyFailure();

                var fault = SoapXmlHelper.LocateSoapFault(body, soapNamespace);

                var faultError = fault.IsNull() ? string.Empty : fault!.InnerText;

                return faultError.IsPresent()
                    ? Result.Failure(MessageCodes.ER_BEC_FLT.GetDescription(), faultError)
                    : Result.Success();
            }
            catch (Exception ex)
            {
                return Result
                    .Failure(MessageCodesType.ER_BEC_CBFFC.GetDescription(), DefaultResultMessageHelper.GetErrorMessage(MessageCodesType.ER_BEC_CBFFC))
                    .WithError(ex);
            }
        }

        /// <inheritdoc cref="ISoapClientEndpoint.GetXmlNodeResponseBody"/>
        public IResult<XmlNode> GetXmlNodeResponseBody(string soapResponseBody, string soapNamespace = null, string soapXmlBodyTag = null)
        {
            try
            {
                var loaded = SoapXmlDocumentLoader.Load(soapResponseBody, true, MessageCodes.ER_BEC_GRB_03);
                if (loaded.IsSuccess.IsFalse())
                    return loaded.Propagate<XmlNode>();

                var body = SoapXmlHelper.LocateSoapBody(loaded.Response, soapNamespace, soapXmlBodyTag);
                if (body.IsNull())
                    return NoSingleBodyFailure().Propagate<XmlNode>();

                var payload = SoapXmlHelper.FirstChildElement(body);

                return payload.IsNull()
                    ? Result<XmlNode>.Failure(MessageCodesType.ER_BEC_GRB_02.GetDescription(), DefaultResultMessageHelper.GetErrorMessage(MessageCodesType.ER_BEC_GRB_02))
                    : Result<XmlNode>.Success(payload);
            }
            catch (Exception ex)
            {
                return Result<XmlNode>
                    .Failure(MessageCodesType.ER_BEC_GRB_03.GetDescription(), DefaultResultMessageHelper.GetErrorMessage(MessageCodesType.ER_BEC_GRB_03))
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
                    ? xmlNode.Propagate<XNode>()
                    : Result<XNode>.Success(XDocument.Parse(xmlNode.Response.OuterXml));
            }
            catch (Exception ex)
            {
                return Result<XNode>
                    .Failure(MessageCodesType.ER_BEC_GRB_03.GetDescription(), DefaultResultMessageHelper.GetErrorMessage(MessageCodesType.ER_BEC_GRB_03))
                    .WithError(ex);
            }
        }

        /// <summary>
        ///     Verifies the WS-Security signature on a SOAP response the caller has already read,
        ///     against the expected certificate and under a policy.
        /// </summary>
        /// <param name="soapResponseBody">
        ///     The raw SOAP response body the caller read from the response.
        /// </param>
        /// <param name="expectedCertificate">The trusted certificate; null fails.</param>
        /// <param name="policy">
        ///     (Optional) The conditions to meet, or null for the strict defaults.
        /// </param>
        /// <returns>
        ///     An IResult&lt;SoapSignatureVerificationResult&gt; that succeeds only when the signature
        ///     is cryptographically valid and meets every condition the policy names.
        /// </returns>
        public IResult<SoapSignatureVerificationResult> VerifyResponseSignature(
            string soapResponseBody, X509Certificate2 expectedCertificate, SoapVerificationPolicyDto policy = null)
            => ResponseSecurity().Verify(soapResponseBody, expectedCertificate, policy);

        /// <summary>
        ///     Verifies the WS-Security signature on a SOAP response the caller has already read, using
        ///     the certificate, policy and verifier in <paramref name="security" />. Symmetric-binding
        ///     and secure-conversation options are refused.
        /// </summary>
        /// <param name="soapResponseBody">
        ///     The raw SOAP response body the caller read from the response.
        /// </param>
        /// <param name="security">
        ///     The options naming the expected response certificate; null fails.
        /// </param>
        /// <returns>
        ///     Success only when the signature is valid and meets the configured policy, or a failed
        ///     result.
        /// </returns>
        public IResult<SoapSignatureVerificationResult> VerifyResponseSignature(
            string soapResponseBody, SoapSecurityDto security)
            => ResponseSecurity().Verify(soapResponseBody, security);

        /// <summary>
        ///     Resolves the response security service the verification shims delegate to, either the one
        ///     this client was constructed with or the library's own over its verifier.
        /// </summary>
        /// <returns>
        ///     The response security service.
        /// </returns>
        private ISoapResponseSecurity ResponseSecurity()
            => _responseSecurity ?? new WsSecurityResponseSecurity(_responseVerifier);

        /// <summary>
        ///     Builds the failure returned when a response resolves no single SOAP Body, or is not a
        ///     SOAP envelope at all.
        /// </summary>
        /// <returns>
        ///     The failed IResult for a missing or ambiguous Body.
        /// </returns>
        private static IResult NoSingleBodyFailure()
            => Result.Failure(MessageCodes.ER_BEC_GRB_01.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_BEC_GRB_01));

        /// <summary>
        ///     Applies the caller's HTTP headers to the request, offering each to the request collection
        ///     first and to the content collection second. A header neither accepts fails the build.
        /// 
        /// </summary>
        /// <param name="requestMessage">The request being built, carrying its content.</param>
        /// <param name="clientHeaders">The caller's HTTP headers, or null.</param>
        /// <returns>
        ///     Success once every header is applied, or a failed result naming the first header neither
        ///     collection accepts or whose value list is null.
        /// </returns>
        private static IResult ApplyClientHeaders(HttpRequestMessage requestMessage, 
            IDictionary<string, IEnumerable<string>> clientHeaders)
        {
            if (clientHeaders.IsNullOrEmptyEnumerable())
                return Result.Success();

            foreach (var clientHeader in clientHeaders)
            {
                var applied = clientHeader.Value.IsNotNull()
                              && (requestMessage.Headers.TryAddWithoutValidation(clientHeader.Key, clientHeader.Value)
                                  || requestMessage.Content.Headers.TryAddWithoutValidation(clientHeader.Key, clientHeader.Value));

                if (applied.IsFalse())
                {
                    return Result.Failure(
                        MessageCodes.V_BEC_HDR_001.GetDescription(),
                        Messages.GetValidationMessage(MessageCodes.V_BEC_HDR_001).TryFormatWith(clientHeader.Key));
                }
            }

            return Result.Success();
        }

        /// <summary>
        ///     Validates and stores a caller-supplied client timeout; the write is atomic and shared by
        ///     every call in flight.
        /// </summary>
        /// <param name="clientTimeout">
        ///     The timeout, or <see cref="Timeout.InfiniteTimeSpan" /> for none.
        /// </param>
        /// <returns>
        ///     Success once stored, or a failed result when the value is one <see cref="HttpClient" />
        ///     would refuse.
        /// </returns>
        protected IResult ApplyClientTimeout(TimeSpan clientTimeout)
        {
            if (IsAcceptableClientTimeout(clientTimeout).IsFalse())
            {
                return Result.Failure(
                    MessageCodes.V_BEC_TMO_001.GetDescription(),
                    Messages.GetValidationMessage(MessageCodes.V_BEC_TMO_001).TryFormatWith(MaxClientTimeout));
            }

            Interlocked.Exchange(ref _clientTimeoutTicks, clientTimeout.Ticks);

            return Result.Success();
        }

        /// <summary>
        ///     Determines whether a timeout is <see cref="Timeout.InfiniteTimeSpan" /> or a positive
        ///     value of at most <see cref="int.MaxValue" /> milliseconds, the range
        ///     <see cref="HttpClient" /> accepts.
        /// </summary>
        /// <param name="clientTimeout">The timeout to classify.</param>
        /// <returns>
        ///     True when the timeout can be applied to an <see cref="HttpClient" /> as it stands.
        /// </returns>
        private static bool IsAcceptableClientTimeout(TimeSpan clientTimeout)
            => clientTimeout == Timeout.InfiniteTimeSpan
               || (clientTimeout > TimeSpan.Zero && clientTimeout.TotalMilliseconds <= MaxClientTimeoutMilliseconds);

        /// <summary>
        ///     Creates the HTTP client a send goes out on, under the library's registered client name,
        ///     with the timeout this send asks for; <see cref="Timeout.InfiniteTimeSpan" /> is honoured.
        /// 
        /// </summary>
        /// <param name="clientTimeOut">The timeout, or <c>default</c> for the client's own.</param>
        /// <returns>
        ///     The configured HTTP client.
        /// </returns>
        private HttpClient CreateConfiguredClient(TimeSpan clientTimeOut)
        {
            var client = _clientFactory.CreateClient(SoapHttpClientRegistration.ClientName);
            client.Timeout = clientTimeOut == Timeout.InfiniteTimeSpan || clientTimeOut > TimeSpan.Zero
                ? clientTimeOut
                : ClientTimeout;

            return client;
        }

        /// <summary>
        ///     Validates the shape of a SOAP request before it is built.
        /// </summary>
        /// <param name="soapRequest">The SOAP request.</param>
        /// <returns>
        ///     Success when the request is valid, or a failed result naming the missing or unsupported
        ///     field.
        /// </returns>
        private static IResult ValidateRequest(BaseSoapRequestDto soapRequest)
        {
            try
            {
                if (soapRequest.IsNull())
                {
                    return Result.Failure(MessageCodes.V_BEC_VR_001.GetDescription(),
                        Messages.GetValidationMessage(MessageCodes.V_BEC_VR_001));
                }

                if (new List<HttpMethod> { HttpMethod.Post, HttpMethod.Get }
                    .Any(x => x == soapRequest.Method).IsFalse())
                {
                    return Result.Failure(MessageCodes.V_BEC_VR_002.GetDescription(),
                        Messages.GetValidationMessage(MessageCodes.V_BEC_VR_002).TryFormatWith(soapRequest.Method));
                }

                if (soapRequest.Security.IsNotNull() && soapRequest.Security.Enabled && soapRequest.Method == HttpMethod.Get)
                {
                    return Result.Failure(MessageCodes.V_SEC_003.GetDescription(),
                        Messages.GetValidationMessage(MessageCodes.V_SEC_003));
                }

                if (soapRequest.SoapUri.IsNull())
                {
                    return Result.Failure(MessageCodes.V_BEC_VR_003.GetDescription(),
                        Messages.GetValidationMessage(MessageCodes.V_BEC_VR_003));
                }

                if (soapRequest.SoapProtocol.IsNull())
                {
                    return Result.Failure(MessageCodes.V_BEC_VR_004.GetDescription(),
                        Messages.GetValidationMessage(MessageCodes.V_BEC_VR_004));
                }

                if (soapRequest.SoapNameSpaceEnvelope.IsNull())
                {
                    return Result.Failure(MessageCodes.V_BEC_VR_005.GetDescription(),
                        Messages.GetValidationMessage(MessageCodes.V_BEC_VR_005));
                }

                if (soapRequest.MediaType.IsNullOrEmpty())
                {
                    return Result.Failure(MessageCodes.V_BEC_VR_006.GetDescription(),
                        Messages.GetValidationMessage(MessageCodes.V_BEC_VR_006));
                }

                if (soapRequest.BodyEncoding.IsNull())
                {
                    return Result.Failure(MessageCodes.V_BEC_VR_007.GetDescription(),
                        Messages.GetValidationMessage(MessageCodes.V_BEC_VR_007));
                }

                return Result.Success();
            }
            catch (Exception e)
            {
                return Result
                    .Failure(MessageCodes.ER_BEC_VR.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_BEC_VR))
                    .WithError(e);
            }
        }
    }
}