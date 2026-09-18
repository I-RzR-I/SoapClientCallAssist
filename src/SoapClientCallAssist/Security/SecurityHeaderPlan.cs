// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SecurityHeaderPlan.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using System;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     The resolved, validated shape of one request's Security header, built once per request.
    ///     It references the caller's options and must not outlive the build.
    /// </summary>
    internal sealed class SecurityHeaderPlan
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SecurityHeaderPlan" /> class.
        /// </summary>
        /// <param name="mode">The resolved security mode.</param>
        /// <param name="keyFamily">
        ///     The key family of the primary signature, or null when the mode emits none.
        /// </param>
        /// <param name="options">The validated security options.</param>
        /// <param name="action">
        ///     The effective WS-Addressing action, or null when addressing is off.
        /// </param>
        /// <param name="to">
        ///     The effective WS-Addressing destination, or null when addressing is off.
        /// </param>
        /// <param name="requireSignatureConfirmation">
        ///     Whether a response must carry a signature confirmation.
        /// </param>
        /// <param name="encryptUsernameToken">Whether the username token is emitted encrypted.</param>
        internal SecurityHeaderPlan(SoapSecurityModeType mode, SignatureKeyFamily? keyFamily, SoapSecurityDto options,
            string action, Uri to, bool requireSignatureConfirmation, bool encryptUsernameToken)
        {
            Mode = mode;
            KeyFamily = keyFamily;
            Options = options;
            Action = action;
            To = to;
            RequireSignatureConfirmation = requireSignatureConfirmation;
            EncryptUsernameToken = encryptUsernameToken;
        }

        /// <summary>
        ///     The one mode the request is built in.
        /// </summary>
        /// <value>
        ///     The mode.
        /// </value>
        internal SoapSecurityModeType Mode { get; }

        /// <summary>
        ///     The key family of the primary signature, or null when the mode emits no signature.
        /// </summary>
        /// <value>
        ///     The key family.
        /// </value>
        internal SignatureKeyFamily? KeyFamily { get; }

        /// <summary>
        ///     The validated security options the plan was resolved from.
        /// </summary>
        /// <value>
        ///     The options.
        /// </value>
        internal SoapSecurityDto Options { get; }

        /// <summary>
        ///     The effective WS-Addressing action, reconciled with the request's own action, or null
        ///     when addressing is off.
        /// </summary>
        /// <value>
        ///     The action.
        /// </value>
        internal string Action { get; }

        /// <summary>
        ///     The effective WS-Addressing destination, or null when addressing is off.
        /// </summary>
        /// <value>
        ///     to.
        /// </value>
        internal Uri To { get; }

        /// <summary>
        ///     True when a response must carry a signed <c>SignatureConfirmation</c> echoing the
        ///     request's signature.
        /// </summary>
        /// <value>
        ///     True if require signature confirmation, false if not.
        /// </value>
        internal bool RequireSignatureConfirmation { get; }

        /// <summary>
        ///     True when the username token is emitted inside an <c>EncryptedData</c>.
        /// </summary>
        /// <value>
        ///     True if encrypt username token, false if not.
        /// </value>
        internal bool EncryptUsernameToken { get; }

        /// <summary>
        ///     True when the mode emits a primary signature, false for a token-only header.
        /// </summary>
        /// <value>
        ///     True if emits signature, false if not.
        /// </value>
        internal bool EmitsSignature => KeyFamily.HasValue;

        /// <summary>
        ///     True for the symmetric binding and the secure conversation, the modes keyed by a shared
        ///     secret.
        /// </summary>
        /// <value>
        ///     True if this object is symmetric family, false if not.
        /// </value>
        internal bool IsSymmetricFamily
            => Mode == SoapSecurityModeType.SymmetricEncryptedKey || Mode == SoapSecurityModeType.SecureConversation;

        /// <summary>
        ///     True when WS-Addressing headers are emitted.
        /// </summary>
        /// <value>
        ///     True if includes addressing, false if not.
        /// </value>
        internal bool IncludesAddressing => Options.Addressing.IsNotNull();

        /// <summary>
        ///     True when the request carries a holder-of-key SAML assertion, whose named key the primary
        ///     signature proves possession of in place of an embedded certificate.
        /// </summary>
        /// <value>
        ///     True if this object is holder of key saml, false if not.
        /// </value>
        internal bool IsHolderOfKeySaml
            => Options.SamlToken.IsNotNull() && Options.SamlToken!.Confirmation == SoapSamlConfirmationType.HolderOfKey;

        /// <summary>
        ///     True when the caller's certificate is embedded as a <c>BinarySecurityToken</c>, on the
        ///     asymmetric binding without a holder-of-key SAML assertion or on a symmetric mode that
        ///     endorses with it.
        /// </summary>
        /// <value>
        ///     True if emits binary security token, false if not.
        /// </value>
        internal bool EmitsBinarySecurityToken
            => (Mode == SoapSecurityModeType.AsymmetricX509 && IsHolderOfKeySaml.IsFalse())
               || (IsSymmetricFamily && EmitsEndorsingSignature);

        /// <summary>
        ///     True when the symmetric binding asks to endorse the primary signature with the signing
        ///     certificate.
        /// </summary>
        /// <value>
        ///     True if emits endorsing signature, false if not.
        /// </value>
        internal bool EmitsEndorsingSignature
            => IsSymmetricFamily
               && Options.SigningCertificate.IsNotNull()
               && Options.SymmetricBinding.IsNotNull()
               && Options.SymmetricBinding!.EndorseWithSigningCertificate;

        /// <summary>
        ///     True when the signing certificate's RSA private key is needed to build the request, to
        ///     sign, to endorse or to prove possession of a holder-of-key SAML assertion.
        /// </summary>
        /// <value>
        ///     True if requires rsa private key, false if not.
        /// </value>
        internal bool RequiresRsaPrivateKey
            => Mode == SoapSecurityModeType.AsymmetricX509
               || EmitsEndorsingSignature
               || IsHolderOfKeySaml;
    }
}
