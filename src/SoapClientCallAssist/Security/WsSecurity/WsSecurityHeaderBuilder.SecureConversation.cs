// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-12 09:30
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="WsSecurityHeaderBuilder.SecureConversation.cs" company="RzR SOFT & TECH">
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
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Helpers;
using System;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     The secure conversation concern of the header builder. It emits the session's
    ///     <c>SecurityContextToken</c> and keys the symmetric key source from a copy of the session's
    ///     secret, zeroed on disposal.
    /// </summary>
    internal sealed partial class WsSecurityHeaderBuilder
    {
        /// <summary>
        ///     The length of the signature derived key of a session, in bytes.
        /// </summary>
        private const int SecureConversationSignatureKeyLength = 24;

        /// <summary>
        ///     The length of the encryption derived key of a session, in bytes.
        /// </summary>
        private const int SecureConversationEncryptionKeyLength = 32;

        /// <summary>
        ///     The fixed clock skew applied to the session's expiry at sign time, one minute,
        ///     independent of the response verification policy's skew.
        /// </summary>
        private static readonly TimeSpan SecureConversationSignSkew = TimeSpan.FromMinutes(1);

        /// <summary>
        ///     Emits the session's <c>sc:SecurityContextToken</c> and keys the symmetric key source
        ///     from a copy of its secret. It fails without a session or for a disposed or expired one.
        /// </summary>
        partial void EmitSecurityContextToken()
        {
            var session = Options.SecureConversation?.Session;

            if (session.IsNull())
            {
                _hookOutcome = SecureConversationRefusal(MessageCodes.V_SEC_061);

                return;
            }

            if (session!.IsDisposed || session.HasSecret.IsFalse())
            {
                _hookOutcome = SecureConversationRefusal(MessageCodes.V_SEC_063);

                return;
            }

            if (IsExpiredWithSkew(session))
            {
                _hookOutcome = SecureConversationRefusal(MessageCodes.V_SEC_062);

                return;
            }

            var version = session.Version;
            var ns = WsSecureConversationNames.Namespace(version);

            var securityContextToken = _document.CreateElement(
                WsSecureConversationNames.Prefix, WsSecureConversationNames.SecurityContextTokenLocalName, ns);
            var securityContextTokenId = _ids.Stamp(_document, securityContextToken, "sct");

            AppendTextChild(
                securityContextToken, WsSecureConversationNames.Prefix, WsSecureConversationNames.IdentifierLocalName, ns, session.ContextIdentifier);

            _security.AppendChild(securityContextToken);

            var secret = session.CopySecret();
            if (secret.IsNull())
            {
                _hookOutcome = SecureConversationRefusal(MessageCodes.V_SEC_063);

                return;
            }

            _symmetricKeySource = new SymmetricKeySource(
                secret,
                SecurityTokenReferenceClause.Reference("#" + securityContextTokenId, WsSecureConversationNames.SecurityContextTokenValueType(version)),
                version,
                WsSecureConversationNames.DefaultLabel,
                SecureConversationSignatureKeyLength,
                SecureConversationEncryptionKeyLength);

            _hookOutcome = Result.Success();
        }

        /// <summary>
        ///     Determines whether a session is at or past its expiry less the fixed sign skew.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>
        ///     True when the session is at or past its expiry within the skew.
        /// </returns>
        private static bool IsExpiredWithSkew(SoapSecureConversationSession session)
            => DateTimeOffset.UtcNow >= session.ExpiresUtc - SecureConversationSignSkew;

        /// <summary>
        ///     Builds a refusal of the secure conversation under a validation code.
        /// </summary>
        /// <param name="code">The validation message code.</param>
        /// <returns>
        ///     A failed IResult.
        /// </returns>
        private static IResult SecureConversationRefusal(MessageCodes code)
            => Result.Failure(code.GetDescription(), Messages.GetValidationMessage(code));
    }
}
