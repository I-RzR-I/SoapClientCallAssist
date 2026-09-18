// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 22:40
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="WsSecurityHeaderBuilder.Symmetric.cs" company="RzR SOFT & TECH">
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
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helpers;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     The symmetric concern of the header builder. It wraps a fresh secret in an encrypted key,
    ///     derives keys by P_SHA1 and hands the secret once to the request's key material.
    /// </summary>
    internal sealed partial class WsSecurityHeaderBuilder
    {
        /// <summary>
        ///     The length of the secret wrapped for the service, in bytes.
        /// </summary>
        private const int SymmetricSecretLength = 32;

        /// <summary>
        ///     The secret and the token reference the derived key tokens draw on, or null until the
        ///     encrypted key is emitted.
        /// </summary>
        private SymmetricKeySource _symmetricKeySource;

        /// <summary>
        ///     Emits the <c>xenc:EncryptedKey</c>, a fresh random secret wrapped with RSA-OAEP for the
        ///     service certificate's key and referenced by that certificate's SHA-1 thumbprint.
        /// </summary>
        partial void EmitEncryptedKey()
        {
            var binding = Options.SymmetricBinding;

            var secret = new byte[SymmetricSecretLength];

            using (var generator = RandomNumberGenerator.Create())
                generator.GetBytes(secret);

            var wrapped = WrapSymmetricSecret(binding.ServiceCertificate, secret);
            if (wrapped.IsSuccess.IsFalse())
            {
                Array.Clear(secret, 0, secret.Length);
                _hookOutcome = wrapped.ToBase();

                return;
            }

            var encryptedKey = _document.CreateElement(XmlEncryptionNames.Prefix, XmlEncryptionNames.EncryptedKeyLocalName, XmlEncryptionNames.Namespace);
            var encryptedKeyId = _ids.Next("ek");
            encryptedKey.SetAttribute(WsSecurityNames.IdLocalName, encryptedKeyId);

            var method = _document.CreateElement(XmlEncryptionNames.Prefix, XmlEncryptionNames.EncryptionMethodLocalName, XmlEncryptionNames.Namespace);
            method.SetAttribute(XmlEncryptionNames.AlgorithmAttributeName, XmlEncryptionNames.RsaOaepMgf1p);
            var digest = _document.CreateElement("ds", "DigestMethod", SignedXml.XmlDsigNamespaceUrl);
            digest.SetAttribute(XmlEncryptionNames.AlgorithmAttributeName, XmlEncryptionNames.Sha1DigestMethod);
            method.AppendChild(digest);
            encryptedKey.AppendChild(method);

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(SecurityTokenReferenceClause.KeyIdentifier(WsSecurity11Names.ThumbprintSha1ValueType, ServiceThumbprintSha1(binding.ServiceCertificate.RawData)));
            encryptedKey.AppendChild(_document.ImportNode(keyInfo.GetXml(), true));

            var cipherData = _document.CreateElement(XmlEncryptionNames.Prefix, XmlEncryptionNames.CipherDataLocalName, XmlEncryptionNames.Namespace);
            AppendTextChild(cipherData, XmlEncryptionNames.Prefix, XmlEncryptionNames.CipherValueLocalName, XmlEncryptionNames.Namespace, Convert.ToBase64String(wrapped.Response));
            encryptedKey.AppendChild(cipherData);

            _security.AppendChild(encryptedKey);

            _symmetricKeySource = new SymmetricKeySource(
                secret,
                SecurityTokenReferenceClause.Reference("#" + encryptedKeyId, WsSecurity11Names.EncryptedKeyTokenType, WsSecurity11Names.EncryptedKeyTokenType),
                binding.Version,
                binding.KeyDerivationLabel,
                binding.SignatureKeyLength,
                binding.EncryptionKeyLength);

            using (var sha1 = SHA1.Create())
                _symmetricKeySource.EncryptedKeySha1 = sha1.ComputeHash(wrapped.Response);

            _hookOutcome = Result.Success();
        }

        /// <summary>
        ///     Emits one <c>DerivedKeyToken</c> for the signature key and, when the build encrypts
        ///     anything, one for the encryption key, each with a fresh nonce; without a key source it
        ///     reports nothing.
        /// </summary>
        partial void EmitDerivedKeyTokens()
        {
            if (_symmetricKeySource.IsNull() || _symmetricKeySource!.HasSecret.IsFalse())
                return;

            _symmetricKeySource.SignatureNonce = NewNonce();
            _symmetricKeySource.SignatureTokenId = EmitDerivedKeyToken(_symmetricKeySource.SignatureNonce, _symmetricKeySource.SignatureKeyLength);

            if (EncryptsAnything())
            {
                _symmetricKeySource.EncryptionNonce = NewNonce();
                _symmetricKeySource.EncryptionTokenId = EmitDerivedKeyToken(_symmetricKeySource.EncryptionNonce, _symmetricKeySource.EncryptionKeyLength);
            }

            _hookOutcome = Result.Success();
        }

        /// <summary>
        ///     Provides the primary signature's HMAC key, derived from the secret under the signature
        ///     token's nonce, and points its <c>KeyInfo</c> at that token. The core zeroes the key once
        ///     the signature is computed.
        /// </summary>
        /// <param name="spec">The spec of the primary signature.</param>
        partial void ProvidePrimarySignatureKey(SignatureSpec spec)
        {
            if (_symmetricKeySource.IsNull() || _symmetricKeySource!.SignatureTokenId.IsNull())
                return;

            var key = _symmetricKeySource.DeriveSignatureKey();
            if (key.IsNull())
                return;

            spec.HmacKey = key;
            spec.KeyInfoClause = SecurityTokenReferenceClause.Reference(
                "#" + _symmetricKeySource.SignatureTokenId, WsSecureConversationNames.DerivedKeyTokenValueType(_symmetricKeySource.Version));

            _hookOutcome = Result.Success();
        }

        /// <summary>
        ///     Hands the build result the SHA-1 of the encrypted key and, outside a secure
        ///     conversation, the secret itself with its ownership, leaving the key source without it.
        /// </summary>
        /// <param name="encryptedKeySha1">Set to the SHA-1 of the encrypted key's cipher value.</param>
        /// <param name="sessionSecret">Set to the secret, ownership included.</param>
        partial void CollectSymmetricMaterial(ref byte[] encryptedKeySha1, ref byte[] sessionSecret)
        {
            if (_symmetricKeySource.IsNull())
                return;

            encryptedKeySha1 = _symmetricKeySource!.EncryptedKeySha1;

            if (_plan.Mode == SoapSecurityModeType.SecureConversation)
                return;

            sessionSecret = _symmetricKeySource.TakeSecret();
        }

        /// <summary>
        ///     Emits one <c>DerivedKeyToken</c> in the key source's WS-SecureConversation version.
        /// </summary>
        /// <param name="nonce">The token's raw nonce.</param>
        /// <param name="length">The derived key length, in bytes.</param>
        /// <returns>
        ///     The token's <c>wsu:Id</c>.
        /// </returns>
        private string EmitDerivedKeyToken(byte[] nonce, int length)
        {
            var ns = WsSecureConversationNames.Namespace(_symmetricKeySource.Version);

            var token = _document.CreateElement(WsSecureConversationNames.Prefix, WsSecureConversationNames.DerivedKeyTokenLocalName, ns);
            var tokenId = _ids.Stamp(_document, token, "dk");

            token.AppendChild(_document.ImportNode(_symmetricKeySource.TokenReference.GetXml(), true));

            AppendTextChild(token, WsSecureConversationNames.Prefix, WsSecureConversationNames.OffsetLocalName, ns, "0");
            AppendTextChild(token, WsSecureConversationNames.Prefix, WsSecureConversationNames.LengthLocalName, ns, length.ToString(CultureInfo.InvariantCulture));

            if (string.Equals(_symmetricKeySource.Label, WsSecureConversationNames.DefaultLabel, StringComparison.Ordinal).IsFalse())
                AppendTextChild(token, WsSecureConversationNames.Prefix, WsSecureConversationNames.LabelLocalName, ns, _symmetricKeySource.Label);

            var encoded = Convert.ToBase64String(nonce);
            AppendTextChild(token, WsSecureConversationNames.Prefix, WsSecureConversationNames.NonceLocalName, ns, encoded);
            _nonces.Add(encoded);

            _security.AppendChild(token);

            return tokenId;
        }

        /// <summary>
        ///     Determines whether the build encrypts anything.
        /// </summary>
        /// <returns>
        ///     True when the username token, the Body or the signature is to be encrypted.
        /// </returns>
        private bool EncryptsAnything()
            => _plan.EncryptUsernameToken
               || (Options.Encryption.IsNotNull() && (Options.Encryption!.EncryptSignature || Options.Encryption.EncryptBody));

        /// <summary>
        ///     Wraps the secret for the service certificate's RSA public key.
        /// </summary>
        /// <param name="serviceCertificate">The service certificate.</param>
        /// <param name="secret">The secret.</param>
        /// <returns>
        ///     The wrapped secret, or a failed result when the certificate carries no readable RSA key.
        /// </returns>
        private static IResult<byte[]> WrapSymmetricSecret(X509Certificate2 serviceCertificate, byte[] secret)
        {
            RSA publicKey;

            try
            {
                publicKey = serviceCertificate.GetRSAPublicKey();
            }
            catch (Exception ex)
            {
                return Result<byte[]>
                    .Failure(MessageCodes.ER_SEC_KEY.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_KEY))
                    .WithOptionalError(ex, "reading the service certificate's public key");
            }

            if (publicKey.IsNull())
                return Result<byte[]>.Failure(MessageCodes.V_SEC_039.GetDescription(), Messages.GetValidationMessage(MessageCodes.V_SEC_039));

            using (publicKey)
                return WsSecurityEncryptor.WrapKey(publicKey, secret);
        }

        /// <summary>
        ///     Computes the base64 SHA-1 thumbprint a <c>#ThumbprintSHA1</c> key identifier carries.
        /// </summary>
        /// <param name="certificateDer">The certificate's DER bytes.</param>
        /// <returns>
        ///     The base64 thumbprint.
        /// </returns>
        private static string ServiceThumbprintSha1(byte[] certificateDer)
        {
            using (var sha1 = SHA1.Create())
                return Convert.ToBase64String(sha1.ComputeHash(certificateDer));
        }

        /// <summary>
        ///     Builds a refusal under a validation code.
        /// </summary>
        /// <param name="code">The validation message code.</param>
        /// <returns>
        ///     A failed IResult.
        /// </returns>
        private static IResult SymmetricRefusal(MessageCodes code)
            => Result.Failure(code.GetDescription(), Messages.GetValidationMessage(code));
    }
}
