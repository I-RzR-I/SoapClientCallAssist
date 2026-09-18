// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 22:40
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="WsSecuritySymmetricResponseVerifier.cs" company="RzR SOFT & TECH">
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
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helpers;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     Verifies a response to a symmetric-binding request against that request's consumed key
    ///     material, deriving the HMAC key from the request's secret under the response's nonce. Any
    ///     <c>EncryptedKey</c> not the request's own is refused.
    /// </summary>
    internal static class WsSecuritySymmetricResponseVerifier
    {
        /// <summary>
        ///     The shortest derived key accepted off a response, in bytes.
        /// </summary>
        private const int MinDerivedKeyLength = 16;

        /// <summary>
        ///     The longest derived key accepted off a response, in bytes.
        /// </summary>
        private const int MaxDerivedKeyLength = 64;

        /// <summary>
        ///     The default length of a derived key whose token states none, in bytes, as
        ///     WS-SecureConversation defines it.
        /// </summary>
        private const int DefaultDerivedKeyLength = 32;

        /// <summary>
        ///     The HMAC-SHA256 signature method.
        /// </summary>
        private const string HmacSha256Method = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256";

        /// <summary>
        ///     The namespaces a <c>DerivedKeyToken</c> may be written in.
        /// </summary>
        private static readonly string[] SecureConversationNamespaces =
        {
            WsSecureConversationNames.NamespaceFebruary2005,
            WsSecureConversationNames.NamespaceDecember2005
        };

        /// <summary>
        ///     Verifies a response against the consumed material of the symmetric request it answers.
        /// </summary>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <param name="soapResponse">The raw response.</param>
        /// <returns>
        ///     Success carrying the coverage report, a failed result when the material carries no key
        ///     or the response is empty or oversized, otherwise the verification failure.
        /// </returns>
        internal static IResult<SoapSignatureVerificationResult> Verify(ConsumedKeyMaterial consumed, string soapResponse)
        {
            if (LacksKeyMaterial(consumed))
                return Refuse(MessageCodes.V_SEC_030);

            if (soapResponse.IsNullOrEmpty() || soapResponse.Length > SoapContracts.MaxDocumentCharacters)
                return Refuse(MessageCodes.V_SEC_005, SoapContracts.MaxDocumentCharacters);

            var loaded = SoapXmlDocumentLoader.Load(soapResponse, true, MessageCodes.ER_SEC_DOM);
            if (loaded.IsSuccess.IsFalse())
                return loaded.Propagate<SoapSignatureVerificationResult>();

            return Verify(consumed, loaded.Response);
        }

        /// <summary>
        ///     Verifies an already parsed response against the consumed material of the symmetric
        ///     request it answers, over the same document the decryptor decrypted into.
        /// </summary>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <param name="document">The parsed response, loaded with whitespace preserved.</param>
        /// <returns>
        ///     Success carrying the coverage report, a failed result for a missing or invalid
        ///     signature, otherwise the first check's failure.
        /// </returns>
        internal static IResult<SoapSignatureVerificationResult> Verify(ConsumedKeyMaterial consumed, XmlDocument document)
        {
            if (LacksKeyMaterial(consumed))
                return Refuse(MessageCodes.V_SEC_030);

            var foreignKey = IsSecureConversation(consumed)
                ? RefuseAnyEncryptedKey(document)
                : RefuseForeignEncryptedKey(document, consumed.EncryptedKeySha1);
            if (foreignKey.IsSuccess.IsFalse())
                return foreignKey.Propagate<SoapSignatureVerificationResult>();

            var signatureElement = WsSecurityResponseInspection.LocateSignature(document);
            if (signatureElement.IsNull())
                return Refuse(MessageCodes.V_SEC_006);

            if (consumed.KeyFamily.IsNull())
                return Refuse(MessageCodes.V_SEC_030);

            var shape = WsSecurityResponseInspection.ValidateSignatureShape(signatureElement, consumed.KeyFamily!.Value, consumed.Policy.AllowSha1Algorithms);
            if (shape.IsSuccess.IsFalse())
                return shape.Propagate<SoapSignatureVerificationResult>();

            var reflectedNonce = WsSecurityResponseBinding.RefuseReflectedNonce(document, consumed.Nonces);
            if (reflectedNonce.IsSuccess.IsFalse())
                return reflectedNonce.Propagate<SoapSignatureVerificationResult>();

            var derived = ReadDerivedKey(document, signatureElement, consumed);
            if (derived.IsSuccess.IsFalse())
                return derived.Propagate<SoapSignatureVerificationResult>();

            WsuSignedXml signedXml;
            bool signatureValid;

            var key = derived.Response;

            try
            {
                signedXml = new WsuSignedXml(document) { Resolver = null };
                signedXml.LoadXml(signatureElement);

                var reflectedSignature = WsSecurityResponseBinding.RefuseReflectedSignatureValue(signedXml, consumed.SignatureValue);
                if (reflectedSignature.IsSuccess.IsFalse())
                    return reflectedSignature.Propagate<SoapSignatureVerificationResult>();

                using (var mac = NewHmac(signedXml.SignedInfo!.SignatureMethod, key))
                    signatureValid = signedXml.CheckSignature(mac);
            }
            catch (Exception ex)
            {
                return Result<SoapSignatureVerificationResult>
                    .Failure(MessageCodes.ER_SEC_C14N.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_C14N))
                    .WithOptionalError(ex, "computing the WS-Security signature during verification");
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
            }

            if (signatureValid.IsFalse())
                return Result<SoapSignatureVerificationResult>.Failure(MessageCodes.ER_SEC_VER.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_VER));

            var coverage = WsSecurityResponseInspection.ResolveCoverage(document, signedXml);

            var policyFailure = WsSecurityResponseInspection.ResolvePolicyFailure(coverage, consumed.Policy);
            if (policyFailure.IsNotNull())
                return policyFailure.Propagate<SoapSignatureVerificationResult>();

            var bound = BindToRequest(document, signedXml, consumed);

            return bound.IsSuccess.IsFalse()
                ? bound.Propagate<SoapSignatureVerificationResult>()
                : Result<SoapSignatureVerificationResult>.Success(coverage);
        }

        /// <summary>
        ///     Refuses a response carrying any <c>xenc:EncryptedKey</c> other than the request's own,
        ///     identified by the SHA-1 of its cipher value.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <param name="ourEncryptedKeySha1">The SHA-1 of the request's encrypted key.</param>
        /// <returns>
        ///     Success when only the request's own key is present, otherwise a failed result.
        /// </returns>
        private static IResult RefuseForeignEncryptedKey(XmlDocument document, byte[] ourEncryptedKeySha1)
        {
            foreach (XmlNode node in document.GetElementsByTagName(XmlEncryptionNames.EncryptedKeyLocalName, XmlEncryptionNames.Namespace))
            {
                if (node is not XmlElement encryptedKey)
                    continue;

                var cipherData = SoapXmlHelper.SingleChildElement(encryptedKey, XmlEncryptionNames.CipherDataLocalName, XmlEncryptionNames.Namespace);
                var cipherValue = cipherData.IsNull()
                    ? null
                    : SoapXmlHelper.SingleChildElement(cipherData, XmlEncryptionNames.CipherValueLocalName, XmlEncryptionNames.Namespace);

                var wrapped = TryDecodeBase64(cipherValue?.InnerText);
                if (wrapped.IsNull())
                    return DecryptionRefusal();

                byte[] sha1;

                using (var hash = SHA1.Create())
                    sha1 = hash.ComputeHash(wrapped);

                if (WsSecurityResponseBinding.FixedTimeEquals(sha1, ourEncryptedKeySha1).IsFalse())
                    return DecryptionRefusal();
            }

            return Result.Success();
        }

        /// <summary>
        ///     Derives the response signature's key from the token the signature's <c>KeyInfo</c>
        ///     references, requiring it to name the request's key and label, and refusing a key the
        ///     request itself derived.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <param name="signatureElement">The response signature.</param>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <returns>
        ///     Success carrying the derived key, otherwise the first check's failure.
        /// </returns>
        private static IResult<byte[]> ReadDerivedKey(XmlDocument document, XmlElement signatureElement, ConsumedKeyMaterial consumed)
        {
            var namespaces = new XmlNamespaceManager(document.NameTable);
            namespaces.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
            namespaces.AddNamespace("wsse", WsSecurityNames.WsseNamespace);

            var keyReference = signatureElement.SelectSingleNode("ds:KeyInfo/wsse:SecurityTokenReference/wsse:Reference", namespaces) as XmlElement;
            if (keyReference.IsNull())
                return Unreadable();

            var uri = keyReference!.GetAttribute(WsSecurityNames.UriAttributeName);
            if (WsSecurityResponseInspection.IsPlainSameDocumentFragment(uri).IsFalse())
                return Unreadable();

            var token = LocateDerivedKeyToken(document, uri.Substring(1));
            if (token.IsNull())
                return Unreadable();

            var valueType = keyReference.GetAttribute(WsSecurityNames.ValueTypeAttributeName);
            if (valueType.IsPresent() && string.Equals(valueType, token!.NamespaceURI + WsSecureConversationNames.DerivedKeyTokenValueTypeSuffix, StringComparison.Ordinal).IsFalse())
                return Unreadable();

            var keyed = IsSecureConversation(consumed)
                ? RequireKeyedToOurSct(token, consumed.SctIdentifier)
                : RequireKeyedToOurEncryptedKey(token, consumed.EncryptedKeySha1);
            if (keyed.IsSuccess.IsFalse())
                return keyed.Propagate<byte[]>();

            var shape = WsSecurityResponseBinding.RequireRequestDerivationShape(token, consumed.KeyDerivationLabel);
            if (shape.IsSuccess.IsFalse())
                return shape.Propagate<byte[]>();

            var derived = DeriveKey(token, consumed.SessionSecret);
            if (derived.IsSuccess.IsFalse())
                return derived;

            var reflected = WsSecurityResponseBinding.RefuseReflectedDerivedKey(derived.Response, consumed.RequestDerivedKeys);
            if (reflected.IsSuccess)
                return derived;

            Array.Clear(derived.Response, 0, derived.Response.Length);

            return reflected.Propagate<byte[]>();
        }

        /// <summary>
        ///     Locates the single <c>DerivedKeyToken</c> carrying a <c>wsu:Id</c> among the direct
        ///     children of the single Security header.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <param name="id">The id the signature references.</param>
        /// <returns>
        ///     The token, or null when absent or ambiguous.
        /// </returns>
        internal static XmlElement LocateDerivedKeyToken(XmlDocument document, string id)
        {
            var security = WsSecurityResponseInspection.LocateSecurityHeader(document);
            if (security.IsNull())
                return null;

            XmlElement found = null;

            foreach (XmlNode child in security!.ChildNodes)
            {
                if (child is not XmlElement element
                    || string.Equals(element.LocalName, WsSecureConversationNames.DerivedKeyTokenLocalName, StringComparison.Ordinal).IsFalse()
                    || Array.IndexOf(SecureConversationNamespaces, element.NamespaceURI) < 0)
                    continue;

                if (string.Equals(element.GetAttribute(WsSecurityNames.IdLocalName, WsSecurityNames.WsuNamespace), id, StringComparison.Ordinal).IsFalse())
                    continue;

                if (found.IsNotNull())
                    return null;

                found = element;
            }

            return found;
        }

        /// <summary>
        ///     Requires the token's <c>SecurityTokenReference</c> to identify the request's encrypted
        ///     key by an <c>#EncryptedKeySHA1</c> key identifier equal, in fixed time, to ours.
        /// </summary>
        /// <param name="token">The derived key token.</param>
        /// <param name="ourEncryptedKeySha1">The SHA-1 of the request's encrypted key.</param>
        /// <returns>
        ///     Success on a match, otherwise a failed result for an unreadable or foreign identifier.
        /// </returns>
        internal static IResult RequireKeyedToOurEncryptedKey(XmlElement token, byte[] ourEncryptedKeySha1)
        {
            var reference = SoapXmlHelper.SingleChildElement(token, WsSecurityNames.SecurityTokenReferenceLocalName, WsSecurityNames.WsseNamespace);
            var identifier = reference.IsNull()
                ? null
                : SoapXmlHelper.SingleChildElement(reference, WsSecurityNames.KeyIdentifierLocalName, WsSecurityNames.WsseNamespace);

            if (identifier.IsNull())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_046);

            if (string.Equals(identifier!.GetAttribute(WsSecurityNames.ValueTypeAttributeName), WsSecurity11Names.EncryptedKeySha1ValueType, StringComparison.Ordinal).IsFalse())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_046);

            var claimed = TryDecodeBase64(identifier.InnerText);
            if (claimed.IsNull())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_046);

            return WsSecurityResponseBinding.FixedTimeEquals(claimed, ourEncryptedKeySha1)
                ? Result.Success()
                : WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_047);
        }

        /// <summary>
        ///     Requires the token's <c>SecurityTokenReference</c> to identify this session's security
        ///     context token, by a <c>Reference</c> whose <c>URI</c> equals the session's context
        ///     identifier, compared byte for byte and never dereferenced.
        /// </summary>
        /// <param name="token">The derived key token.</param>
        /// <param name="sctIdentifier">This session's security context token identifier.</param>
        /// <returns>
        ///     Success on a match, otherwise a failed result for an unreadable reference or another
        ///     context.
        /// </returns>
        internal static IResult RequireKeyedToOurSct(XmlElement token, string sctIdentifier)
        {
            if (sctIdentifier.IsMissing())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_046);

            var reference = SoapXmlHelper.SingleChildElement(token, WsSecurityNames.SecurityTokenReferenceLocalName, WsSecurityNames.WsseNamespace);
            var contextReference = reference.IsNull()
                ? null
                : SoapXmlHelper.SingleChildElement(reference, WsSecurityNames.ReferenceLocalName, WsSecurityNames.WsseNamespace);

            if (contextReference.IsNull())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_046);

            var valueType = contextReference!.GetAttribute(WsSecurityNames.ValueTypeAttributeName);
            if (valueType.IsPresent() && valueType.EndsWith(WsSecureConversationNames.SecurityContextTokenValueTypeSuffix, StringComparison.Ordinal).IsFalse())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_046);

            var uri = contextReference.GetAttribute(WsSecurityNames.UriAttributeName);
            if (uri.IsMissing())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_046);

            return string.Equals(uri, sctIdentifier, StringComparison.Ordinal)
                ? Result.Success()
                : WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_074);
        }

        /// <summary>
        ///     Determines whether the consumed material was minted for a secure conversation request.
        /// </summary>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <returns>
        ///     True for a secure conversation, false for the encrypted-key symmetric binding.
        /// </returns>
        internal static bool IsSecureConversation(ConsumedKeyMaterial consumed)
            => consumed.Mode == SoapSecurityModeType.SecureConversation;

        /// <summary>
        ///     Determines whether the consumed material lacks the key a symmetric check needs: always
        ///     the secret, and on the encrypted-key binding also the encrypted key digest.
        /// </summary>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <returns>
        ///     True when a required piece is missing.
        /// </returns>
        private static bool LacksKeyMaterial(ConsumedKeyMaterial consumed)
            => consumed.SessionSecret.IsNullOrEmptyEnumerable()
               || IsSecureConversation(consumed).IsFalse() && consumed.EncryptedKeySha1.IsNullOrEmptyEnumerable();

        /// <summary>
        ///     Refuses a secure conversation response carrying any <c>xenc:EncryptedKey</c>.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <returns>
        ///     Success when none is present, otherwise a failed result.
        /// </returns>
        private static IResult RefuseAnyEncryptedKey(XmlDocument document)
            => document.GetElementsByTagName(XmlEncryptionNames.EncryptedKeyLocalName, XmlEncryptionNames.Namespace).Count > 0
                ? DecryptionRefusal()
                : Result.Success();

        /// <summary>
        ///     Reads the nonce, offset, length, label and algorithm off a derived key token, applying
        ///     the specification defaults and refusing anything outside what this library derives,
        ///     then derives the key from the secret.
        /// </summary>
        /// <param name="token">The derived key token.</param>
        /// <param name="secret">The request's secret.</param>
        /// <returns>
        ///     The derived key, or a failed result for a token this library cannot read.
        /// </returns>
        internal static IResult<byte[]> DeriveKey(XmlElement token, byte[] secret)
        {
            var ns = token.NamespaceURI;

            var algorithm = token.GetAttribute(WsSecureConversationNames.AlgorithmAttributeName);
            if (algorithm.IsPresent() && string.Equals(algorithm, ns + WsSecureConversationNames.Psha1AlgorithmSuffix, StringComparison.Ordinal).IsFalse())
                return Unreadable();

            var nonce = TryDecodeBase64(ChildText(token, WsSecureConversationNames.NonceLocalName, ns));
            if (nonce.IsNullOrEmptyEnumerable())
                return Unreadable();

            var length = ReadInteger(token, WsSecureConversationNames.LengthLocalName, ns, DefaultDerivedKeyLength);
            var offset = ReadInteger(token, WsSecureConversationNames.OffsetLocalName, ns, 0);
            var generation = ReadInteger(token, WsSecureConversationNames.GenerationLocalName, ns, 0);

            if (length.IsNull() || offset.IsNull() || generation.IsNull())
                return Unreadable();

            if (length!.Value < MinDerivedKeyLength || length.Value > MaxDerivedKeyLength || offset!.Value < 0 || generation!.Value < 0)
                return Unreadable();

            if (offset.Value > 0 && generation.Value > 0)
                return Unreadable();

            var effectiveOffset = generation.Value > 0 ? generation.Value * length.Value : offset.Value;
            if (effectiveOffset > WsSecurityKeyDerivation.MaxDerivedKeyLength)
                return Unreadable();

            var label = ChildText(token, WsSecureConversationNames.LabelLocalName, ns) ?? WsSecureConversationNames.DefaultLabel;

            var key = WsSecurityKeyDerivation.DeriveKey(secret, label, nonce, effectiveOffset, length.Value);

            return key.IsNull() ? Unreadable() : Result<byte[]>.Success(key);
        }

        /// <summary>
        ///     Binds the verified response to the request: the signed <c>wsa:RelatesTo</c> when the
        ///     request carried a message id, and the signed signature confirmations when the request's
        ///     mode demands them.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <param name="signedXml">The loaded, verified signature.</param>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <returns>
        ///     Success when the response binds, otherwise the <c>RelatesTo</c> or confirmation failure.
        /// </returns>
        private static IResult BindToRequest(XmlDocument document, WsuSignedXml signedXml, ConsumedKeyMaterial consumed)
        {
            if (consumed.MessageId.IsNotNull())
            {
                var relatesTo = WsSecurityResponseBinding.RequireRelatesTo(document, signedXml, consumed.MessageId);
                if (relatesTo.IsSuccess.IsFalse())
                    return relatesTo;
            }

            return consumed.RequireSignatureConfirmation
                ? RequireSignatureConfirmations(document, signedXml, consumed.SignatureValue)
                : Result.Success();
        }

        /// <summary>
        ///     Requires every WS-Security 1.1 <c>SignatureConfirmation</c> of the response to sit inside
        ///     the signature's coverage, and one to equal the request's primary signature value in fixed
        ///     time.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <param name="signedXml">The loaded, verified signature.</param>
        /// <param name="requestSignatureValue">The request's primary signature value.</param>
        /// <returns>
        ///     Success when a covered confirmation matches, otherwise a failed result.
        /// </returns>
        private static IResult RequireSignatureConfirmations(XmlDocument document, WsuSignedXml signedXml, byte[] requestSignatureValue)
        {
            var security = WsSecurityResponseInspection.LocateSecurityHeader(document);
            if (security.IsNull() || requestSignatureValue.IsNullOrEmptyEnumerable())
                return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_040);

            var signedIds = WsSecurityResponseInspection.SignedIds(signedXml);
            var confirmed = false;
            var count = 0;

            foreach (XmlNode child in security!.ChildNodes)
            {
                if (child is not XmlElement confirmation
                    || string.Equals(confirmation.LocalName, WsSecurity11Names.SignatureConfirmationLocalName, StringComparison.Ordinal).IsFalse()
                    || string.Equals(confirmation.NamespaceURI, WsSecurity11Names.Namespace, StringComparison.Ordinal).IsFalse())
                    continue;

                count++;

                if (WsSecurityResponseInspection.IsCoveredByReference(document, signedXml, confirmation, signedIds).IsFalse())
                    return WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_040);

                var echoed = TryDecodeBase64(confirmation.GetAttribute(WsSecurity11Names.ValueAttributeName));

                confirmed |= WsSecurityResponseBinding.FixedTimeEquals(echoed, requestSignatureValue);
            }

            return count > 0 && confirmed
                ? Result.Success()
                : WsSecurityResponseInspection.ValidationFailure(MessageCodes.V_SEC_040);
        }

        /// <summary>
        ///     Builds the keyed hash of a signature method the shape gate already accepted.
        /// </summary>
        /// <param name="signatureMethod">The signature method URI.</param>
        /// <param name="key">The derived key.</param>
        /// <returns>
        ///     The keyed hash.
        /// </returns>
        private static HMAC NewHmac(string signatureMethod, byte[] key)
            => string.Equals(signatureMethod, HmacSha256Method, StringComparison.Ordinal)
                ? new HMACSHA256(key)
                : new HMACSHA1(key);

        /// <summary>
        ///     Reads the text of a single child element, or null when absent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="localName">The child's local name.</param>
        /// <param name="ns">The child's namespace.</param>
        /// <returns>
        ///     The trimmed text, or null.
        /// </returns>
        private static string ChildText(XmlElement parent, string localName, string ns)
            => SoapXmlHelper.SingleChildElement(parent, localName, ns)?.InnerText.Trim();

        /// <summary>
        ///     Reads a non-negative integer child, applying a default when absent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="localName">The child's local name.</param>
        /// <param name="ns">The child's namespace.</param>
        /// <param name="defaultValue">The value when the child is absent.</param>
        /// <returns>
        ///     The value, or null when the child is present but not an integer.
        /// </returns>
        private static int? ReadInteger(XmlElement parent, string localName, string ns, int defaultValue)
        {
            var text = ChildText(parent, localName, ns);
            if (text.IsNull())
                return defaultValue;

            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;
        }

        /// <summary>
        ///     Decodes a base64 value without throwing.
        /// </summary>
        /// <param name="value">The value, or null.</param>
        /// <returns>
        ///     The decoded bytes, or null when absent or not base64.
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

        /// <summary>
        ///     The refusal of a derived key token this library cannot read.
        /// </summary>
        /// <returns>
        ///     The failed IResult&lt;byte[]&gt;.
        /// </returns>
        private static IResult<byte[]> Unreadable()
            => Result<byte[]>.Failure(MessageCodes.V_SEC_046.GetDescription(), Messages.GetValidationMessage(MessageCodes.V_SEC_046));

        /// <summary>
        ///     The refusal of a response carrying a key the client cannot unwrap.
        /// </summary>
        /// <returns>
        ///     The failed IResult.
        /// </returns>
        private static IResult DecryptionRefusal()
            => Result.Failure(MessageCodes.ER_SEC_DEC.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_DEC));

        /// <summary>
        ///     Builds a validation failure from a message code.
        /// </summary>
        /// <param name="code">The validation message code.</param>
        /// <param name="args">The message arguments.</param>
        /// <returns>
        ///     A failure carrying the code and its formatted message.
        /// </returns>
        private static IResult<SoapSignatureVerificationResult> Refuse(MessageCodes code, params object[] args)
            => Result<SoapSignatureVerificationResult>.Failure(code.GetDescription(), Messages.GetValidationMessage(code).TryFormatWith(args));
    }
}
