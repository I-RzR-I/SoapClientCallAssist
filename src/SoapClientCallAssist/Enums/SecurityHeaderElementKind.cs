// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SecurityHeaderElementKind.cs" company="RzR SOFT & TECH">
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
    ///     The kinds of child element a <c>wsse:Security</c> header can carry. The header builder
    ///     walks a per-mode table of these in order and emits each one the options call for.
    /// </summary>
    internal enum SecurityHeaderElementKind
    {
        /// <summary>
        ///     The <c>wsse:BinarySecurityToken</c> carrying the caller's X.509 certificate.
        /// </summary>
        BinarySecurityToken = 1,

        /// <summary>
        ///     The <c>wsu:Timestamp</c>.
        /// </summary>
        Timestamp = 2,

        /// <summary>
        ///     The plaintext <c>wsse:UsernameToken</c>.
        /// </summary>
        UsernameToken = 3,

        /// <summary>
        ///     The caller-supplied <c>saml:Assertion</c>.
        /// </summary>
        SamlAssertion = 4,

        /// <summary>
        ///     The <c>xenc:EncryptedKey</c> wrapping the symmetric secret for the service certificate.
        /// </summary>
        EncryptedKey = 5,

        /// <summary>
        ///     The <c>SecurityContextToken</c> referencing an established WS-SecureConversation session.
        /// </summary>
        SecurityContextToken = 6,

        /// <summary>
        ///     The <c>DerivedKeyToken</c> elements, one per derived signing or encryption key, each
        ///     placed after the token it references by URI.
        /// </summary>
        DerivedKeyTokens = 7,

        /// <summary>
        ///     The <c>xenc:ReferenceList</c> naming every encrypted part of the message.
        /// </summary>
        ReferenceList = 8,

        /// <summary>
        ///     The <c>wsse:UsernameToken</c> wrapped in <c>xenc:EncryptedData</c>.
        /// </summary>
        EncryptedUsernameToken = 9,

        /// <summary>
        ///     The primary <c>ds:Signature</c> covering the message parts.
        /// </summary>
        PrimarySignature = 10,

        /// <summary>
        ///     The endorsing <c>ds:Signature</c> whose single reference is the primary signature.
        /// </summary>
        EndorsingSignature = 11
    }
}
