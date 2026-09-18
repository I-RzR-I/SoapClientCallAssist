// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-12 00:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="WsSecurityResponseDecryptor.cs" company="RzR SOFT & TECH">
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
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     Decrypts the Body of a symmetric-binding request's response and verifies it against the
    ///     request's key material, handing out the plaintext only once its signature verifies.
    /// </summary>
    internal static class WsSecurityResponseDecryptor
    {
        /// <summary>
        ///     The largest decrypted plaintext accepted when the caller sets no cap, in bytes.
        /// </summary>
        internal const int DefaultMaxPlaintextBytes = 4 * 1024 * 1024;

        /// <summary>
        ///     The AES block size, which is also the length of the initialisation vector carried in
        ///     front of the cipher text, in bytes.
        /// </summary>
        private const int BlockSize = 16;

        /// <summary>
        ///     The derived key length a token that states none has, in bytes.
        /// </summary>
        private const int DefaultDerivedKeyLength = 32;

        /// <summary>
        ///     The most encrypted elements accepted in the Security header: the signature and the
        ///     signature confirmations a service encrypts alongside it, one per request signature.
        /// </summary>
        private const int MaxSecurityHeaderParts = 8;

        /// <summary>
        ///     The local name of the wrapper the Body content is re-parsed inside.
        /// </summary>
        private const string ContentWrapperLocalName = "content";

        /// <summary>
        ///     The local name of the <c>ds:KeyInfo</c> element.
        /// </summary>
        private const string KeyInfoLocalName = "KeyInfo";

        /// <summary>
        ///     The local name of the XML Encryption <c>CipherReference</c> element.
        /// </summary>
        private const string CipherReferenceLocalName = "CipherReference";

        /// <summary>
        ///     The local name of the XML Encryption <c>CarriedKeyName</c> element.
        /// </summary>
        private const string CarriedKeyNameLocalName = "CarriedKeyName";

        /// <summary>
        ///     The local names no decrypted plaintext may carry, each of which would replace content the
        ///     request's checks were about to read.
        /// </summary>
        private static readonly string[] ForbiddenPlaintextLocalNames =
        {
            WsSecurityNames.EnvelopeLocalName,
            WsSecurityNames.HeaderLocalName,
            SoapContracts.BodyLocalName,
            WsSecurityNames.SecurityLocalName,
            WsSecurityNames.SignatureLocalName,
            XmlEncryptionNames.EncryptedKeyLocalName,
            XmlEncryptionNames.EncryptedDataLocalName
        };

        /// <summary>
        ///     The element names whose presence anywhere in a response refuses it before any key is
        ///     touched, none of which this library resolves.
        /// </summary>
        private static readonly (string LocalName, string Namespace)[] ForbiddenResponseElements =
        {
            (XmlEncryptionNames.EncryptedKeyLocalName, XmlEncryptionNames.Namespace),
            (CipherReferenceLocalName, XmlEncryptionNames.Namespace),
            (CarriedKeyNameLocalName, XmlEncryptionNames.Namespace),
            (XmlEncryptionNames.KeyReferenceLocalName, XmlEncryptionNames.Namespace),
            (WsSecurityNames.RetrievalMethodLocalName, SignedXml.XmlDsigNamespaceUrl)
        };

        /// <summary>
        ///     The namespaces a <c>DerivedKeyToken</c> may be written in.
        /// </summary>
        private static readonly string[] SecureConversationNamespaces =
        {
            WsSecureConversationNames.NamespaceFebruary2005,
            WsSecureConversationNames.NamespaceDecember2005
        };

        /// <summary>
        ///     The unqualified id attribute names, in the order the signature implementation tries them.
        ///     The WS-Security utility <c>wsu:Id</c> is checked separately.
        /// </summary>
        private static readonly string[] IdAttributeNames = { "Id", "id", "ID" };

        /// <summary>
        ///     Decrypts and verifies a response against the consumed material of the request it answers,
        ///     refusing a reflected nonce by name before anything else.
        /// </summary>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <param name="soapResponse">The raw response.</param>
        /// <returns>
        ///     Success carrying the decrypted, verified envelope, otherwise a failed result. A failure
        ///     carries no plaintext.
        /// </returns>
        internal static IResult<string> DecryptAndVerify(ConsumedKeyMaterial consumed, string soapResponse)
        {
            if (consumed.AllowDecryption.IsFalse())
                return Refuse(MessageCodes.V_SEC_058).Propagate<string>();

            if (consumed.SessionSecret.IsNullOrEmptyEnumerable()
                || WsSecuritySymmetricResponseVerifier.IsSecureConversation(consumed).IsFalse() && consumed.EncryptedKeySha1.IsNullOrEmptyEnumerable())
                return Refuse(MessageCodes.V_SEC_030).Propagate<string>();

            if (soapResponse.IsNullOrEmpty() || soapResponse.Length > SoapContracts.MaxDocumentCharacters)
                return Refuse(MessageCodes.V_SEC_005, SoapContracts.MaxDocumentCharacters).Propagate<string>();

            var loaded = SoapXmlDocumentLoader.Load(soapResponse, true, MessageCodes.ER_SEC_DOM);
            if (loaded.IsSuccess.IsFalse())
                return loaded.Propagate<string>();

            var document = loaded.Response;

            var reflected = WsSecurityResponseBinding.RefuseReflectedNonce(document, consumed.Nonces);
            if (reflected.IsSuccess.IsFalse())
                return reflected.Propagate<string>();

            var located = LocateEncryptedParts(document, consumed, out var parts, out var tokens);
            if (located.IsSuccess.IsFalse())
                return located.Propagate<string>();

            return DecryptThenVerify(document, parts, tokens, consumed);
        }

        /// <summary>
        ///     Determines whether a response carries encrypted content.
        /// </summary>
        /// <param name="document">The parsed response.</param>
        /// <returns>
        ///     True when the response carries an <c>EncryptedData</c> or a <c>ReferenceList</c>.
        /// </returns>
        internal static bool CarriesEncryptedContent(XmlDocument document)
            => document.GetElementsByTagName(XmlEncryptionNames.EncryptedDataLocalName, XmlEncryptionNames.Namespace).Count > 0
               || document.GetElementsByTagName(XmlEncryptionNames.ReferenceListLocalName, XmlEncryptionNames.Namespace).Count > 0;

        /// <summary>
        ///     Locates every encrypted part and runs every check decidable without a key: the single
        ///     <c>ReferenceList</c>, the derived key tokens, one <c>EncryptedData</c> per reference
        ///     keyed to the request's own key, and the plaintext cap.
        /// </summary>
        /// <param name="document">The parsed response.</param>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <param name="parts">The located parts, in reference order.</param>
        /// <param name="tokens">Every derived key token of the Security header.</param>
        /// <returns>
        ///     Success once every part is located, otherwise a failed result.
        /// </returns>
        private static IResult LocateEncryptedParts(
            XmlDocument document, ConsumedKeyMaterial consumed, out List<EncryptedPart> parts, out List<XmlElement> tokens)
        {
            parts = new List<EncryptedPart>();
            tokens = new List<XmlElement>();

            var security = WsSecurityResponseInspection.LocateSecurityHeader(document);
            var body = SoapXmlHelper.LocateSoapBody(document);
            if (security.IsNull() || body.IsNull())
                return Refuse(MessageCodes.V_SEC_059);

            foreach (var forbidden in ForbiddenResponseElements)
            {
                if (document.GetElementsByTagName(forbidden.LocalName, forbidden.Namespace).Count > 0)
                    return Refuse(MessageCodes.V_SEC_059);
            }

            var derivedKeyTokens = ReadDerivedKeyTokens(security, consumed.KeyDerivationLabel);
            if (derivedKeyTokens.IsNull())
                return Refuse(MessageCodes.V_SEC_046);

            tokens = derivedKeyTokens;

            if (document.GetElementsByTagName(XmlEncryptionNames.ReferenceListLocalName, XmlEncryptionNames.Namespace).Count != 1)
                return Refuse(MessageCodes.V_SEC_059);

            var referenceList = SoapXmlHelper.SingleChildElement(security, XmlEncryptionNames.ReferenceListLocalName, XmlEncryptionNames.Namespace);
            if (referenceList.IsNull())
                return Refuse(MessageCodes.V_SEC_059);

            var ids = ReadDataReferenceIds(referenceList);
            if (ids.IsNull()
                || document.GetElementsByTagName(XmlEncryptionNames.EncryptedDataLocalName, XmlEncryptionNames.Namespace).Count != ids!.Count)
                return Refuse(MessageCodes.V_SEC_059);

            var keyLength = KeyLengthOf(consumed.DataEncryptionAlgorithm);
            var signedXml = new WsuSignedXml(document) { Resolver = null };
            var cipherTextTotal = 0L;
            var bodyParts = 0;
            var securityParts = 0;

            foreach (var id in ids)
            {
                var encryptedData = ResolveEncryptedData(document, signedXml, id);
                if (encryptedData.IsNull())
                    return Refuse(MessageCodes.V_SEC_059);

                bool content;

                if (ReferenceEquals(encryptedData!.ParentNode, body))
                {
                    content = true;
                    bodyParts++;

                    if (IsOnlyContentOf(encryptedData, body).IsFalse())
                        return Refuse(MessageCodes.V_SEC_059);
                }
                else if (ReferenceEquals(encryptedData.ParentNode, security))
                {
                    content = false;
                    securityParts++;
                }
                else
                {
                    return Refuse(MessageCodes.V_SEC_059);
                }

                var expectedType = content ? XmlEncryptionNames.ContentType : XmlEncryptionNames.ElementType;
                if (string.Equals(encryptedData.GetAttribute(XmlEncryptionNames.TypeAttributeName), expectedType, StringComparison.Ordinal).IsFalse())
                    return Refuse(MessageCodes.V_SEC_059);

                if (HasOnlyChildren(encryptedData, XmlEncryptionNames.EncryptionMethodLocalName, KeyInfoLocalName, XmlEncryptionNames.CipherDataLocalName).IsFalse())
                    return Refuse(MessageCodes.V_SEC_059);

                var method = SoapXmlHelper.SingleChildElement(encryptedData, XmlEncryptionNames.EncryptionMethodLocalName, XmlEncryptionNames.Namespace);
                if (method.IsNull()
                    || string.Equals(method!.GetAttribute(XmlEncryptionNames.AlgorithmAttributeName), consumed.DataEncryptionAlgorithm, StringComparison.Ordinal).IsFalse())
                    return Refuse(MessageCodes.V_SEC_059);

                var keyed = ResolveDerivedKeyToken(document, encryptedData, consumed, out var token);
                if (keyed.IsSuccess.IsFalse())
                    return keyed;

                if (DerivedKeyLengthOf(token) != keyLength)
                    return Refuse(MessageCodes.V_SEC_059);

                var cipherText = ReadCipherText(encryptedData);
                if (cipherText.IsNull())
                    return Refuse(MessageCodes.V_SEC_059);

                cipherTextTotal += cipherText!.Length - BlockSize;
                if (cipherTextTotal - 1 > consumed.MaxPlaintextBytes)
                    return Refuse(MessageCodes.V_SEC_054, consumed.MaxPlaintextBytes);

                parts.Add(new EncryptedPart(encryptedData, token, cipherText, content));
            }

            return bodyParts <= 1 && securityParts <= MaxSecurityHeaderParts ? Result.Success() : Refuse(MessageCodes.V_SEC_059);
        }

        /// <summary>
        ///     Reads every derived key token that is a direct child of the Security header, requiring
        ///     each to carry the request's derivation label and a nonce of the one accepted length.
        /// </summary>
        /// <param name="security">The Security header.</param>
        /// <param name="requestLabel">The request's derivation label.</param>
        /// <returns>
        ///     The tokens, or null when any token is not in that shape.
        /// </returns>
        private static List<XmlElement> ReadDerivedKeyTokens(XmlElement security, string requestLabel)
        {
            var tokens = new List<XmlElement>();

            foreach (XmlNode child in security.ChildNodes)
            {
                if (child is not XmlElement token
                    || string.Equals(token.LocalName, WsSecureConversationNames.DerivedKeyTokenLocalName, StringComparison.Ordinal).IsFalse()
                    || Array.IndexOf(SecureConversationNamespaces, token.NamespaceURI) < 0)
                    continue;

                if (WsSecurityResponseBinding.RequireRequestDerivationShape(token, requestLabel).IsSuccess.IsFalse())
                    return null;

                tokens.Add(token);
            }

            return tokens;
        }

        /// <summary>
        ///     Reads the ids the reference list names: one plain same-document fragment per
        ///     <c>DataReference</c>, no other child, no duplicate, at least one.
        /// </summary>
        /// <param name="referenceList">The reference list.</param>
        /// <returns>
        ///     The ids, or null when the list is not in that shape.
        /// </returns>
        private static List<string> ReadDataReferenceIds(XmlElement referenceList)
        {
            var ids = new List<string>();

            foreach (XmlNode child in referenceList.ChildNodes)
            {
                if (child is not XmlElement reference)
                    continue;

                if (IsNamed(reference, XmlEncryptionNames.DataReferenceLocalName, XmlEncryptionNames.Namespace).IsFalse())
                    return null;

                var uri = reference.GetAttribute(WsSecurityNames.UriAttributeName);
                if (WsSecurityResponseInspection.IsPlainSameDocumentFragment(uri).IsFalse())
                    return null;

                var id = uri.Substring(1);
                if (ids.Contains(id))
                    return null;

                ids.Add(id);
            }

            return ids.Count == 0 ? null : ids;
        }

        /// <summary>
        ///     Resolves the single <c>EncryptedData</c> an id names, requiring the signature
        ///     implementation to resolve the same id to the same element and refusing any id more than
        ///     one element claims.
        /// </summary>
        /// <param name="document">The parsed response.</param>
        /// <param name="signedXml">The signature implementation bound to the document.</param>
        /// <param name="id">The id a data reference names.</param>
        /// <returns>
        ///     The <c>EncryptedData</c>, or null when the id resolves to nothing, to several elements,
        ///     to a non-<c>EncryptedData</c>, or to a different element than the signature resolves. 
        /// </returns>
        private static XmlElement ResolveEncryptedData(XmlDocument document, WsuSignedXml signedXml, string id)
        {
            XmlElement found = null;

            foreach (XmlNode node in document.GetElementsByTagName("*"))
            {
                if (node is not XmlElement element || CarriesId(element, id).IsFalse())
                    continue;

                if (found.IsNotNull())
                    return null;

                found = element;
            }

            if (found.IsNull() || IsNamed(found, XmlEncryptionNames.EncryptedDataLocalName, XmlEncryptionNames.Namespace).IsFalse())
                return null;

            try
            {
                return ReferenceEquals(signedXml.GetIdElement(document, id), found) ? found : null;
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        /// <summary>
        ///     Follows an <c>EncryptedData</c>'s <c>KeyInfo</c> through one
        ///     <c>SecurityTokenReference</c> and <c>Reference</c> to the same-document
        ///     <c>DerivedKeyToken</c> keyed to the request's own key. Nothing is derived here.
        /// </summary>
        /// <param name="document">The parsed response.</param>
        /// <param name="encryptedData">The encrypted part.</param>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <param name="token">[out] The derived key token, or null when refused.</param>
        /// <returns>
        ///     Success carrying the token, otherwise a failed result.
        /// </returns>
        private static IResult ResolveDerivedKeyToken(XmlDocument document, XmlElement encryptedData, 
            ConsumedKeyMaterial consumed, out XmlElement token)
        {
            token = null;

            var keyInfo = SoapXmlHelper.SingleChildElement(encryptedData, KeyInfoLocalName, SignedXml.XmlDsigNamespaceUrl);
            if (keyInfo.IsNull() || HasOnlyChildren(keyInfo, WsSecurityNames.SecurityTokenReferenceLocalName).IsFalse())
                return Refuse(MessageCodes.V_SEC_059);

            var tokenReference = SoapXmlHelper.SingleChildElement(keyInfo, WsSecurityNames.SecurityTokenReferenceLocalName, WsSecurityNames.WsseNamespace);
            if (tokenReference.IsNull() || HasOnlyChildren(tokenReference, WsSecurityNames.ReferenceLocalName).IsFalse())
                return Refuse(MessageCodes.V_SEC_059);

            var reference = SoapXmlHelper.SingleChildElement(tokenReference, WsSecurityNames.ReferenceLocalName, WsSecurityNames.WsseNamespace);
            if (reference.IsNull())
                return Refuse(MessageCodes.V_SEC_059);

            var uri = reference!.GetAttribute(WsSecurityNames.UriAttributeName);
            if (WsSecurityResponseInspection.IsPlainSameDocumentFragment(uri).IsFalse())
                return Refuse(MessageCodes.V_SEC_059);

            var located = WsSecuritySymmetricResponseVerifier.LocateDerivedKeyToken(document, uri.Substring(1));
            if (located.IsNull())
                return Refuse(MessageCodes.V_SEC_046);

            var valueType = reference.GetAttribute(WsSecurityNames.ValueTypeAttributeName);
            if (valueType.IsPresent()
                && string.Equals(valueType, located!.NamespaceURI + WsSecureConversationNames.DerivedKeyTokenValueTypeSuffix, StringComparison.Ordinal).IsFalse())
                return Refuse(MessageCodes.V_SEC_046);

            var keyed = WsSecuritySymmetricResponseVerifier.IsSecureConversation(consumed)
                ? WsSecuritySymmetricResponseVerifier.RequireKeyedToOurSct(located, consumed.SctIdentifier)
                : WsSecuritySymmetricResponseVerifier.RequireKeyedToOurEncryptedKey(located, consumed.EncryptedKeySha1);
            if (keyed.IsSuccess.IsFalse())
                return keyed;

            token = located;

            return Result.Success();
        }

        /// <summary>
        ///     Reads the cipher text of an encrypted part: the one <c>CipherValue</c> inside the one
        ///     <c>CipherData</c>, base64, at least an initialisation vector and one block long, and a
        ///     whole number of blocks.
        /// </summary>
        /// <param name="encryptedData">The encrypted part.</param>
        /// <returns>
        ///     The cipher text with its vector in front, or null when it is not in that shape.
        /// </returns>
        private static byte[] ReadCipherText(XmlElement encryptedData)
        {
            var cipherData = SoapXmlHelper.SingleChildElement(encryptedData, XmlEncryptionNames.CipherDataLocalName, XmlEncryptionNames.Namespace);
            if (cipherData.IsNull() || HasOnlyChildren(cipherData, XmlEncryptionNames.CipherValueLocalName).IsFalse())
                return null;

            var cipherValue = SoapXmlHelper.SingleChildElement(cipherData, XmlEncryptionNames.CipherValueLocalName, XmlEncryptionNames.Namespace);
            var cipherText = cipherValue.IsNull() ? null : TryDecodeBase64(cipherValue!.InnerText);

            return cipherText.IsNotNull() && cipherText!.Length >= 2 * BlockSize && cipherText.Length % BlockSize == 0 ? cipherText : null;
        }

        /// <summary>
        ///     Derives the keys, decrypts and gates every plaintext, re-inserts them and verifies the
        ///     signature, running every stage to its end and zeroing each key and plaintext.
        /// </summary>
        /// <param name="document">The parsed response.</param>
        /// <param name="parts">The located parts.</param>
        /// <param name="tokens">Every derived key token of the Security header.</param>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <returns>
        ///     Success carrying the decrypted, verified envelope, otherwise a failed result.
        /// </returns>
        private static IResult<string> DecryptThenVerify(XmlDocument document, List<EncryptedPart> parts,
            List<XmlElement> tokens, ConsumedKeyMaterial consumed)
        {
            var keys = new Dictionary<XmlElement, byte[]>();
            var plaintexts = new List<byte[]>();
            var failure = new DecryptionStages();

            try
            {
                failure.Record(DecryptionStages.KeyDerivation, DeriveKeys(tokens, consumed, keys).IsFalse());

                var plaintextTotal = 0L;

                foreach (var part in parts)
                {
                    var plaintext = TryDecryptCipherText(keys, part, consumed.DataEncryptionAlgorithm);
                    plaintexts.Add(plaintext);

                    plaintextTotal += plaintext.Length;
                    failure.Record(DecryptionStages.CipherText, plaintext.Length == 0);
                    failure.Record(DecryptionStages.PlaintextSize, plaintextTotal > consumed.MaxPlaintextBytes);
                }

                var replacements = new List<XmlNode[]>();

                for (var index = 0; index < parts.Count; index++)
                {
                    var nodes = TryParsePlaintext(document, parts[index], plaintexts[index], failure);
                    failure.Record(DecryptionStages.PlaintextContent, nodes.IsNull());
                    replacements.Add(nodes);
                }

                if (failure.Failed.IsFalse())
                {
                    for (var index = 0; index < parts.Count; index++)
                        Replace(parts[index], replacements[index]);
                }

                var verified = WsSecuritySymmetricResponseVerifier.Verify(consumed, document);
                failure.Record(DecryptionStages.Signature, verified.IsSuccess.IsFalse());

                return failure.Failed ? DecryptionFailure(consumed, failure.Stage) : Result<string>.Success(document.OuterXml);
            }
            catch (Exception)
            {
                return DecryptionFailure(consumed, failure.Stage ?? DecryptionStages.CipherText);
            }
            finally
            {
                foreach (var key in keys.Values)
                    Array.Clear(key, 0, key.Length);

                foreach (var plaintext in plaintexts)
                    Array.Clear(plaintext, 0, plaintext.Length);
            }
        }

        /// <summary>
        ///     Derives the key of every derived key token and refuses any equal, in fixed time, to a key
        ///     the request itself derived.
        /// </summary>
        /// <param name="tokens">Every derived key token of the Security header.</param>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <param name="keys">The keys by token.</param>
        /// <returns>
        ///     True when every key derived and none reflects the request.
        /// </returns>
        private static bool DeriveKeys(List<XmlElement> tokens, ConsumedKeyMaterial consumed, Dictionary<XmlElement, byte[]> keys)
        {
            var refused = false;

            foreach (var token in tokens)
            {
                var derived = WsSecuritySymmetricResponseVerifier.DeriveKey(token, consumed.SessionSecret);
                if (derived.IsSuccess.IsFalse())
                {
                    refused = true;

                    continue;
                }

                keys[token] = derived.Response;

                refused |= WsSecurityResponseBinding.RefuseReflectedDerivedKey(derived.Response, consumed.RequestDerivedKeys).IsSuccess.IsFalse();
            }

            return refused.IsFalse();
        }

        /// <summary>
        ///     Decrypts one cipher text with a raw AES-CBC transform under its leading vector, sized by
        ///     the announced algorithm and unpadded as ISO 10126. A part that cannot be decrypted yields
        ///     an empty plaintext.
        /// </summary>
        /// <param name="keys">The keys by token.</param>
        /// <param name="part">The encrypted part.</param>
        /// <param name="algorithm">The announced data encryption algorithm.</param>
        /// <returns>
        ///     The plaintext, or an empty array when the part could not be decrypted.
        /// </returns>
        private static byte[] TryDecryptCipherText(Dictionary<XmlElement, byte[]> keys, EncryptedPart part, string algorithm)
        {
            if (keys.TryGetValue(part.Token, out var key).IsFalse() || key.Length != KeyLengthOf(algorithm))
                return new byte[0];

            var iv = new byte[BlockSize];
            Buffer.BlockCopy(part.CipherText, 0, iv, 0, BlockSize);

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeyLengthOf(algorithm) * 8;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.ISO10126;

                    using (var decryptor = aes.CreateDecryptor(key, iv))
                        return decryptor.TransformFinalBlock(part.CipherText, BlockSize, part.CipherText.Length - BlockSize);
                }
            }
            catch (CryptographicException)
            {
                return new byte[0];
            }
        }

        /// <summary>
        ///     Re-parses a plaintext through the hardened reader and gates its content, wrapping a Body
        ///     content in the Body's in-scope namespaces and requiring a Security header element to be
        ///     one <c>ds:Signature</c> or <c>wsse11:SignatureConfirmation</c>.
        /// </summary>
        /// <param name="document">The response document the nodes are imported into.</param>
        /// <param name="part">The encrypted part.</param>
        /// <param name="plaintext">The decrypted bytes.</param>
        /// <param name="failure">The stage record a refusal is noted in.</param>
        /// <returns>
        ///     The imported nodes, or null when the plaintext is refused.
        /// </returns>
        private static XmlNode[] TryParsePlaintext(XmlDocument document, EncryptedPart part, byte[] plaintext, DecryptionStages failure)
        {
            string text;

            try
            {
                text = DecodeUtf8(plaintext);
            }
            catch (Exception)
            {
                failure.Record(DecryptionStages.PlaintextEncoding, true);

                return null;
            }

            XmlDocument parsed;

            try
            {
                parsed = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };

                using (var reader = SoapXmlDocumentLoader.CreateReader(part.Content ? WrapContent(part.Element.ParentNode as XmlElement, text) : text, true))
                    parsed.Load(reader);
            }
            catch (Exception)
            {
                failure.Record(DecryptionStages.PlaintextParse, true);

                return null;
            }

            var root = parsed.DocumentElement;
            if (root.IsNull() || CarriesForbiddenElement(root!.ChildNodes))
                return null;

            if (part.Content)
            {
                var nodes = new List<XmlNode>();

                foreach (XmlNode child in root.ChildNodes)
                    nodes.Add(document.ImportNode(child, true));

                return nodes.ToArray();
            }

            return IsNamed(root, WsSecurityNames.SignatureLocalName, SignedXml.XmlDsigNamespaceUrl)
                   || IsNamed(root, WsSecurity11Names.SignatureConfirmationLocalName, WsSecurity11Names.Namespace)
                ? new XmlNode[] { document.ImportNode(root, true) }
                : null;
        }

        /// <summary>
        ///     Re-inserts a decrypted part where its <c>EncryptedData</c> stood, replacing it for a
        ///     Security header element or inserting a Body content's nodes before it. The Body element
        ///     and its <c>wsu:Id</c> are never touched.
        /// </summary>
        /// <param name="part">The encrypted part.</param>
        /// <param name="nodes">The imported plaintext nodes.</param>
        private static void Replace(EncryptedPart part, XmlNode[] nodes)
        {
            var parent = part.Element.ParentNode;

            if (part.Content.IsFalse())
            {
                parent!.ReplaceChild(nodes[0], part.Element);

                return;
            }

            foreach (var node in nodes)
                parent!.InsertBefore(node, part.Element);

            parent!.RemoveChild(part.Element);
        }

        /// <summary>
        ///     Decodes a plaintext as UTF-8, refusing any byte sequence that is not, after dropping a
        ///     leading byte order mark.
        /// </summary>
        /// <param name="plaintext">The decrypted bytes.</param>
        /// <returns>
        ///     The text.
        /// </returns>
        private static string DecodeUtf8(byte[] plaintext)
        {
            var offset = plaintext.Length >= 3 && plaintext[0] == 0xEF && plaintext[1] == 0xBB && plaintext[2] == 0xBF ? 3 : 0;

            return new UTF8Encoding(false, true).GetString(plaintext, offset, plaintext.Length - offset);
        }

        /// <summary>
        ///     Wraps a Body content in an element declaring every namespace in scope at the Body, the
        ///     nearest declaration of each prefix winning.
        /// </summary>
        /// <param name="body">The Body the content belongs to.</param>
        /// <param name="content">The plaintext content.</param>
        /// <returns>
        ///     The wrapped content.
        /// </returns>
        private static string WrapContent(XmlElement body, string content)
        {
            var declared = new HashSet<string>(StringComparer.Ordinal);
            var wrapper = new StringBuilder("<" + ContentWrapperLocalName);

            for (var node = (XmlNode)body; node is XmlElement element; node = node.ParentNode)
            {
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    var isDefault = string.Equals(attribute.Name, "xmlns", StringComparison.Ordinal);
                    if (isDefault.IsFalse() && string.Equals(attribute.Prefix, "xmlns", StringComparison.Ordinal).IsFalse())
                        continue;

                    if (declared.Add(attribute.Name).IsFalse())
                        continue;

                    wrapper.Append(' ')
                        .Append(attribute.Name)
                        .Append("=\"")
                        .Append(EscapeAttributeValue(attribute.Value))
                        .Append('"');
                }
            }

            return wrapper.Append('>').Append(content).Append("</").Append(ContentWrapperLocalName).Append('>').ToString();
        }

        /// <summary>
        ///     Escapes a namespace URI for an attribute value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The escaped value.
        /// </returns>
        private static string EscapeAttributeValue(string value)
            => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace("\"", "&quot;");

        /// <summary>
        ///     Determines whether any element in a node list, at any depth, carries a forbidden name.
        /// </summary>
        /// <param name="nodes">The nodes to walk.</param>
        /// <returns>
        ///     True when a forbidden element is present.
        /// </returns>
        private static bool CarriesForbiddenElement(XmlNodeList nodes)
        {
            foreach (XmlNode node in nodes)
            {
                if (node is not XmlElement element)
                    continue;

                if (Array.IndexOf(ForbiddenPlaintextLocalNames, element.LocalName) >= 0 || CarriesForbiddenElement(element.ChildNodes))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Determines whether an element is the only content of its parent, allowing whitespace
        ///     text around it and nothing else.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>
        ///     True when nothing but whitespace accompanies the element.
        /// </returns>
        private static bool IsOnlyContentOf(XmlElement element, XmlElement parent)
        {
            foreach (XmlNode child in parent.ChildNodes)
            {
                if (ReferenceEquals(child, element) || child is XmlWhitespace || child is XmlSignificantWhitespace)
                    continue;

                if (child is XmlText text && string.IsNullOrWhiteSpace(text.Value))
                    continue;

                return false;
            }

            return true;
        }

        /// <summary>
        ///     Determines whether every element child of a parent carries one of the permitted local
        ///     names.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="localNames">The permitted local names.</param>
        /// <returns>
        ///     True when no other element child is present.
        /// </returns>
        private static bool HasOnlyChildren(XmlElement parent, params string[] localNames)
        {
            foreach (XmlNode child in parent.ChildNodes)
            {
                if (child is XmlElement element && Array.IndexOf(localNames, element.LocalName) < 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Determines whether an element claims an id through the unqualified <c>Id</c>, <c>id</c> or
        ///     <c>ID</c>, or the WS-Security utility <c>wsu:Id</c>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="id">The id.</param>
        /// <returns>
        ///     True when the element claims the id.
        /// </returns>
        private static bool CarriesId(XmlElement element, string id)
        {
            foreach (var name in IdAttributeNames)
            {
                if (element.HasAttribute(name) && string.Equals(element.GetAttribute(name), id, StringComparison.Ordinal))
                    return true;
            }

            return element.HasAttribute(WsSecurityNames.IdLocalName, WsSecurityNames.WsuNamespace)
                   && string.Equals(element.GetAttribute(WsSecurityNames.IdLocalName, WsSecurityNames.WsuNamespace), id, StringComparison.Ordinal);
        }

        /// <summary>
        ///     Resolves the key length a data encryption algorithm takes.
        /// </summary>
        /// <param name="algorithm">The algorithm URI.</param>
        /// <returns>
        ///     The key length in bytes, or zero for an algorithm this library does not decrypt with.
        /// </returns>
        private static int KeyLengthOf(string algorithm)
        {
            if (string.Equals(algorithm, XmlEncryptionNames.Aes128Cbc, StringComparison.Ordinal))
                return 16;

            if (string.Equals(algorithm, XmlEncryptionNames.Aes192Cbc, StringComparison.Ordinal))
                return 24;

            return string.Equals(algorithm, XmlEncryptionNames.Aes256Cbc, StringComparison.Ordinal) ? 32 : 0;
        }

        /// <summary>
        ///     Reads the key length a derived key token states, applying the specification default when
        ///     it states none.
        /// </summary>
        /// <param name="token">The derived key token.</param>
        /// <returns>
        ///     The length in bytes, or -1 when the token's length is not a non-negative integer.
        /// </returns>
        private static int DerivedKeyLengthOf(XmlElement token)
        {
            var text = ChildText(token, WsSecureConversationNames.LengthLocalName);
            if (text.IsNull())
                return DefaultDerivedKeyLength;

            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var length) ? length : -1;
        }

        /// <summary>
        ///     Reads the text of a single child element in the parent's own namespace.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="localName">The child's local name.</param>
        /// <returns>
        ///     The trimmed text, or null when absent or ambiguous.
        /// </returns>
        private static string ChildText(XmlElement parent, string localName)
            => SoapXmlHelper.SingleChildElement(parent, localName, parent.NamespaceURI)?.InnerText.Trim();

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
        ///     Determines whether an element carries a qualified name.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="localName">The local name.</param>
        /// <param name="ns">The namespace.</param>
        /// <returns>
        ///     True when both match ordinally.
        /// </returns>
        private static bool IsNamed(XmlElement element, string localName, string ns)
            => string.Equals(element.LocalName, localName, StringComparison.Ordinal)
               && string.Equals(element.NamespaceURI, ns, StringComparison.Ordinal);

        /// <summary>
        ///     Builds the uniform failure of everything past key derivation, naming the failed stage in
        ///     the message text only when the request's policy opted in to
        ///     <see cref="Dto.Public.SoapVerificationPolicyDto.DiagnosticDetail" />.
        /// </summary>
        /// <param name="consumed">The request's consumed key material.</param>
        /// <param name="stage">The first stage that failed, or null when unknown.</param>
        /// <returns>
        ///     The failed IResult&lt;string&gt;.
        /// </returns>
        private static IResult<string> DecryptionFailure(ConsumedKeyMaterial consumed, string stage)
        {
            var message = Messages.GetErrorMessage(MessageCodes.ER_SEC_DEC);

            if (consumed.Policy.IsNotNull() && consumed.Policy!.DiagnosticDetail && stage.IsNotNull())
                message = message + " " + DecryptionStages.Describe(stage);

            return Result<string>.Failure(MessageCodes.ER_SEC_DEC.GetDescription(), message);
        }

        /// <summary>
        ///     Builds a validation refusal from a message code.
        /// </summary>
        /// <param name="code">The validation message code.</param>
        /// <param name="args">The message arguments.</param>
        /// <returns>
        ///     A failure carrying the code and its formatted message.
        /// </returns>
        private static IResult Refuse(MessageCodes code, params object[] args)
            => Result.Failure(code.GetDescription(), Messages.GetValidationMessage(code).TryFormatWith(args));
    }
}
