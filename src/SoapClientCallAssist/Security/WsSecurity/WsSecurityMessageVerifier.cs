// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="WsSecurityMessageVerifier.cs" company="RzR SOFT & TECH">
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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     Verifies a WS-Security signature on a raw SOAP response against a certificate the caller
    ///     trusts. An HMAC signature method is refused.
    /// </summary>
    public sealed class WsSecurityMessageVerifier : ISoapMessageVerifier
    {
        /// <inheritdoc />
        public IResult<SoapSignatureVerificationResult> Verify(string soapResponse, X509Certificate2 expectedCertificate)
            => Verify(soapResponse, expectedCertificate, null);

        /// <inheritdoc />
        public IResult<SoapSignatureVerificationResult> Verify(string soapResponse,
            X509Certificate2 expectedCertificate, SoapVerificationPolicyDto policy)
        {
            try
            {
                var effectivePolicy = policy ?? new SoapVerificationPolicyDto();

                if (expectedCertificate.IsNull())
                    return ValidationFailure(MessageCodes.V_SEC_004);

                if (soapResponse.IsNullOrEmpty() || soapResponse.Length > SoapContracts.MaxDocumentCharacters)
                    return ValidationFailure(MessageCodes.V_SEC_005, SoapContracts.MaxDocumentCharacters);

                using (var publicKey = ResolvePublicKey(expectedCertificate, out var keyError))
                {
                    if (keyError.IsNotNull())
                        return keyError;

                    if (publicKey.IsNull())
                        return ValidationFailure(MessageCodes.V_SEC_004);

                    return VerifyWithKey(soapResponse, publicKey, effectivePolicy);
                }
            }
            catch (Exception ex)
            {
                return ErrorFailure(MessageCodes.ER_SEC_VER)
                    .WithOptionalError(ex, "verifying the SOAP response signature");
            }
        }

        /// <summary>
        ///     Resolves the RSA public key of the expected certificate.
        /// </summary>
        /// <param name="certificate">The expected certificate.</param>
        /// <param name="error">[out] The failure to return, or null when the key was resolved.</param>
        /// <returns>
        ///     The RSA public key, or null when the certificate carries none.
        /// </returns>
        private static RSA ResolvePublicKey(X509Certificate2 certificate, out IResult<SoapSignatureVerificationResult> error)
        {
            try
            {
                error = null;

                return certificate.GetRSAPublicKey();
            }
            catch (Exception ex)
            {
                error = ErrorFailure(MessageCodes.ER_SEC_KEY)
                    .WithOptionalError(ex, "reading the expected certificate's public key");

                return null;
            }
        }

        /// <summary>
        ///     Parses the response through the depth-bounded loader, locates the one signature in the
        ///     Security header, gates its shape as RSA, checks it and only then applies the policy. 
        /// </summary>
        /// <param name="soapResponse">The raw response.</param>
        /// <param name="publicKey">The expected certificate's RSA public key.</param>
        /// <param name="policy">The conditions the signature must meet.</param>
        /// <returns>
        ///     The signature's coverage, or a failed result for a missing or invalid signature or when
        ///     checking throws.
        /// </returns>
        private static IResult<SoapSignatureVerificationResult> VerifyWithKey(string soapResponse,
            RSA publicKey, SoapVerificationPolicyDto policy)
        {
            var loaded = SoapXmlDocumentLoader.Load(soapResponse, true, MessageCodes.ER_SEC_DOM);
            if (loaded.IsSuccess.IsFalse())
                return loaded.Propagate<SoapSignatureVerificationResult>();

            var document = loaded.Response;

            var signatureElement = WsSecurityResponseInspection.LocateSignature(document);
            if (signatureElement.IsNull())
                return ValidationFailure(MessageCodes.V_SEC_006);

            var shape = WsSecurityResponseInspection.ValidateSignatureShape(signatureElement, SignatureKeyFamily.Rsa, policy.AllowSha1Algorithms);
            if (shape.IsSuccess.IsFalse())
                return shape.Propagate<SoapSignatureVerificationResult>();

            bool signatureValid;
            WsuSignedXml signedXml;

            try
            {
                signedXml = new WsuSignedXml(document) { Resolver = null };
                signedXml.LoadXml(signatureElement);
                signatureValid = signedXml.CheckSignature(publicKey);
            }
            catch (Exception ex)
            {
                return ErrorFailure(MessageCodes.ER_SEC_C14N)
                    .WithOptionalError(ex, "computing the WS-Security signature during verification");
            }

            if (signatureValid.IsFalse())
                return ErrorFailure(MessageCodes.ER_SEC_VER);

            var coverage = WsSecurityResponseInspection.ResolveCoverage(document, signedXml);

            var policyFailure = WsSecurityResponseInspection.ResolvePolicyFailure(coverage, policy);

            return policyFailure.IsNotNull()
                ? policyFailure.Propagate<SoapSignatureVerificationResult>()
                : Result<SoapSignatureVerificationResult>.Success(coverage);
        }

        /// <summary>
        ///     Builds a validation failure from a message code, formatting the message without
        ///     throwing.
        /// </summary>
        /// <param name="code">The validation message code.</param>
        /// <param name="args">The message arguments, if the message text carries placeholders.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapSignatureVerificationResult&gt;.
        /// </returns>
        private static IResult<SoapSignatureVerificationResult> ValidationFailure(MessageCodes code, params object[] args)
            => Result<SoapSignatureVerificationResult>.Failure(
                code.GetDescription(), Messages.GetValidationMessage(code).TryFormatWith(args));

        /// <summary>
        ///     Builds an error failure from a message code, ready for a captured exception to be
        ///     attached to it.
        /// </summary>
        /// <param name="code">The error message code.</param>
        /// <returns>
        ///     A failed Result&lt;SoapSignatureVerificationResult&gt;.
        /// </returns>
        private static Result<SoapSignatureVerificationResult> ErrorFailure(MessageCodes code)
            => Result<SoapSignatureVerificationResult>.Failure(code.GetDescription(), Messages.GetErrorMessage(code));
    }
}
