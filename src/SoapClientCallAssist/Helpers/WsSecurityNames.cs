// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="WsSecurityNames.cs" company="RzR SOFT & TECH">
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
    ///     The WS-Security XML names shared by the signer and the verifier.
    /// </summary>
    internal static class WsSecurityNames
    {
        /// <summary>
        ///     The WS-Security 1.0 secext namespace.
        /// </summary>
        internal const string WsseNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        /// <summary>
        ///     The WS-Security 1.0 utility namespace, home of <c>wsu:Id</c>.
        /// </summary>
        internal const string WsuNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

        /// <summary>
        ///     The X.509 token profile value type of a <c>BinarySecurityToken</c>.
        /// </summary>
        internal const string X509TokenValueType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3";

        /// <summary>
        ///     The base64 binary encoding type of a <c>BinarySecurityToken</c>.
        /// </summary>
        internal const string Base64BinaryEncodingType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";

        /// <summary>
        ///     The local name of the <c>wsse:Security</c> header.
        /// </summary>
        internal const string SecurityLocalName = "Security";

        /// <summary>
        ///     The local name of the <c>wsse:BinarySecurityToken</c> element.
        /// </summary>
        internal const string BinarySecurityTokenLocalName = "BinarySecurityToken";

        /// <summary>
        ///     The local name of the <c>wsu:Timestamp</c> element.
        /// </summary>
        internal const string TimestampLocalName = "Timestamp";

        /// <summary>
        ///     The local name of the <c>wsu:Created</c> element.
        /// </summary>
        internal const string CreatedLocalName = "Created";

        /// <summary>
        ///     The local name of the <c>wsu:Expires</c> element.
        /// </summary>
        internal const string ExpiresLocalName = "Expires";

        /// <summary>
        ///     The local name of the <c>wsu:Id</c> attribute.
        /// </summary>
        internal const string IdLocalName = "Id";

        /// <summary>
        ///     The local name of the <c>ValueType</c> attribute.
        /// </summary>
        internal const string ValueTypeAttributeName = "ValueType";

        /// <summary>
        ///     The local name of the <c>EncodingType</c> attribute.
        /// </summary>
        internal const string EncodingTypeAttributeName = "EncodingType";

        /// <summary>
        ///     The local name of the SOAP <c>mustUnderstand</c> attribute.
        /// </summary>
        internal const string MustUnderstandLocalName = "mustUnderstand";

        /// <summary>
        ///     The local name of the SOAP 1.1 <c>actor</c> attribute.
        /// </summary>
        internal const string Soap11ActorLocalName = "actor";

        /// <summary>
        ///     The local name of the SOAP 1.2 <c>role</c> attribute.
        /// </summary>
        internal const string Soap12RoleLocalName = "role";

        /// <summary>
        ///     The SOAP 1.2 envelope namespace, which selects the <c>role</c> attribute name over
        ///     <c>actor</c>.
        /// </summary>
        internal const string Soap12EnvelopeNamespace = "http://www.w3.org/2003/05/soap-envelope";

        /// <summary>
        ///     The round-trippable date and time format of <c>wsu:Created</c> and <c>wsu:Expires</c>.
        /// </summary>
        internal const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        /// <summary>
        ///     The local name of the SOAP Envelope element, defined once on <see cref="SoapContracts" />.
        /// </summary>
        internal const string EnvelopeLocalName = SoapContracts.EnvelopeLocalName;

        /// <summary>
        ///     The local name of the SOAP Header element.
        /// </summary>
        internal const string HeaderLocalName = "Header";

        /// <summary>
        ///     The local name of the <c>ds:Signature</c> element, accepted by the verifier only as a
        ///     child of the <c>wsse:Security</c> header.
        /// </summary>
        internal const string SignatureLocalName = "Signature";

        /// <summary>
        ///     The local name of the <c>ds:RetrievalMethod</c> element, refused in a response
        ///     signature's <c>KeyInfo</c>.
        /// </summary>
        internal const string RetrievalMethodLocalName = "RetrievalMethod";

        /// <summary>
        ///     The local name of the <c>ds:HMACOutputLength</c> element, refused in a response
        ///     signature.
        /// </summary>
        internal const string HmacOutputLengthLocalName = "HMACOutputLength";

        /// <summary>
        ///     The prefix the WS-Security secext elements are written with.
        /// </summary>
        internal const string WssePrefix = "wsse";

        /// <summary>
        ///     The prefix the WS-Security utility elements are written with.
        /// </summary>
        internal const string WsuPrefix = "wsu";

        /// <summary>
        ///     The local name of the <c>wsse:UsernameToken</c> element.
        /// </summary>
        internal const string UsernameTokenLocalName = "UsernameToken";

        /// <summary>
        ///     The local name of the <c>wsse:Username</c> element.
        /// </summary>
        internal const string UsernameLocalName = "Username";

        /// <summary>
        ///     The local name of the <c>wsse:Password</c> element.
        /// </summary>
        internal const string PasswordLocalName = "Password";

        /// <summary>
        ///     The local name of the <c>wsse:Nonce</c> element.
        /// </summary>
        internal const string NonceLocalName = "Nonce";

        /// <summary>
        ///     The local name of the <c>Type</c> attribute of a <c>wsse:Password</c>.
        /// </summary>
        internal const string PasswordTypeAttributeName = "Type";

        /// <summary>
        ///     The local name of the <c>wsse:SecurityTokenReference</c> element.
        /// </summary>
        internal const string SecurityTokenReferenceLocalName = "SecurityTokenReference";

        /// <summary>
        ///     The local name of the <c>wsse:Reference</c> child of a token reference.
        /// </summary>
        internal const string ReferenceLocalName = "Reference";

        /// <summary>
        ///     The local name of the <c>wsse:KeyIdentifier</c> child of a token reference.
        /// </summary>
        internal const string KeyIdentifierLocalName = "KeyIdentifier";

        /// <summary>
        ///     The local name of the <c>URI</c> attribute of a <c>wsse:Reference</c>.
        /// </summary>
        internal const string UriAttributeName = "URI";

        /// <summary>
        ///     The prefix declared for the envelope namespace on a signed header when the envelope
        ///     binds that namespace to no prefix.
        /// </summary>
        internal const string EnvelopePrefixFallback = "senv";
    }
}