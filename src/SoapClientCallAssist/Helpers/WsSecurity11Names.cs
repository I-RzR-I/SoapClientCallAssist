// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="WsSecurity11Names.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     The WS-Security 1.1 XML names, including the 1.1 and SAML token profile token reference
    ///     value types.
    /// </summary>
    internal static class WsSecurity11Names
    {
        /// <summary>
        ///     The WS-Security 1.1 secext namespace.
        /// </summary>
        internal const string Namespace = "http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd";

        /// <summary>
        ///     The prefix the 1.1 namespace is written with.
        /// </summary>
        internal const string Prefix = "wsse11";

        /// <summary>
        ///     The local name of the <c>wsse11:SignatureConfirmation</c> element a WS-Security 1.1
        ///     responder echoes the request signature value in.
        /// </summary>
        internal const string SignatureConfirmationLocalName = "SignatureConfirmation";

        /// <summary>
        ///     The local name of the <c>Value</c> attribute of a <c>SignatureConfirmation</c>.
        /// </summary>
        internal const string ValueAttributeName = "Value";

        /// <summary>
        ///     The local name of the <c>wsse11:TokenType</c> attribute placed on a
        ///     <c>SecurityTokenReference</c>.
        /// </summary>
        internal const string TokenTypeAttributeName = "TokenType";

        /// <summary>
        ///     The key identifier value type carrying the SHA-1 thumbprint of a certificate.
        /// </summary>
        internal const string ThumbprintSha1ValueType = "http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#ThumbprintSHA1";

        /// <summary>
        ///     The key identifier value type carrying the SHA-1 of an <c>EncryptedKey</c>'s cipher
        ///     value.
        /// </summary>
        internal const string EncryptedKeySha1ValueType = "http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKeySHA1";

        /// <summary>
        ///     The token type of an <c>EncryptedKey</c> referenced through a
        ///     <c>SecurityTokenReference</c>.
        /// </summary>
        internal const string EncryptedKeyTokenType = "http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKey";

        /// <summary>
        ///     The key identifier value type naming a SAML 2.0 assertion by its <c>ID</c>.
        /// </summary>
        internal const string SamlIdValueType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLID";

        /// <summary>
        ///     The key identifier value type naming a SAML 1.1 assertion by its <c>AssertionID</c>.
        /// </summary>
        internal const string SamlAssertionIdValueType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.0#SAMLAssertionID";

        /// <summary>
        ///     The token type of a SAML 1.1 assertion.
        /// </summary>
        internal const string SamlV11TokenType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1";

        /// <summary>
        ///     The token type of a SAML 2.0 assertion.
        /// </summary>
        internal const string SamlV20TokenType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0";
    }
}
