// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="WsSecurityResponseBinding.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Collections;
using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Text;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Helpers;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;
using System.Xml;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     The checks that bind a verified response to its request and refuse a reflected one. An
    ///     element counts as covered only when a signature reference resolves to that very node.
    /// </summary>
    internal static class WsSecurityResponseBinding
    {
        /// <summary>
        ///     The one nonce length a response's derived key token may carry, in bytes.
        /// </summary>
        private const int DerivedKeyNonceLength = 16;

        /// <summary>
        ///     Requires a response's derived key token to carry the request's derivation label, stated
        ///     or defaulted, a nonce of exactly the accepted length and no <c>Properties</c> element. 
        /// </summary>
        /// <param name="token">The derived key token.</param>
        /// <param name="requestLabel">The request's derivation label.</param>
        /// <returns>
        ///     Success when the shape matches, otherwise a failed result.
        /// </returns>
        internal static IResult RequireRequestDerivationShape(XmlElement token, string requestLabel)
        {
            var ns = token.NamespaceURI;

            if (SoapXmlHelper.SingleChildElement(token, WsSecureConversationNames.PropertiesLocalName, ns).IsNotNull())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_075);

            var label = SoapXmlHelper.SingleChildElement(token, WsSecureConversationNames.LabelLocalName, ns)?.InnerText.Trim()
                        ?? WsSecureConversationNames.DefaultLabel;
            if (string.Equals(label, requestLabel, StringComparison.Ordinal).IsFalse())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_046);

            var nonce = TryDecodeBase64(SoapXmlHelper.SingleChildElement(token, WsSecureConversationNames.NonceLocalName, ns)?.InnerText);

            return nonce.IsNull() || nonce!.Length != DerivedKeyNonceLength
                ? WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_046)
                : Result.Success();
        }

        /// <summary>
        ///     Refuses a derived key that equals, in fixed time, any key the request itself derived.
        /// </summary>
        /// <param name="derivedKey">The key derived for the response.</param>
        /// <param name="requestDerivedKeys">Every key the request derived, or null on a mode that derives none.</param>
        /// <returns>
        ///     Success when no request key is reflected, otherwise a failed result.
        /// </returns>
        internal static IResult RefuseReflectedDerivedKey(byte[] derivedKey, byte[][] requestDerivedKeys)
        {
            if (requestDerivedKeys.IsNull())
                return Result.Success();

            var reflected = false;

            foreach (var requestKey in requestDerivedKeys!)
                reflected |= FixedTimeEquals(derivedKey, requestKey);

            return reflected
                ? WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_041)
                : Result.Success();
        }

        /// <summary>
        ///     Requires the response to carry, inside the signature's coverage, a single
        ///     <c>wsa:RelatesTo</c> equal to the request's <c>wsa:MessageID</c>.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <param name="signedXml">The loaded, already-verified signature.</param>
        /// <param name="expectedMessageId">The request's message id.</param>
        /// <returns>
        ///     Success when the covered <c>wsa:RelatesTo</c> matches, otherwise a failed result.
        /// </returns>
        internal static IResult RequireRelatesTo(XmlDocument document, WsuSignedXml signedXml, string expectedMessageId)
        {
            var header = WsSecurityResponseInspection.LocateHeader(document);
            var relatesTo = SingleChildByLocalName(header, WsAddressingNames.RelatesToLocalName, WsAddressingNames.Namespace10, WsAddressingNames.NamespaceAugust2004);

            if (relatesTo.IsNull())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_023);

            if (IsCovered(document, signedXml, relatesTo).IsFalse())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_023);

            return string.Equals(relatesTo!.InnerText.Trim(), expectedMessageId, StringComparison.Ordinal)
                ? Result.Success()
                : WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_023);
        }

        /// <summary>
        ///     Requires the response's Security header to carry, inside the signature's coverage, a
        ///     single WS-Security 1.1 <c>SignatureConfirmation</c> whose value equals the request's
        ///     signature value, compared in fixed time.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <param name="signedXml">The loaded, already-verified signature.</param>
        /// <param name="requestSignatureValue">The request's primary signature value.</param>
        /// <returns>
        ///     Success when the covered confirmation matches, otherwise a failed result.
        /// </returns>
        internal static IResult RequireSignatureConfirmation(XmlDocument document, WsuSignedXml signedXml, byte[] requestSignatureValue)
        {
            var security = WsSecurityResponseInspection.LocateSecurityHeader(document);
            var confirmation = SoapXmlHelper.SingleChildElement(
                security, WsSecurity11Names.SignatureConfirmationLocalName, WsSecurity11Names.Namespace);

            if (confirmation.IsNull() || requestSignatureValue.IsNullOrEmptyEnumerable())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_040);

            if (IsCovered(document, signedXml, confirmation).IsFalse())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_040);

            var echoed = TryDecodeBase64(confirmation!.GetAttribute(WsSecurity11Names.ValueAttributeName));

            return FixedTimeEquals(echoed, requestSignatureValue)
                ? Result.Success()
                : WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_040);
        }

        /// <summary>
        ///     Refuses a response whose Security header carries any <c>Nonce</c> the request itself
        ///     generated, in a username token or in a derived key token.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <param name="requestNonces">The nonces the request generated, base64 encoded.</param>
        /// <returns>
        ///     Success when no request nonce is reflected, otherwise a failed result.
        /// </returns>
        internal static IResult RefuseReflectedNonce(XmlDocument document, IReadOnlyList<string> requestNonces)
        {
            var security = WsSecurityResponseInspection.LocateSecurityHeader(document);

            if (security.IsNull() || requestNonces.IsNullOrEmptyEnumerable())
                return Result.Success();

            foreach (XmlNode node in security!.GetElementsByTagName("*"))
            {
                if (node is not XmlElement element || string.Equals(element.LocalName, WsSecurityNames.NonceLocalName, StringComparison.Ordinal).IsFalse())
                    continue;

                var value = element.InnerText.Trim();

                foreach (var ours in requestNonces)
                {
                    if (string.Equals(value, ours, StringComparison.Ordinal))
                        return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_041);
                }
            }

            return Result.Success();
        }

        /// <summary>
        ///     Refuses a response whose signature value is the request's own.
        /// </summary>
        /// <param name="signedXml">The loaded response signature.</param>
        /// <param name="requestSignatureValue">The request's primary signature value, or null.</param>
        /// <returns>
        ///     Success when the signature value is not the request's own, otherwise a failed result.
        /// </returns>
        internal static IResult RefuseReflectedSignatureValue(SignedXml signedXml, byte[] requestSignatureValue)
        {
            if (requestSignatureValue.IsNullOrEmptyEnumerable())
                return Result.Success();

            return FixedTimeEquals(signedXml.SignatureValue, requestSignatureValue)
                ? WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_042)
                : Result.Success();
        }

        /// <summary>
        ///     Compares two byte arrays in time that depends only on the length of the first.
        /// </summary>
        /// <param name="left">The first array, or null.</param>
        /// <param name="right">The second array, or null.</param>
        /// <returns>
        ///     True when both are present, equal in length and equal in content.
        /// </returns>
        internal static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left.IsNull() || right.IsNull() || left!.Length != right!.Length)
                return false;

            var difference = 0;

            for (var index = 0; index < left.Length; index++)
                difference |= left[index] ^ right[index];

            return difference == 0;
        }

        /// <summary>
        ///     Determines whether a structurally located element is one the signature covers.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <param name="signedXml">The loaded signature.</param>
        /// <param name="candidate">The candidate element.</param>
        /// <returns>
        ///     True when a reference resolves to the very candidate.
        /// </returns>
        private static bool IsCovered(XmlDocument document, WsuSignedXml signedXml, XmlElement candidate)
            => WsSecurityResponseInspection.IsCoveredByReference(
                document, signedXml, candidate, WsSecurityResponseInspection.SignedIds(signedXml));

        /// <summary>
        ///     Locates the single child of an element with a local name in any of the given
        ///     namespaces.
        /// </summary>
        /// <param name="parent">The parent, or null.</param>
        /// <param name="localName">The child's local name.</param>
        /// <param name="namespaces">The namespaces accepted.</param>
        /// <returns>
        ///     The single matching child, or null when none or more than one matches.
        /// </returns>
        private static XmlElement SingleChildByLocalName(XmlElement parent, string localName, params string[] namespaces)
        {
            if (parent.IsNull())
                return null;

            XmlElement found = null;

            foreach (XmlNode child in parent!.ChildNodes)
            {
                if (child is not XmlElement element || string.Equals(element.LocalName, localName, StringComparison.Ordinal).IsFalse())
                    continue;

                if (Array.IndexOf(namespaces, element.NamespaceURI) < 0)
                    continue;

                if (found.IsNotNull())
                    return null;

                found = element;
            }

            return found;
        }

        /// <summary>
        ///     Decodes a base64 attribute value without throwing.
        /// </summary>
        /// <param name="value">The attribute value, or null.</param>
        /// <returns>
        ///     The decoded bytes, or null when the value is absent or not base64.
        /// </returns>
        private static byte[] TryDecodeBase64(string value)
        {
            if (value.IsMissing())
                return null;

            try
            {
                return Convert.FromBase64String(value.Trim());
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
