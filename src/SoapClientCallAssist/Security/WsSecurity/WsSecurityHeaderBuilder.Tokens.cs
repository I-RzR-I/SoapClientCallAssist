// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="WsSecurityHeaderBuilder.Tokens.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Helpers;
using System;
using System.Security.Cryptography;
using System.Text;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     The token concern of the header builder: the certificate token and the username token.
    /// </summary>
    internal sealed partial class WsSecurityHeaderBuilder
    {
        /// <summary>
        ///     (Immutable)
        ///     The length of the random nonce a username token carries, in bytes.
        /// </summary>
        private const int UsernameTokenNonceLength = 16;

        /// <summary>
        ///     Emits the <c>wsse:BinarySecurityToken</c> carrying the caller's certificate.
        /// </summary>
        private void EmitBinarySecurityToken()
        {
            var token = _document.CreateElement(
                WsSecurityNames.WssePrefix, WsSecurityNames.BinarySecurityTokenLocalName, WsSecurityNames.WsseNamespace);
            token.SetAttribute(WsSecurityNames.ValueTypeAttributeName, WsSecurityNames.X509TokenValueType);
            token.SetAttribute(WsSecurityNames.EncodingTypeAttributeName, WsSecurityNames.Base64BinaryEncodingType);
            token.InnerText = Convert.ToBase64String(Options.SigningCertificate.RawData);

            _binarySecurityTokenId = _ids.Stamp(_document, token, "bst");

            _security.AppendChild(token);
        }

        /// <summary>
        ///     Emits the plaintext <c>wsse:UsernameToken</c> with the user name, the password as text or
        ///     digest, the nonce and the creation instant. A digest token always carries a nonce and an
        ///     instant.
        /// </summary>
        private void EmitUsernameToken()
        {
            var options = Options.UsernameToken;
            var digest = options.PasswordType == SoapPasswordType.Digest;

            var token = _document.CreateElement(WsSecurityNames.WssePrefix, WsSecurityNames.UsernameTokenLocalName, WsSecurityNames.WsseNamespace);
            var tokenId = _ids.Stamp(_document, token, "ut");

            AppendTextChild(token, WsSecurityNames.WssePrefix, WsSecurityNames.UsernameLocalName, WsSecurityNames.WsseNamespace, options.Username);

            var nonce = options.IncludeNonce || digest ? NewNonce() : null;
            var created = options.IncludeCreated || digest ? Instant(DateTime.UtcNow) : null;

            if (options.Password.IsNotNull())
            {
                var password = AppendTextChild(
                    token, WsSecurityNames.WssePrefix, WsSecurityNames.PasswordLocalName, WsSecurityNames.WsseNamespace,
                    digest ? PasswordDigest(nonce, created, options.Password) : options.Password);
                password.SetAttribute(WsSecurityNames.PasswordTypeAttributeName, options.PasswordType.GetDescription());
            }

            if (nonce.IsNotNull())
            {
                var encoded = Convert.ToBase64String(nonce);
                var nonceElement = AppendTextChild(
                    token, WsSecurityNames.WssePrefix, WsSecurityNames.NonceLocalName, WsSecurityNames.WsseNamespace, encoded);
                nonceElement.SetAttribute(WsSecurityNames.EncodingTypeAttributeName, WsSecurityNames.Base64BinaryEncodingType);
                _nonces.Add(encoded);

                Array.Clear(nonce, 0, nonce.Length);
            }

            if (created.IsNotNull())
                AppendTextChild(token, WsSecurityNames.WsuPrefix, WsSecurityNames.CreatedLocalName, WsSecurityNames.WsuNamespace, created);

            _security.AppendChild(token);

            if (_plan.EmitsSignature && options.SignToken)
                _referenceIds.Add(tokenId);
        }

        /// <summary>
        ///     Generates the random nonce of a username token.
        /// </summary>
        /// <returns>
        ///     The nonce bytes.
        /// </returns>
        private static byte[] NewNonce()
        {
            var nonce = new byte[UsernameTokenNonceLength];

            using (var generator = RandomNumberGenerator.Create())
                generator.GetBytes(nonce);

            return nonce;
        }

        /// <summary>
        ///     Computes the Username Token Profile password digest,
        ///     <c>Base64(SHA-1(nonce + created + password))</c>, over the raw nonce bytes, the UTF-8 of
        ///     the created instant and the UTF-8 of the raw password.
        /// </summary>
        /// <param name="nonce">The raw nonce bytes.</param>
        /// <param name="created">The creation instant, as written on the wire.</param>
        /// <param name="password">The raw password.</param>
        /// <returns>
        ///     The base64 digest.
        /// </returns>
        private static string PasswordDigest(byte[] nonce, string created, string password)
        {
            var createdBytes = Encoding.UTF8.GetBytes(created);
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var input = new byte[nonce.Length + createdBytes.Length + passwordBytes.Length];

            try
            {
                Buffer.BlockCopy(nonce, 0, input, 0, nonce.Length);
                Buffer.BlockCopy(createdBytes, 0, input, nonce.Length, createdBytes.Length);
                Buffer.BlockCopy(passwordBytes, 0, input, nonce.Length + createdBytes.Length, passwordBytes.Length);

                using (var sha1 = SHA1.Create())
                    return Convert.ToBase64String(sha1.ComputeHash(input));
            }
            finally
            {
                Array.Clear(input, 0, input.Length);
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
        }
    }
}
