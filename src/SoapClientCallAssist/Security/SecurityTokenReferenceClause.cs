// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SecurityTokenReferenceClause.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Text;
using SoapClientCallAssist.Helpers;
using System.Security.Cryptography.Xml;
using System.Xml;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     A <see cref="KeyInfoClause" /> that points a signature's <c>KeyInfo</c> at its signing token
    ///     through a <c>wsse:SecurityTokenReference</c>, by <c>wsse:Reference</c> to an element of the
    ///     message or by <c>wsse:KeyIdentifier</c> to a token the receiver holds.
    /// </summary>
    internal sealed class SecurityTokenReferenceClause : KeyInfoClause
    {
        /// <summary>
        ///     The <c>URI</c> of a <c>wsse:Reference</c>, or null for a key identifier.
        /// </summary>
        private readonly string _referenceUri;

        /// <summary>
        ///     The text of a <c>wsse:KeyIdentifier</c>, or null for a reference.
        /// </summary>
        private readonly string _keyIdentifierValue;

        /// <summary>
        ///     The <c>ValueType</c> of the reference or the key identifier.
        /// </summary>
        private readonly string _valueType;

        /// <summary>
        ///     The WS-Security 1.1 <c>TokenType</c> stamped on the token reference, or null.
        /// </summary>
        private readonly string _tokenType;

        /// <summary>
        ///     The <c>EncodingType</c> stamped on a key identifier, or null for a plain text identifier
        ///     such as a SAML assertion id.
        /// </summary>
        private readonly string _encodingType;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SecurityTokenReferenceClause" /> class
        ///     whose key identifier, if any, is base64 encoded.
        /// </summary>
        /// <param name="referenceUri">The <c>URI</c> of a <c>wsse:Reference</c>, or null.</param>
        /// <param name="keyIdentifierValue">The text of a <c>wsse:KeyIdentifier</c>, or null.</param>
        /// <param name="valueType">The <c>ValueType</c>.</param>
        /// <param name="tokenType">The <c>TokenType</c>, or null.</param>
        private SecurityTokenReferenceClause(string referenceUri, string keyIdentifierValue, string valueType, string tokenType)
            : this(referenceUri, keyIdentifierValue, valueType, tokenType, WsSecurityNames.Base64BinaryEncodingType)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SecurityTokenReferenceClause" /> class.
        /// </summary>
        /// <param name="referenceUri">The <c>URI</c> of a <c>wsse:Reference</c>, or null.</param>
        /// <param name="keyIdentifierValue">The text of a <c>wsse:KeyIdentifier</c>, or null.</param>
        /// <param name="valueType">The <c>ValueType</c>.</param>
        /// <param name="tokenType">The <c>TokenType</c>, or null.</param>
        /// <param name="encodingType">The <c>EncodingType</c> of the key identifier, or null for plain text.</param>
        private SecurityTokenReferenceClause(string referenceUri, string keyIdentifierValue, 
            string valueType, string tokenType, string encodingType)
        {
            _referenceUri = referenceUri;
            _keyIdentifierValue = keyIdentifierValue;
            _valueType = valueType;
            _tokenType = tokenType;
            _encodingType = encodingType;
        }

        /// <summary>
        ///     Builds a token reference pointing at an element of the same message by URI.
        /// </summary>
        /// <param name="uri">The reference URI, including the leading <c>#</c> for a same-document reference.</param>
        /// <param name="valueType">The value type naming the kind of token referenced.</param>
        /// <param name="tokenType">The WS-Security 1.1 token type to stamp on the token reference.</param>
        /// <returns>
        ///     The clause.
        /// </returns>
        internal static SecurityTokenReferenceClause Reference(string uri, string valueType, string tokenType = null)
            => new(uri, null, valueType, tokenType);

        /// <summary>
        ///     Builds a token reference identifying a token the receiver already holds.
        /// </summary>
        /// <param name="valueType">The value type naming the kind of identifier.</param>
        /// <param name="value">The identifier, already encoded as its value type requires.</param>
        /// <param name="tokenType">The WS-Security 1.1 token type to stamp on the token reference.</param>
        /// <returns>
        ///     The clause.
        /// </returns>
        internal static SecurityTokenReferenceClause KeyIdentifier(string valueType, string value, string tokenType = null)
            => new(null, value, valueType, tokenType);

        /// <summary>
        ///     Builds the token reference of the certificate embedded as a <c>BinarySecurityToken</c>.
        /// </summary>
        /// <param name="binarySecurityTokenId">The <c>wsu:Id</c> of the token.</param>
        /// <returns>
        ///     The clause.
        /// </returns>
        internal static SecurityTokenReferenceClause ToBinarySecurityToken(string binarySecurityTokenId)
            => Reference("#" + binarySecurityTokenId, WsSecurityNames.X509TokenValueType);

        /// <summary>
        ///     Builds the token reference of a holder-of-key SAML assertion in the same message, a plain
        ///     text key identifier with no <c>EncodingType</c> under the assertion version's value type
        ///     and token type.
        /// </summary>
        /// <param name="assertionId">The assertion's <c>ID</c> (SAML 2.0) or <c>AssertionID</c> (SAML 1.1).</param>
        /// <param name="saml20">True for a SAML 2.0 assertion, false for a SAML 1.1 one.</param>
        /// <returns>
        ///     The clause.
        /// </returns>
        internal static SecurityTokenReferenceClause ToSamlAssertion(string assertionId, bool saml20)
            => new(null, assertionId, SamlNames.KeyIdentifierValueType(saml20), SamlNames.TokenType(saml20), null);

        /// <inheritdoc />
        public override XmlElement GetXml()
        {
            var document = new XmlDocument();

            var securityTokenReference = document.CreateElement(
                WsSecurityNames.WssePrefix, WsSecurityNames.SecurityTokenReferenceLocalName, WsSecurityNames.WsseNamespace);

            if (_tokenType.IsPresent())
            {
                securityTokenReference.SetAttribute(
                    WsSecurity11Names.TokenTypeAttributeName, WsSecurity11Names.Namespace, _tokenType);
            }

            if (_referenceUri.IsPresent())
            {
                var reference = document.CreateElement(
                    WsSecurityNames.WssePrefix, WsSecurityNames.ReferenceLocalName, WsSecurityNames.WsseNamespace);
                reference.SetAttribute(WsSecurityNames.UriAttributeName, _referenceUri);
                reference.SetAttribute(WsSecurityNames.ValueTypeAttributeName, _valueType);
                securityTokenReference.AppendChild(reference);
            }
            else
            {
                var keyIdentifier = document.CreateElement(
                    WsSecurityNames.WssePrefix, WsSecurityNames.KeyIdentifierLocalName, WsSecurityNames.WsseNamespace);
                keyIdentifier.SetAttribute(WsSecurityNames.ValueTypeAttributeName, _valueType);

                if (_encodingType.IsPresent())
                    keyIdentifier.SetAttribute(WsSecurityNames.EncodingTypeAttributeName, _encodingType);

                keyIdentifier.InnerText = _keyIdentifierValue;
                securityTokenReference.AppendChild(keyIdentifier);
            }

            return securityTokenReference;
        }

        /// <summary>
        ///     Does nothing. The clause is only ever written, never parsed from a <c>KeyInfo</c>.
        /// </summary>
        /// <param name="element">The element, ignored.</param>
        public override void LoadXml(XmlElement element)
        {
        }
    }
}
