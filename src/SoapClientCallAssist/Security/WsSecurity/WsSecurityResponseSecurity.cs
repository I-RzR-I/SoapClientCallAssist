// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="WsSecurityResponseSecurity.cs" company="RzR SOFT & TECH">
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
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     The library's <see cref="ISoapResponseSecurity" />. It checks a response by the mode
    ///     snapshotted on the sent request, by certificate or by derived keys, and consumes that
    ///     single-use snapshot.
    /// </summary>
    public sealed class WsSecurityResponseSecurity : ISoapResponseSecurity
    {
        /// <summary>
        ///     The verifier the certificate-based checks run through when the options name none, or
        ///     null to fall back to the library's own.
        /// </summary>
        private readonly ISoapMessageVerifier _verifier;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WsSecurityResponseSecurity" /> class with
        ///     the library's own verifier.
        /// </summary>
        public WsSecurityResponseSecurity() : this(null) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WsSecurityResponseSecurity" /> class with
        ///     the verifier certificate-based checks run through.
        /// </summary>
        /// <param name="verifier">The fallback verifier, or null for the library's own.</param>
        public WsSecurityResponseSecurity(ISoapMessageVerifier verifier) => _verifier = verifier;

        /// <inheritdoc />
        public IResult<SoapSignatureVerificationResult> Verify(HttpRequestMessage sentRequest, string soapResponse)
        {
            var consumed = Consume(sentRequest, out var refused);
            if (refused.IsNotNull())
                return refused.Propagate<SoapSignatureVerificationResult>();

            using (consumed)
            {
                try
                {
                    if (consumed.Mode == SoapSecurityModeType.SecureConversation && consumed.SessionUnavailable)
                        return ValidationFailure(MessageCodes.V_SEC_073);

                    if (IsSymmetricFamily(consumed.Mode))
                        return VerifySymmetric(consumed, soapResponse);

                    return VerifyAgainstCertificate(consumed, soapResponse);
                }
                catch (Exception ex)
                {
                    return VerifierFailure(ex);
                }
            }
        }

        /// <inheritdoc />
        public IResult<SoapSignatureVerificationResult> Verify(
            string soapResponse, X509Certificate2 expectedCertificate, SoapVerificationPolicyDto policy)
            => RunVerifier(DefaultVerifier(), soapResponse, expectedCertificate, policy);

        /// <inheritdoc />
        public IResult<SoapSignatureVerificationResult> Verify(string soapResponse, SoapSecurityDto security)
        {
            if (security.IsNull())
                return ValidationFailure(MessageCodes.V_SEC_013);

            if (SoapSecurityPlanner.IsSymmetricFamily(security))
                return ValidationFailure(MessageCodes.V_SEC_032);

            if (X509KeyMatch.SharesPublicKey(security!.SigningCertificate, security.ExpectedResponseCertificate))
                return ValidationFailure(MessageCodes.V_SEC_015);

            return RunVerifier(
                security.ResponseVerifier ?? DefaultVerifier(),
                soapResponse,
                security.ExpectedResponseCertificate,
                security.ResponseVerificationPolicy);
        }

        /// <inheritdoc />
        public IResult<string> DecryptAndVerify(HttpRequestMessage sentRequest, string soapResponse)
        {
            var consumed = Consume(sentRequest, out var refused);
            if (refused.IsNotNull())
                return refused.Propagate<string>();

            using (consumed)
            {
                if (consumed.Mode == SoapSecurityModeType.SecureConversation && consumed.SessionUnavailable)
                    return ValidationFailure(MessageCodes.V_SEC_073).Propagate<string>();

                if (IsSymmetricFamily(consumed.Mode).IsFalse())
                    return ValidationFailure(MessageCodes.V_SEC_053).Propagate<string>();

                if (consumed.IsBindable.IsFalse())
                    return ValidationFailure(MessageCodes.V_SEC_024).Propagate<string>();

                try
                {
                    return WsSecurityResponseDecryptor.DecryptAndVerify(consumed, soapResponse);
                }
                catch (Exception)
                {
                    return Result<string>.Failure(MessageCodes.ER_SEC_DEC.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_DEC));
                }
            }
        }

        /// <inheritdoc />
        public IResult<string> Decrypt(HttpRequestMessage sentRequest, string soapResponse)
            => DecryptAndVerify(sentRequest, soapResponse);

        /// <summary>
        ///     Reads and consumes the single-use key material of a sent request.
        /// </summary>
        /// <param name="sentRequest">The sent request, or null.</param>
        /// <param name="refused">[out] The refusal, or null when the material was consumed.</param>
        /// <returns>
        ///     The one-time view, or null with a refusal when the request carries no key material or it
        ///     was already consumed.
        /// </returns>
        private static ConsumedKeyMaterial Consume(HttpRequestMessage sentRequest, out IResult refused)
        {
            refused = null;

            RequestKeyMaterial material = null;

            if (sentRequest.IsNotNull()
                && sentRequest!.Properties.TryGetValue(SoapClientEndpointExtensions.RequestSecurityStateKey, out var stored))
                material = stored as RequestKeyMaterial;

            if (material.IsNull())
            {
                refused = WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_030);

                return null;
            }

            if (material!.TryConsume(out var consumed))
                return consumed;

            refused = WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_031);

            return null;
        }

        /// <summary>
        ///     Checks a response in clear to a symmetric request through the symmetric verifier, after
        ///     refusing an unbindable request, a reflected nonce and encrypted content.
        /// </summary>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <param name="soapResponse">The raw response.</param>
        /// <returns>
        ///     The verifier's verdict, or a failed result for an unbindable request or encrypted content.
        /// </returns>
        private static IResult<SoapSignatureVerificationResult> VerifySymmetric(ConsumedKeyMaterial consumed, string soapResponse)
        {
            if (consumed.IsBindable.IsFalse())
                return ValidationFailure(MessageCodes.V_SEC_024);

            if (soapResponse.IsNullOrEmpty() || soapResponse.Length > SoapContracts.MaxDocumentCharacters)
                return WsSecuritySymmetricResponseVerifier.Verify(consumed, soapResponse);

            var loaded = SoapXmlDocumentLoader.Load(soapResponse, true, MessageCodes.ER_SEC_DOM);
            if (loaded.IsSuccess.IsFalse())
                return loaded.Propagate<SoapSignatureVerificationResult>();

            var reflected = WsSecurityResponseBinding.RefuseReflectedNonce(loaded.Response, consumed.Nonces);
            if (reflected.IsSuccess.IsFalse())
                return reflected.Propagate<SoapSignatureVerificationResult>();

            return WsSecurityResponseDecryptor.CarriesEncryptedContent(loaded.Response)
                ? ValidationFailure(MessageCodes.V_SEC_058)
                : WsSecuritySymmetricResponseVerifier.Verify(consumed, loaded.Response);
        }

        /// <summary>
        ///     Checks a response to an asymmetric or token-only request against the expected certificate
        ///     under the snapshotted policy, gating the key family first and binding the response to the
        ///     request afterwards.
        /// </summary>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <param name="soapResponse">The raw response.</param>
        /// <returns>
        ///     The verifier's verdict once bound, or a failed result without an expected certificate,
        ///     for an unbindable request or for an empty or oversized response.
        /// </returns>
        private IResult<SoapSignatureVerificationResult> VerifyAgainstCertificate(ConsumedKeyMaterial consumed, string soapResponse)
        {
            if (consumed.ExpectedCertificate.IsNull())
                return ValidationFailure(MessageCodes.V_SEC_004);

            if (consumed.IsBindable.IsFalse())
                return ValidationFailure(MessageCodes.V_SEC_024);

            XmlDocument document = null;

            if (soapResponse.IsNullOrEmpty().IsFalse() && soapResponse.Length <= SoapContracts.MaxDocumentCharacters)
            {
                var loaded = SoapXmlDocumentLoader.Load(soapResponse, true, MessageCodes.ER_SEC_DOM);
                if (loaded.IsSuccess.IsFalse())
                    return loaded.Propagate<SoapSignatureVerificationResult>();

                document = loaded.Response;

                var signatureElement = WsSecurityResponseInspection.LocateSignature(document);
                if (signatureElement.IsNotNull())
                {
                    var shape = WsSecurityResponseInspection.ValidateSignatureShape(
                        signatureElement, consumed.KeyFamily ?? SignatureKeyFamily.Rsa, consumed.Policy.AllowSha1Algorithms);
                    if (shape.IsSuccess.IsFalse())
                        return shape.Propagate<SoapSignatureVerificationResult>();
                }
            }

            IResult<SoapSignatureVerificationResult> verification;

            using (var expected = new X509Certificate2(consumed.ExpectedCertificate))
                verification = RunVerifier(DefaultVerifier(), soapResponse, expected, consumed.Policy);

            if (verification.IsSuccess.IsFalse())
                return verification;

            if (document.IsNull())
                return Result<SoapSignatureVerificationResult>.Failure(
                    MessageCodes.V_SEC_005.GetDescription(),
                    Messages.GetValidationMessage(MessageCodes.V_SEC_005).TryFormatWith(SoapContracts.MaxDocumentCharacters));

            var bound = BindToRequest(consumed, document);

            return bound.IsSuccess.IsFalse() ? bound.Propagate<SoapSignatureVerificationResult>() : verification;
        }

        /// <summary>
        ///     Binds a verified response to the request, refusing a reflected signature value or nonce
        ///     and requiring the signed <c>wsa:RelatesTo</c> and signature confirmation the request's
        ///     snapshot demands.
        /// </summary>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <param name="document">The parsed response, loaded with whitespace preserved.</param>
        /// <returns>
        ///     Success when bound, a failed result without a signature, or the binding check's own
        ///     refusal.
        /// </returns>
        private static IResult BindToRequest(ConsumedKeyMaterial consumed, XmlDocument document)
        {
            var signatureElement = WsSecurityResponseInspection.LocateSignature(document);
            if (signatureElement.IsNull())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_006);

            var signedXml = new WsuSignedXml(document) { Resolver = null };
            signedXml.LoadXml(signatureElement);

            var reflected = WsSecurityResponseBinding.RefuseReflectedSignatureValue(signedXml, consumed.SignatureValue);
            if (reflected.IsSuccess.IsFalse())
                return reflected;

            var nonce = WsSecurityResponseBinding.RefuseReflectedNonce(document, consumed.Nonces);
            if (nonce.IsSuccess.IsFalse())
                return nonce;

            if (consumed.MessageId.IsNotNull())
            {
                var relatesTo = WsSecurityResponseBinding.RequireRelatesTo(document, signedXml, consumed.MessageId);
                if (relatesTo.IsSuccess.IsFalse())
                    return relatesTo;
            }

            return consumed.RequireSignatureConfirmation
                ? WsSecurityResponseBinding.RequireSignatureConfirmation(document, signedXml, consumed.SignatureValue)
                : Result.Success();
        }

        /// <summary>
        ///     Runs a verifier, turning an exception it raises or a null result it returns into a
        ///     failure.
        /// </summary>
        /// <param name="verifier">The verifier to run.</param>
        /// <param name="soapResponse">The raw SOAP response.</param>
        /// <param name="expectedCertificate">
        ///     The certificate the response is expected to be signed with.
        /// </param>
        /// <param name="policy">
        ///     The conditions the signature must meet, or null for the strict defaults.
        /// </param>
        /// <returns>
        ///     The verifier's verdict, or a failed result when it threw or returned nothing.
        /// </returns>
        private static IResult<SoapSignatureVerificationResult> RunVerifier(ISoapMessageVerifier verifier, string soapResponse,
            X509Certificate2 expectedCertificate, SoapVerificationPolicyDto policy)
        {
            try
            {
                var verification = verifier.Verify(soapResponse, expectedCertificate, policy);

                return verification.IsNull() ? VerifierFailure(null) : verification;
            }
            catch (Exception ex)
            {
                return VerifierFailure(ex);
            }
        }

        /// <summary>
        ///     Resolves the fallback verifier of a call whose security options name none.
        /// </summary>
        /// <returns>
        ///     The verifier this service was constructed with, or the library's own.
        /// </returns>
        private ISoapMessageVerifier DefaultVerifier() => _verifier ?? new WsSecurityMessageVerifier();

        /// <summary>
        ///     Determines whether a mode is keyed by a shared secret.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>
        ///     True for the symmetric binding and the secure conversation.
        /// </returns>
        private static bool IsSymmetricFamily(SoapSecurityModeType mode)
            => mode == SoapSecurityModeType.SymmetricEncryptedKey || mode == SoapSecurityModeType.SecureConversation;

        /// <summary>
        ///     Builds the failure returned when a verification request is refused before, or bound
        ///     after, any verifier runs.
        /// </summary>
        /// <param name="code">The validation message code.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapSignatureVerificationResult&gt;.
        /// </returns>
        private static IResult<SoapSignatureVerificationResult> ValidationFailure(MessageCodes code)
            => Result<SoapSignatureVerificationResult>.Failure(code.GetDescription(), Messages.GetValidationMessage(code));

        /// <summary>
        ///     Builds the failure returned when a verifier threw or returned nothing.
        /// </summary>
        /// <param name="exception">The captured exception, or null when the verifier returned nothing.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapSignatureVerificationResult&gt;.
        /// </returns>
        private static IResult<SoapSignatureVerificationResult> VerifierFailure(Exception exception)
            => Result<SoapSignatureVerificationResult>
                .Failure(MessageCodes.ER_BEC_VRS.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_BEC_VRS))
                .WithOptionalError(exception, "verifying the WS-Security signature on the SOAP response");
    }
}
