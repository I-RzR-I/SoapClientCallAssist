// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 22:40
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 02:30
//  ***********************************************************************
//  <copyright file="WsSecurityEncryptor.cs" company="RzR SOFT & TECH">
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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     Wraps a symmetric secret for a service's RSA key and replaces an element or its content in
    ///     place with an <c>xenc:EncryptedData</c> under a fresh AES-CBC vector. Nothing here
    ///     decrypts.
    /// </summary>
    internal static class WsSecurityEncryptor
    {
        /// <summary>
        ///     The shortest RSA key a Body may be encrypted for, in bits.
        /// </summary>
        private const int MinRecipientRsaKeyBits = 2048;

        /// <summary>
        ///     Resolves the key length a data encryption algorithm takes.
        /// </summary>
        /// <param name="algorithm">The algorithm.</param>
        /// <returns>
        ///     The key length, in bytes.
        /// </returns>
        internal static int KeyLengthOf(SoapDataEncryptionAlgorithmType algorithm)
        {
            switch (algorithm)
            {
                case SoapDataEncryptionAlgorithmType.Aes128Cbc:
                    return 16;
                case SoapDataEncryptionAlgorithmType.Aes192Cbc:
                    return 24;
                default:
                    return 32;
            }
        }

        /// <summary>
        ///     Resolves the key length a data encryption algorithm URI takes.
        /// </summary>
        /// <param name="algorithm">The algorithm URI.</param>
        /// <returns>
        ///     The key length in bytes, or zero for an algorithm this library does not encrypt with.
        /// </returns>
        internal static int KeyLengthOf(string algorithm)
        {
            if (string.Equals(algorithm, XmlEncryptionNames.Aes128Cbc, StringComparison.Ordinal))
                return 16;

            if (string.Equals(algorithm, XmlEncryptionNames.Aes192Cbc, StringComparison.Ordinal))
                return 24;

            return string.Equals(algorithm, XmlEncryptionNames.Aes256Cbc, StringComparison.Ordinal) ? 32 : 0;
        }

        /// <summary>
        ///     Checks that a service certificate has an RSA key of at least 2048 bits, is valid now
        ///     within the skew, allows key encipherment and has a public key distinct from the signing
        ///     certificate's.
        /// </summary>
        /// <param name="recipient">The service certificate the secret is wrapped for.</param>
        /// <param name="signingCertificate">The caller's signing certificate, or null.</param>
        /// <param name="clockSkew">The tolerance applied to both ends of the validity window.</param>
        /// <returns>
        ///     Success when the certificate is fit, otherwise a failed result.
        /// </returns>
        internal static IResult ValidateRecipientCertificate(X509Certificate2 recipient,
            X509Certificate2 signingCertificate, TimeSpan clockSkew)
        {
            if (recipient.IsNull())
                return RecipientRefusal();

            try
            {
                using (var publicKey = recipient!.GetRSAPublicKey())
                {
                    if (publicKey.IsNull() || publicKey!.KeySize < MinRecipientRsaKeyBits)
                        return RecipientRefusal();
                }

                var now = DateTimeOffset.UtcNow;
                var skew = clockSkew < TimeSpan.Zero ? TimeSpan.Zero : clockSkew;
                var notBefore = new DateTimeOffset(recipient.NotBefore.ToUniversalTime());
                var notAfter = new DateTimeOffset(recipient.NotAfter.ToUniversalTime());

                if (now + skew < notBefore || now - skew > notAfter)
                    return RecipientRefusal();

                foreach (var extension in recipient.Extensions)
                {
                    if (extension is X509KeyUsageExtension keyUsage && (keyUsage.KeyUsages & X509KeyUsageFlags.KeyEncipherment) == 0)
                        return RecipientRefusal();
                }

                return X509KeyMatch.SharesPublicKey(recipient, signingCertificate) ? RecipientRefusal() : Result.Success();
            }
            catch (Exception)
            {
                return RecipientRefusal();
            }
        }

        /// <summary>
        ///     Wraps a secret for a service's RSA public key with RSA-OAEP over MGF1 and SHA-1.
        /// </summary>
        /// <param name="publicKey">The service certificate's RSA public key.</param>
        /// <param name="secret">The secret to wrap.</param>
        /// <returns>
        ///     The wrapped secret, or a failed result when the encryption throws.
        /// </returns>
        internal static IResult<byte[]> WrapKey(RSA publicKey, byte[] secret)
        {
            try
            {
                return Result<byte[]>.Success(publicKey.Encrypt(secret, RSAEncryptionPadding.OaepSHA1));
            }
            catch (Exception ex)
            {
                return Result<byte[]>
                    .Failure(MessageCodes.ER_SEC_KEY.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_KEY))
                    .WithOptionalError(ex, "wrapping the symmetric secret for the service certificate");
            }
        }

        /// <summary>
        ///     Replaces an element in place with an <c>xenc:EncryptedData</c> of type <c>#Element</c>
        ///     carrying its AES-CBC cipher text, including the namespace declarations it needs to stand
        ///     alone.
        /// </summary>
        /// <param name="document">The message document.</param>
        /// <param name="target">The attached element to encrypt.</param>
        /// <param name="key">The AES key, whose length must match the algorithm.</param>
        /// <param name="algorithm">The data encryption algorithm URI.</param>
        /// <param name="keyInfoClause">The clause naming the key the receiver decrypts with.</param>
        /// <param name="id">
        ///     The plain <c>Id</c> stamped on the <c>EncryptedData</c>, as the reference list names it.
        /// </param>
        /// <returns>
        ///     The attached <c>EncryptedData</c>, or a failed result for a detached element, a key or
        ///     algorithm mismatch or a failed encryption.
        /// </returns>
        internal static IResult<XmlElement> EncryptElementInPlace(XmlDocument document, XmlElement target, 
            byte[] key, string algorithm, KeyInfoClause keyInfoClause, string id)
        {
            if (target.IsNull() || target!.ParentNode.IsNull())
                return EncryptionFailure(null);

            var encrypted = Encrypt(document, target, key, algorithm, keyInfoClause, id);
            if (encrypted.IsSuccess.IsFalse())
                return encrypted;

            target.ParentNode!.ReplaceChild(encrypted.Response, target);

            return encrypted;
        }

        /// <summary>
        ///     Replaces the content of an element, the SOAP Body, with one <c>xenc:EncryptedData</c> of
        ///     type <c>#Content</c> carrying its AES-CBC cipher text. The element and its
        ///     <c>wsu:Id</c> stay in place.
        /// </summary>
        /// <param name="document">The message document.</param>
        /// <param name="target">The attached element whose content is encrypted.</param>
        /// <param name="key">The AES key.</param>
        /// <param name="algorithm">The data encryption algorithm URI.</param>
        /// <param name="keyInfoClause">The clause naming the key the receiver decrypts with.</param>
        /// <param name="id">
        ///     The plain <c>Id</c> stamped on the <c>EncryptedData</c>, as the reference list names it.
        /// </param>
        /// <returns>
        ///     The attached <c>EncryptedData</c>, or a failed result for a null element, a key or
        ///     algorithm mismatch or a failed encryption.
        /// </returns>
        internal static IResult<XmlElement> EncryptContentInPlace(XmlDocument document, XmlElement target, 
            byte[] key, string algorithm, KeyInfoClause keyInfoClause, string id)
        {
            if (target.IsNull())
                return EncryptionFailure(null);

            var encrypted = Encrypt(document, target, key, algorithm, keyInfoClause, id, true);
            if (encrypted.IsSuccess.IsFalse())
                return encrypted;

            while (target!.FirstChild.IsNotNull())
                target.RemoveChild(target.FirstChild);

            target.AppendChild(encrypted.Response);

            return encrypted;
        }

        /// <summary>
        ///     Builds the <c>xenc:EncryptedData</c> of an element, or of its content, without attaching
        ///     it.
        /// </summary>
        /// <param name="document">The message document.</param>
        /// <param name="target">The element to encrypt.</param>
        /// <param name="key">The AES key.</param>
        /// <param name="algorithm">The data encryption algorithm URI.</param>
        /// <param name="keyInfoClause">The clause naming the decryption key.</param>
        /// <param name="id">The plain <c>Id</c> of the <c>EncryptedData</c>.</param>
        /// <param name="content">
        ///     (Optional) True to encrypt the element's content, false to encrypt the element.
        /// </param>
        /// <returns>
        ///     The unattached <c>EncryptedData</c>, or a failed result for a key or algorithm mismatch
        ///     or when the cipher throws.
        /// </returns>
        private static IResult<XmlElement> Encrypt(XmlDocument document, XmlElement target, byte[] key,
            string algorithm, KeyInfoClause keyInfoClause, string id, bool content = false)
        {
            var keyLength = KeyLengthOf(algorithm);
            if (keyLength == 0 || key.IsNull() || key!.Length != keyLength)
                return Result<XmlElement>.Failure(MessageCodes.V_SEC_056.GetDescription(), Messages.GetValidationMessage(MessageCodes.V_SEC_056));

            byte[] cipherText;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.KeySize = keyLength * 8;
                    aes.Key = key;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.ISO10126;
                    aes.GenerateIV();

                    cipherText = new EncryptedXml(document).EncryptData(target, aes, content);
                }
            }
            catch (Exception ex)
            {
                return EncryptionFailure(ex);
            }

            var encryptedData = document.CreateElement(XmlEncryptionNames.Prefix, XmlEncryptionNames.EncryptedDataLocalName, XmlEncryptionNames.Namespace);
            encryptedData.SetAttribute(WsSecurityNames.IdLocalName, id);
            encryptedData.SetAttribute(XmlEncryptionNames.TypeAttributeName, content ? XmlEncryptionNames.ContentType : XmlEncryptionNames.ElementType);

            var method = document.CreateElement(XmlEncryptionNames.Prefix, XmlEncryptionNames.EncryptionMethodLocalName, XmlEncryptionNames.Namespace);
            method.SetAttribute(XmlEncryptionNames.AlgorithmAttributeName, algorithm);
            encryptedData.AppendChild(method);

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(keyInfoClause);
            encryptedData.AppendChild(document.ImportNode(keyInfo.GetXml(), true));

            var cipherData = document.CreateElement(XmlEncryptionNames.Prefix, XmlEncryptionNames.CipherDataLocalName, XmlEncryptionNames.Namespace);
            var cipherValue = document.CreateElement(XmlEncryptionNames.Prefix, XmlEncryptionNames.CipherValueLocalName, XmlEncryptionNames.Namespace);
            cipherValue.InnerText = Convert.ToBase64String(cipherText);
            cipherData.AppendChild(cipherValue);
            encryptedData.AppendChild(cipherData);

            return Result<XmlElement>.Success(encryptedData);
        }

        /// <summary>
        ///     Builds the failure returned when an element cannot be encrypted.
        /// </summary>
        /// <param name="exception">The captured exception, or null.</param>
        /// <returns>
        ///     A failed IResult&lt;XmlElement&gt;.
        /// </returns>
        private static IResult<XmlElement> EncryptionFailure(Exception exception)
            => Result<XmlElement>
                .Failure(MessageCodes.ER_SEC_SGN.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_SGN))
                .WithOptionalError(exception, "encrypting a WS-Security protected element");

        /// <summary>
        ///     Builds the refusal of a service certificate that is unfit to encrypt a Body for.
        /// </summary>
        /// <returns>
        ///     A failed IResult.
        /// </returns>
        private static IResult RecipientRefusal()
            => Result.Failure(MessageCodes.V_SEC_057.GetDescription(), Messages.GetValidationMessage(MessageCodes.V_SEC_057));
    }
}
