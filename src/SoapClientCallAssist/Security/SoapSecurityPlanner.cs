// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 02:30
//  ***********************************************************************
//  <copyright file="SoapSecurityPlanner.cs" company="RzR SOFT & TECH">
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
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using System;
using System.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;
using SoapClientCallAssist.Security.WsSecurity;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     Resolves the one WS-Security mode a set of options describes and validates every option
    ///     against it before anything is built. Options naming no mode, two, or an impossible one
    ///     fail with a specific code.
    /// </summary>
    internal static class SoapSecurityPlanner
    {
        /// <summary>
        ///     The shortest derived signature key accepted, in bytes.
        /// </summary>
        private const int MinSignatureKeyLength = 16;

        /// <summary>
        ///     The longest derived signature key accepted, in bytes.
        /// </summary>
        private const int MaxSignatureKeyLength = 64;

        /// <summary>
        ///     The skew applied to the service certificate's validity window when the caller's policy
        ///     names none.
        /// </summary>
        private static readonly TimeSpan DefaultRecipientClockSkew = TimeSpan.FromMinutes(5);

        /// <summary>
        ///     Resolves and validates the plan of a request.
        /// </summary>
        /// <param name="options">The security options. Null, or disabled, resolves no plan.</param>
        /// <param name="transportAction">The request's own <c>SOAPAction</c>, or null.</param>
        /// <param name="endpoint">The endpoint the request is built for, or null when unknown.</param>
        /// <returns>
        ///     Success carrying the plan, or the failure of the first rule that does not hold.
        /// </returns>
        internal static IResult<SecurityHeaderPlan> Plan(SoapSecurityDto options, string transportAction, Uri endpoint)
        {
            if (options.IsNull() || options!.Enabled.IsFalse())
                return Refuse(MessageCodes.V_SEC_002);

            var mode = ResolveMode(options, out var modeFailure);
            if (modeFailure.IsNotNull())
                return modeFailure;

            var symmetricFamily = mode == SoapSecurityModeType.SymmetricEncryptedKey || mode == SoapSecurityModeType.SecureConversation;

            var refused = symmetricFamily ? ValidateSymmetricFamily(options, mode) : ValidateAsymmetricFamily(options);
            refused = refused ?? ValidateEncryption(options, symmetricFamily);
            refused = refused ?? ValidateResponseSecurity(options, symmetricFamily);
            refused = refused ?? ValidateSamlToken(options, symmetricFamily, endpoint);
            refused = refused ?? ValidateUsernameToken(options, mode, endpoint);
            if (refused.IsNotNull())
                return refused;

            string action = null;
            Uri to = null;

            if (options.Addressing.IsNotNull())
            {
                var addressing = ResolveAddressing(options.Addressing, transportAction, endpoint, out action, out to);
                if (addressing.IsNotNull())
                    return addressing;
            }

            var requireConfirmation = options.ResponseSecurity?.RequireSignatureConfirmation ?? symmetricFamily;
            var encryptUsernameToken = symmetricFamily && options.UsernameToken.IsNotNull();

            return Result<SecurityHeaderPlan>.Success(new SecurityHeaderPlan(
                mode, KeyFamilyOf(mode), options, action, to, requireConfirmation, encryptUsernameToken));
        }

        /// <summary>
        ///     Determines whether options select a mode keyed by a shared secret.
        /// </summary>
        /// <param name="options">The security options, or null.</param>
        /// <returns>
        ///     True when a symmetric binding or a secure conversation is configured.
        /// </returns>
        internal static bool IsSymmetricFamily(SoapSecurityDto options)
            => options.IsNotNull() && (options!.SymmetricBinding.IsNotNull() || options.SecureConversation.IsNotNull());

        /// <summary>
        ///     Resolves the one mode the options select.
        /// </summary>
        /// <param name="options">The security options.</param>
        /// <param name="failure">[out] The refusal, or null when a mode was resolved.</param>
        /// <returns>
        ///     The mode; meaningless when <paramref name="failure" /> is set.
        /// </returns>
        private static SoapSecurityModeType ResolveMode(SoapSecurityDto options, out IResult<SecurityHeaderPlan> failure)
        {
            failure = null;

            if (options.SymmetricBinding.IsNotNull() && options.SecureConversation.IsNotNull())
            {
                failure = Refuse(MessageCodes.V_SEC_090);

                return default;
            }

            if (options.SymmetricBinding.IsNotNull())
                return SoapSecurityModeType.SymmetricEncryptedKey;

            if (options.SecureConversation.IsNotNull())
                return SoapSecurityModeType.SecureConversation;

            if (options.SigningCertificate.IsNotNull())
                return SoapSecurityModeType.AsymmetricX509;

            if (options.UsernameToken.IsNotNull() || options.SamlToken.IsNotNull())
                return SoapSecurityModeType.TokenOnly;

            failure = Refuse(MessageCodes.V_SEC_002);

            return default;
        }

        /// <summary>
        ///     Resolves the key family of the primary signature a mode emits.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>
        ///     The key family, or null for a token-only header.
        /// </returns>
        private static SignatureKeyFamily? KeyFamilyOf(SoapSecurityModeType mode)
        {
            switch (mode)
            {
                case SoapSecurityModeType.AsymmetricX509:
                    return SignatureKeyFamily.Rsa;
                case SoapSecurityModeType.SymmetricEncryptedKey:
                case SoapSecurityModeType.SecureConversation:
                    return SignatureKeyFamily.Hmac;
                default:
                    return null;
            }
        }

        /// <summary>
        ///     Validates the rules shared by the symmetric binding and the secure conversation, plus the
        ///     ones specific to each.
        /// </summary>
        /// <param name="options">The security options.</param>
        /// <param name="mode">The resolved mode.</param>
        /// <returns>
        ///     The refusal, or null when every rule holds.
        /// </returns>
        private static IResult<SecurityHeaderPlan> ValidateSymmetricFamily(SoapSecurityDto options, SoapSecurityModeType mode)
        {
            if (options.Addressing.IsNull())
                return Refuse(MessageCodes.V_SEC_033);

            if (options.Signer.IsNotNull() || options.ResponseVerifier.IsNotNull())
                return Refuse(MessageCodes.V_SEC_035);

            if (options.ExpectedResponseCertificate.IsNotNull())
                return Refuse(MessageCodes.V_SEC_036);

            if (options.UsernameToken.IsNotNull() && options.Encryption.IsNotNull() && options.Encryption!.EncryptUsernameToken.IsFalse())
                return Refuse(MessageCodes.V_SEC_038);

            return mode == SoapSecurityModeType.SymmetricEncryptedKey
                ? ValidateSymmetricBinding(options)
                : ValidateSecureConversation(options.SecureConversation);
        }

        /// <summary>
        ///     Validates the rule shared by the asymmetric binding and a token-only header, refusing an
        ///     expected response certificate that carries the signing certificate's own key.
        /// </summary>
        /// <param name="options">The security options.</param>
        /// <returns>
        ///     The refusal when the two certificates share a key, otherwise null.
        /// </returns>
        private static IResult<SecurityHeaderPlan> ValidateAsymmetricFamily(SoapSecurityDto options)
            => X509KeyMatch.SharesPublicKey(options.SigningCertificate, options.ExpectedResponseCertificate)
                ? Refuse(MessageCodes.V_SEC_015)
                : null;

        /// <summary>
        ///     Validates the symmetric binding's own options, checking the service certificate, the key
        ///     lengths, the key identifier, the signature algorithm, and that the encryption key fits
        ///     the data algorithm in effect.
        /// </summary>
        /// <param name="options">The security options.</param>
        /// <returns>
        ///     The refusal of the first check that does not hold, otherwise null.
        /// </returns>
        private static IResult<SecurityHeaderPlan> ValidateSymmetricBinding(SoapSecurityDto options)
        {
            var binding = options.SymmetricBinding;

            if (binding.ServiceCertificate.IsNull())
                return Refuse(MessageCodes.V_SEC_039);

            if (X509KeyMatch.SharesPublicKey(binding.ServiceCertificate, options.SigningCertificate))
                return Refuse(MessageCodes.V_SEC_037);

            var signatureLengthValid = binding.SignatureKeyLength >= MinSignatureKeyLength && binding.SignatureKeyLength <= MaxSignatureKeyLength;
            var encryptionLengthValid = binding.EncryptionKeyLength == 16 || binding.EncryptionKeyLength == 24 || binding.EncryptionKeyLength == 32;

            if (signatureLengthValid.IsFalse() || encryptionLengthValid.IsFalse())
                return Refuse(MessageCodes.V_SEC_043);

            if (binding.ServiceKeyIdentifier != SoapServiceKeyIdentifierType.ThumbprintSha1)
                return Refuse(MessageCodes.V_SEC_045);

            if (binding.SignatureAlgorithm == SoapSymmetricSignatureAlgorithmType.HmacSha1 && AllowsSha1(options).IsFalse())
                return Refuse(MessageCodes.V_SEC_044);

            var dataAlgorithm = options.Encryption?.DataAlgorithm ?? SoapDataEncryptionAlgorithmType.Aes256Cbc;

            return binding.EncryptionKeyLength == WsSecurityEncryptor.KeyLengthOf(dataAlgorithm) ? null : Refuse(MessageCodes.V_SEC_056);
        }

        /// <summary>
        ///     Determines whether the caller opted in to SHA-1 through the response verification policy,
        ///     the one place the library accepts that opt-in.
        /// </summary>
        /// <param name="options">The security options.</param>
        /// <returns>
        ///     True when SHA-1 is allowed.
        /// </returns>
        private static bool AllowsSha1(SoapSecurityDto options)
            => options.ResponseVerificationPolicy.IsNotNull() && options.ResponseVerificationPolicy!.AllowSha1Algorithms;

        /// <summary>
        ///     Validates the secure conversation's session.
        /// </summary>
        /// <param name="conversation">The secure conversation options.</param>
        /// <returns>
        ///     The refusal, or null when the session can key a request.
        /// </returns>
        private static IResult<SecurityHeaderPlan> ValidateSecureConversation(SoapSecureConversationDto conversation)
        {
            var session = conversation.Session;

            if (session.IsNull())
                return Refuse(MessageCodes.V_SEC_061);

            if (session!.IsExpired)
                return Refuse(MessageCodes.V_SEC_062);

            return session.HasSecret ? null : Refuse(MessageCodes.V_SEC_063);
        }

        /// <summary>
        ///     Validates the response security options against the mode, allowing decryption only on a
        ///     mode that carries a decryption key and requiring a positive plaintext cap.
        /// </summary>
        /// <param name="options">The security options.</param>
        /// <param name="symmetricFamily">True when the mode carries a decryption key.</param>
        /// <returns>
        ///     The refusal when decryption is allowed without a key or the plaintext cap is not positive,
        ///     otherwise null.
        /// </returns>
        private static IResult<SecurityHeaderPlan> ValidateResponseSecurity(SoapSecurityDto options, bool symmetricFamily)
        {
            var response = options.ResponseSecurity;
            if (response.IsNull())
                return null;

            if (response!.AllowDecryption && symmetricFamily.IsFalse())
                return Refuse(MessageCodes.V_SEC_053);

            if (response.MaxPlaintextBytes.HasValue && response.MaxPlaintextBytes.Value <= 0)
            {
                return Result<SecurityHeaderPlan>.Failure(
                    MessageCodes.V_SEC_054.GetDescription(),
                    Messages.GetValidationMessage(MessageCodes.V_SEC_054).TryFormatWith(response.MaxPlaintextBytes.Value));
            }

            return null;
        }

        /// <summary>
        ///     Validates the encryption options against the mode, requiring an encryption key, a
        ///     decryptable reply for an encrypted Body, RSA-OAEP key wrapping, and a service certificate
        ///     fit to encrypt for under the policy's clock skew.
        /// </summary>
        /// <param name="options">The security options.</param>
        /// <param name="symmetricFamily">True when the mode carries an encryption key.</param>
        /// <returns>
        ///     The refusal of the first check that does not hold, otherwise null.
        /// </returns>
        private static IResult<SecurityHeaderPlan> ValidateEncryption(SoapSecurityDto options, bool symmetricFamily)
        {
            var encryption = options.Encryption;

            if (encryption.IsNull())
                return null;

            if (symmetricFamily.IsFalse())
                return Refuse(MessageCodes.V_SEC_051);

            var allowDecryption = options.ResponseSecurity.IsNotNull() && options.ResponseSecurity!.AllowDecryption;

            if (encryption!.EncryptBody && allowDecryption.IsFalse())
                return Refuse(MessageCodes.V_SEC_052);

            if (encryption.KeyWrap != SoapKeyWrapAlgorithmType.RsaOaepMgf1pSha1)
                return Refuse(MessageCodes.V_SEC_055);

            var recipient = options.SymmetricBinding?.ServiceCertificate;

            if (encryption.EncryptBody.IsFalse() || recipient.IsNull())
                return null;

            var fit = WsSecurityEncryptor.ValidateRecipientCertificate(
                recipient, options.SigningCertificate, options.ResponseVerificationPolicy?.ClockSkew ?? DefaultRecipientClockSkew);

            return fit.IsSuccess ? null : Refuse(MessageCodes.V_SEC_057);
        }

        /// <summary>
        ///     Validates the SAML token options, refusing a holder-of-key assertion with no signing
        ///     certificate and a bearer assertion over a known non-<c>https</c> endpoint unless the
        ///     caller opted in.
        /// </summary>
        /// <param name="options">The security options.</param>
        /// <param name="symmetricFamily">True when the mode carries an encryption key.</param>
        /// <param name="endpoint">The endpoint the request is built for, or null when unknown.</param>
        /// <returns>
        ///     The refusal of the first check that does not hold, otherwise null.
        /// </returns>
        private static IResult<SecurityHeaderPlan> ValidateSamlToken(SoapSecurityDto options, bool symmetricFamily, Uri endpoint)
        {
            var saml = options.SamlToken;

            if (saml.IsNull())
                return null;

            if (saml!.Assertion.IsNull())
                return Refuse(MessageCodes.V_SEC_081);

            if (symmetricFamily)
                return Refuse(MessageCodes.V_SEC_088);

            if (saml.Confirmation == SoapSamlConfirmationType.HolderOfKey)
                return options.SigningCertificate.IsNull() ? Refuse(MessageCodes.V_SEC_082) : null;

            return IsInsecureTransport(endpoint) && saml.AllowBearerOverInsecureTransport.IsFalse()
                ? Refuse(MessageCodes.V_SEC_083)
                : null;
        }

        /// <summary>
        ///     Determines whether an endpoint is known to be reached over a transport that does not
        ///     protect the message in transit.
        /// </summary>
        /// <param name="endpoint">The endpoint, or null when unknown.</param>
        /// <returns>
        ///     True for a known absolute endpoint whose scheme is not <c>https</c>.
        /// </returns>
        private static bool IsInsecureTransport(Uri endpoint)
            => endpoint.IsNotNull()
               && endpoint!.IsAbsoluteUri
               && string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase).IsFalse();

        /// <summary>
        ///     Validates the username token options, checking the values for XML-representable
        ///     characters and refusing a text password sent in clear over a known non-<c>https</c>
        ///     endpoint unless the caller opted in.
        /// </summary>
        /// <param name="options">The security options.</param>
        /// <param name="mode">The resolved mode.</param>
        /// <param name="endpoint">The endpoint the request is built for, or null when unknown.</param>
        /// <returns>
        ///     The refusal of the first check that does not hold, otherwise null.
        /// </returns>
        private static IResult<SecurityHeaderPlan> ValidateUsernameToken(SoapSecurityDto options, SoapSecurityModeType mode, Uri endpoint)
        {
            var token = options.UsernameToken;

            if (token.IsNull())
                return null;

            if (token!.Username.IsMissing())
                return Refuse(MessageCodes.V_SEC_092);

            if (token.PasswordType == SoapPasswordType.Digest && token.Password.IsNull())
                return Refuse(MessageCodes.V_SEC_092);

            if (IsXmlRepresentable(token.Username).IsFalse() || IsXmlRepresentable(token.Password).IsFalse())
                return Refuse(MessageCodes.V_SEC_093);

            if (token.SignToken && mode == SoapSecurityModeType.TokenOnly)
                return Refuse(MessageCodes.V_SEC_091);

            var tokenTravelsInClear = mode == SoapSecurityModeType.AsymmetricX509 || mode == SoapSecurityModeType.TokenOnly;

            return tokenTravelsInClear
                   && token.PasswordType == SoapPasswordType.Text
                   && token.Password.IsNotNull()
                   && IsInsecureTransport(endpoint)
                   && token.AllowTextPasswordOverInsecureTransport.IsFalse()
                ? Refuse(MessageCodes.V_SEC_094)
                : null;
        }

        /// <summary>
        ///     Reconciles the addressing action with the request's own action and resolves the
        ///     destination.
        /// </summary>
        /// <param name="addressing">The addressing options.</param>
        /// <param name="transportAction">The request's own action, or null.</param>
        /// <param name="endpoint">The endpoint the request is built for, or null.</param>
        /// <param name="action">[out] The effective action.</param>
        /// <param name="to">[out] The effective destination.</param>
        /// <returns>
        ///     The refusal when the actions conflict or are both unset or the destination is missing or
        ///     relative, otherwise null.
        /// </returns>
        private static IResult<SecurityHeaderPlan> ResolveAddressing(SoapAddressingDto addressing, 
            string transportAction, Uri endpoint, out string action, out Uri to)
        {
            action = null;
            to = null;

            var declared = addressing.Action.IsPresent() ? addressing.Action : null;
            var transport = transportAction.IsPresent() ? transportAction : null;

            if (declared.IsNotNull() && transport.IsNotNull() && string.Equals(declared, transport, StringComparison.Ordinal).IsFalse())
                return Refuse(MessageCodes.V_SEC_020);

            action = declared ?? transport;
            if (action.IsNull())
                return Refuse(MessageCodes.V_SEC_021);

            to = addressing.To ?? endpoint;

            return to.IsNull() || to!.IsAbsoluteUri.IsFalse()
                ? Refuse(MessageCodes.V_SEC_022)
                : null;
        }

        /// <summary>
        ///     Determines whether every character of a value can be written into an XML 1.0 document. A
        ///     null value is representable.
        /// </summary>
        /// <param name="value">The value, or null.</param>
        /// <returns>
        ///     True when the value can be written.
        /// </returns>
        private static bool IsXmlRepresentable(string value)
        {
            if (value.IsNull())
                return true;

            for (var index = 0; index < value!.Length; index++)
            {
                var current = value[index];

                if (XmlConvert.IsXmlChar(current))
                    continue;

                if (char.IsHighSurrogate(current) 
                    && index + 1 < value.Length 
                    && XmlConvert.IsXmlSurrogatePair(value[index + 1], current))
                {
                    index++;

                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        ///     Builds the refusal of a plan under a validation code.
        /// </summary>
        /// <param name="code">The validation message code.</param>
        /// <returns>
        ///     A failed IResult&lt;SecurityHeaderPlan&gt;.
        /// </returns>
        private static IResult<SecurityHeaderPlan> Refuse(MessageCodes code)
            => Result<SecurityHeaderPlan>.Failure(code.GetDescription(), Messages.GetValidationMessage(code));
    }
}
