// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-12 09:30
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="SoapSecureConversationClient.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Text;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helpers;
using SoapClientCallAssist.Security.WsSecurity;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     Issues and cancels WS-SecureConversation sessions through a WS-Trust exchange. The client is
    ///     stateless and returns each session to the caller, who owns and disposes it; renewal is not
    ///     offered.
    /// </summary>
    public sealed class SoapSecureConversationClient
    {
        /// <summary>
        ///     The length of the client entropy, in bytes.
        /// </summary>
        private const int EntropyLength = 32;

        /// <summary>
        ///     The requested key size, in bits.
        /// </summary>
        private const int RequestedKeySizeBits = 256;

        /// <summary>
        ///     The length of the computed session key, in bytes.
        /// </summary>
        private const int SessionSecretLength = 32;

        /// <summary>
        ///     The injected HTTP client factory.
        /// </summary>
        private readonly IHttpClientFactory _clientFactory;

        /// <summary>
        ///     The response security service that decrypts and verifies the reply.
        /// </summary>
        private readonly ISoapResponseSecurity _responseSecurity;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapSecureConversationClient" /> class that
        ///     resolves its HTTP client from the library's shared fallback factory and verifies replies
        ///     with the default response security service.
        /// </summary>
        public SoapSecureConversationClient() 
            : this(SoapHttpClientRegistration.FallbackFactory, null) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapSecureConversationClient" /> class.
        /// </summary>
        /// <param name="clientFactory">The HTTP client factory the named client is resolved from.</param>
        /// <param name="responseSecurity">The response security, or null for the default.</param>
        public SoapSecureConversationClient(IHttpClientFactory clientFactory, ISoapResponseSecurity responseSecurity)
        {
            _clientFactory = clientFactory ?? SoapHttpClientRegistration.FallbackFactory;
            _responseSecurity = responseSecurity ?? new WsSecurityResponseSecurity();
        }

        /// <summary>
        ///     Issues a session from the service at <paramref name="endpoint" /> under the caller's
        ///     <paramref name="bootstrap" /> symmetric binding, turning on body encryption, reply
        ///     decryption and WS-Addressing for the exchange. The caller's options are not mutated.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="bootstrap">The bootstrap security options, which must select a symmetric binding.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="protocol">The SOAP protocol of the exchange; the default is SOAP 1.2.</param>
        /// <returns>
        ///     The established session, or a failed result for an unusable endpoint or bootstrap or
        ///     when the exchange fails.
        /// </returns>
        public async Task<IResult<SoapSecureConversationSession>> IssueAsync(Uri endpoint, SoapSecurityDto bootstrap, 
            CancellationToken cancellationToken = default, SoapProtocolType protocol = SoapProtocolType.SOAP_1_2)
        {
            if (endpoint.IsNull() || endpoint!.IsAbsoluteUri.IsFalse())
                return Refuse(MessageCodes.V_SEC_022);

            if (bootstrap.IsNull() || bootstrap!.SymmetricBinding.IsNull())
                return Refuse(MessageCodes.V_SEC_033);

            var version = bootstrap.SymmetricBinding!.Version;
            var clientEntropy = new byte[EntropyLength];

            try
            {
                using (var generator = RandomNumberGenerator.Create())
                    generator.GetBytes(clientEntropy);

                var body = BuildIssueBody(version, clientEntropy);
                var security = ExchangeSecurity(bootstrap, endpoint, WsTrustNames.SecureConversationIssueAction(version), version);

                var exchanged = await ExchangeAsync(endpoint, body, security, protocol, cancellationToken);
                if (exchanged.IsSuccess.IsFalse())
                    return exchanged.Propagate<SoapSecureConversationSession>();

                return ReadIssuedSession(exchanged.Response, version, clientEntropy);
            }
            catch (Exception ex)
            {
                return Result<SoapSecureConversationSession>
                    .Failure(MessageCodes.ER_SEC_SGN.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_SGN))
                    .WithOptionalError(ex, "issuing the secure conversation session");
            }
            finally
            {
                Array.Clear(clientEntropy, 0, clientEntropy.Length);
            }
        }

        /// <summary>
        ///     Cancels a session with a <c>wst:RequestSecurityToken</c> of type <c>Cancel</c> keyed by
        ///     the session itself. Whatever the outcome, the session is disposed and its secret zeroed
        ///     before this returns.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="session">The session to cancel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="protocol">The SOAP protocol of the exchange; the default is SOAP 1.2.</param>
        /// <returns>
        ///     Success when the service accepted the cancel, or a failed result for an unusable
        ///     endpoint or session or when the exchange fails.
        /// </returns>
        public async Task<IResult> CancelAsync(Uri endpoint, SoapSecureConversationSession session, 
            CancellationToken cancellationToken = default, SoapProtocolType protocol = SoapProtocolType.SOAP_1_2)
        {
            if (session.IsNull())
                return Result.Failure(MessageCodes.V_SEC_061.GetDescription(), Messages.GetValidationMessage(MessageCodes.V_SEC_061));

            try
            {
                if (endpoint.IsNull() || endpoint!.IsAbsoluteUri.IsFalse())
                    return Result.Failure(MessageCodes.V_SEC_022.GetDescription(), Messages.GetValidationMessage(MessageCodes.V_SEC_022));

                if (session.IsDisposed || session.IsExpired)
                    return Result.Failure(MessageCodes.V_SEC_063.GetDescription(), Messages.GetValidationMessage(MessageCodes.V_SEC_063));

                var version = session.Version;
                var body = BuildCancelBody(version, session.ContextIdentifier);
                var security = SessionSecurity(session, endpoint, WsTrustNames.SecureConversationCancelAction(version));

                var exchanged = await ExchangeAsync(endpoint, body, security, protocol, cancellationToken, false);

                return exchanged.ToBase();
            }
            catch (Exception ex)
            {
                return Result
                    .Failure(MessageCodes.ER_SEC_SGN.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_SGN))
                    .WithOptionalError(ex, "cancelling the secure conversation session");
            }
            finally
            {
                session.Dispose();
            }
        }

        /// <summary>
        ///     Builds the <c>wst:RequestSecurityToken</c> body of an issue request.
        /// </summary>
        /// <param name="version">The WS-SecureConversation version.</param>
        /// <param name="clientEntropy">The client entropy.</param>
        /// <returns>
        ///     The request body element.
        /// </returns>
        private static XElement BuildIssueBody(SoapSecureConversationVersionType version, byte[] clientEntropy)
        {
            XNamespace wst = WsTrustNames.Namespace(version);

            return new XElement(
                wst + WsTrustNames.RequestSecurityTokenLocalName,
                new XAttribute(XNamespace.Xmlns + WsTrustNames.Prefix, wst.NamespaceName),
                new XElement(wst + WsTrustNames.RequestTypeLocalName, WsTrustNames.IssueRequestType(version)),
                new XElement(wst + WsTrustNames.TokenTypeLocalName, WsSecureConversationNames.SecurityContextTokenValueType(version)),
                new XElement(wst + WsTrustNames.KeyTypeLocalName, WsTrustNames.SymmetricKeyType(version)),
                new XElement(wst + WsTrustNames.KeySizeLocalName, RequestedKeySizeBits.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new XElement(
                    wst + WsTrustNames.EntropyLocalName,
                    new XElement(
                        wst + WsTrustNames.BinarySecretLocalName,
                        new XAttribute("Type", WsTrustNames.Namespace(version) + WsTrustNames.NonceBinarySecretTypeSuffix),
                        Convert.ToBase64String(clientEntropy))),
                new XElement(wst + WsTrustNames.ComputedKeyAlgorithmLocalName, WsTrustNames.Psha1ComputedKey(version)));
        }

        /// <summary>
        ///     Builds the <c>wst:RequestSecurityToken</c> body of a cancel request, whose
        ///     <c>CancelTarget</c> references the session's security context token.
        /// </summary>
        /// <param name="version">The WS-SecureConversation version.</param>
        /// <param name="contextIdentifier">The session's context identifier.</param>
        /// <returns>
        ///     The request body element.
        /// </returns>
        private static XElement BuildCancelBody(SoapSecureConversationVersionType version, string contextIdentifier)
        {
            XNamespace wst = WsTrustNames.Namespace(version);
            XNamespace wsse = WsSecurityNames.WsseNamespace;

            return new XElement(
                wst + WsTrustNames.RequestSecurityTokenLocalName,
                new XAttribute(XNamespace.Xmlns + WsTrustNames.Prefix, wst.NamespaceName),
                new XElement(wst + WsTrustNames.RequestTypeLocalName, WsTrustNames.CancelRequestType(version)),
                new XElement(
                    wst + WsTrustNames.CancelTargetLocalName,
                    new XElement(
                        wsse + WsSecurityNames.SecurityTokenReferenceLocalName,
                        new XAttribute(XNamespace.Xmlns + WsSecurityNames.WssePrefix, WsSecurityNames.WsseNamespace),
                        new XElement(
                            wsse + WsSecurityNames.ReferenceLocalName,
                            new XAttribute(WsSecurityNames.UriAttributeName, contextIdentifier),
                            new XAttribute(WsSecurityNames.ValueTypeAttributeName, WsSecureConversationNames.SecurityContextTokenValueType(version))))));
        }

        /// <summary>
        ///     Builds the exchange security of an issue request from a copy of the caller's bootstrap,
        ///     with body encryption, reply decryption and WS-Addressing to the endpoint with the WS-
        ///     Trust action.
        /// </summary>
        /// <param name="bootstrap">The caller's bootstrap options.</param>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="action">The WS-Trust action.</param>
        /// <param name="version">The WS-SecureConversation version.</param>
        /// <returns>
        ///     The exchange security options.
        /// </returns>
        private static SoapSecurityDto ExchangeSecurity(SoapSecurityDto bootstrap, Uri endpoint, string action,
            SoapSecureConversationVersionType version)
            => new()
            {
                Enabled = true,
                SigningCertificate = bootstrap.SigningCertificate,
                UsernameToken = bootstrap.UsernameToken,
                SymmetricBinding = bootstrap.SymmetricBinding,
                MustUnderstand = bootstrap.MustUnderstand,
                ResponseVerificationPolicy = bootstrap.ResponseVerificationPolicy,
                Encryption = new SoapEncryptionDto { EncryptBody = true, EncryptSignature = true },
                ResponseSecurity = new SoapResponseSecurityDto
                {
                    AllowDecryption = true, 
                    RequireSignatureConfirmation = false
                },
                Addressing = Addressing(bootstrap.Addressing, endpoint, action)
            };

        /// <summary>
        ///     Builds the exchange security of a request keyed by an established session, addressing it
        ///     to the endpoint with the WS-Trust action.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="action">The WS-Trust action.</param>
        /// <returns>
        ///     The exchange security options.
        /// </returns>
        private static SoapSecurityDto SessionSecurity(SoapSecureConversationSession session, Uri endpoint, string action)
            => new()
            {
                Enabled = true,
                SecureConversation = new SoapSecureConversationDto { Session = session },
                ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true, RequireSignatureConfirmation = false },
                Addressing = Addressing(null, endpoint, action)
            };

        /// <summary>
        ///     Builds the addressing options of an exchange, keeping the caller's version when one was
        ///     supplied.
        /// </summary>
        /// <param name="template">The caller's addressing options, or null.</param>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="action">The WS-Trust action.</param>
        /// <returns>
        ///     The addressing options.
        /// </returns>
        private static SoapAddressingDto Addressing(SoapAddressingDto template, Uri endpoint, string action)
            => new()
            {
                To = endpoint,
                Action = action,
                IncludeMessageId = true,
                IncludeReplyTo = true,
                Version = template?.Version ?? SoapAddressingVersionType.WsAddressing10
            };

        /// <summary>
        ///     Signs the body, posts it through the named HTTP client and, when asked, decrypts and
        ///     verifies the reply, which consumes the single-use key material.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="body">The request body.</param>
        /// <param name="security">The exchange security options.</param>
        /// <param name="protocol">The SOAP protocol.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="decryptResponse">(Optional) True to decrypt response.</param>
        /// <returns>
        ///     The reply envelope, decrypted and verified when asked, or the signing, transport or
        ///     verification failure.
        /// </returns>
        private async Task<IResult<string>> ExchangeAsync(Uri endpoint, XElement body, SoapSecurityDto security, SoapProtocolType protocol,
            CancellationToken cancellationToken, bool decryptResponse = true)
        {
            var soap = SoapNamespace(protocol);
            var envelope = new XElement(
                soap + SoapContracts.EnvelopeLocalName,
                new XAttribute(XNamespace.Xmlns + "soap", soap.NamespaceName),
                new XElement(soap + WsSecurityNames.HeaderLocalName),
                new XElement(soap + SoapContracts.BodyLocalName, body));

            var action = security.Addressing.Action;

            var signed = new WsSecurityMessageSigner().SignCore(envelope, security, action, endpoint, out var material);
            if (signed.IsSuccess.IsFalse())
            {
                material?.Dispose();

                return signed;
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                var mediaType = protocol == SoapProtocolType.SOAP_1_2 ? SoapMediaType.Soap12.GetDescription() : SoapMediaType.Soap11.GetDescription();
                var content = new StringContent(signed.Response, Encoding.UTF8, mediaType);

                if (protocol == SoapProtocolType.SOAP_1_2)
                    content.Headers.ContentType!.Parameters.Add(new NameValueHeaderValue("action", "\"" + action + "\""));
                else
                    content.Headers.Add("SOAPAction", "\"" + action + "\"");

                request.Content = content;

                if (material.IsNotNull())
                    request.Properties[SoapClientEndpointExtensions.RequestSecurityStateKey] = material;

                var client = _clientFactory.CreateClient(SoapHttpClientRegistration.ClientName);

                using (var response = await client.SendAsync(request, cancellationToken))
                {
                    var classified = SoapHttpStatusClassifier.Classify(response);
                    var body2 = response.Content.IsNull() ? null : await response.Content.ReadAsStringAsync();

                    if (classified.IsSuccess.IsFalse())
                    {
                        material?.Dispose();

                        return classified.Propagate<string>();
                    }

                    if (decryptResponse)
                        return _responseSecurity.DecryptAndVerify(request, body2);

                    material?.Dispose();

                    return Result<string>.Success(body2);
                }
            }
        }

        /// <summary>
        ///     Reads the decrypted, verified issue response into a session through
        ///     <see cref="SecureConversationIssueReader" />.
        /// </summary>
        /// <param name="responseEnvelope">The decrypted, verified response envelope.</param>
        /// <param name="version">The WS-SecureConversation version.</param>
        /// <param name="clientEntropy">The client entropy.</param>
        /// <returns>
        ///     The session, or the reader's refusal.
        /// </returns>
        private static IResult<SoapSecureConversationSession> ReadIssuedSession(string responseEnvelope, 
            SoapSecureConversationVersionType version, byte[] clientEntropy)
            => SecureConversationIssueReader.Read(responseEnvelope, version, clientEntropy, SessionSecretLength, RequestedKeySizeBits);

        /// <summary>
        ///     Resolves the SOAP envelope namespace of a protocol.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <returns>
        ///     The namespace.
        /// </returns>
        private static XNamespace SoapNamespace(SoapProtocolType protocol)
            => protocol == SoapProtocolType.SOAP_1_2
                ? "http://www.w3.org/2003/05/soap-envelope"
                : "http://schemas.xmlsoap.org/soap/envelope/";

        /// <summary>
        ///     Builds the refusal of an issue under a validation code.
        /// </summary>
        /// <param name="code">The validation message code.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapSecureConversationSession&gt;.
        /// </returns>
        private static IResult<SoapSecureConversationSession> Refuse(MessageCodes code)
            => Result<SoapSecureConversationSession>.Failure(code.GetDescription(), Messages.GetValidationMessage(code));
    }
}
