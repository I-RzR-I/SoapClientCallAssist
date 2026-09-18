// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapSecurityModeType.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

namespace SoapClientCallAssist.Enums
{
    /// <summary>
    ///     The WS-Security mode a request is built in, resolved from the security options. It decides
    ///     what the Security header carries, in which order, and how the response is checked.
    /// </summary>
    internal enum SoapSecurityModeType
    {
        /// <summary>
        ///     Asymmetric X.509 binding: the request is signed with the caller's certificate, embedded
        ///     as a <c>BinarySecurityToken</c>, and the response is checked against a service
        ///     certificate.
        /// </summary>
        AsymmetricX509 = 1,

        /// <summary>
        ///     Symmetric binding keyed by an <c>EncryptedKey</c> wrapped for the service certificate;
        ///     both directions are signed with keys derived from that one secret.
        /// </summary>
        SymmetricEncryptedKey = 2,

        /// <summary>
        ///     Symmetric binding keyed by an established WS-SecureConversation session, referenced
        ///     through a <c>SecurityContextToken</c>.
        /// </summary>
        SecureConversation = 3,

        /// <summary>
        ///     A token-carrying header with no signature at all: a <c>UsernameToken</c> or a SAML
        ///     assertion, optionally with a timestamp, and nothing to check on the response.
        /// </summary>
        TokenOnly = 4
    }
}
