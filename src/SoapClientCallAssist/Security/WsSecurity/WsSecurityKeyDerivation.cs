// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 22:40
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 22:40
//  ***********************************************************************
//  <copyright file="WsSecurityKeyDerivation.cs" company="RzR SOFT & TECH">
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
using System;
using System.Security.Cryptography;
using System.Text;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     The TLS <c>P_SHA1</c> pseudo-random function of RFC 2246 section 5, implemented from the
    ///     definition, and the two ways WS-SecureConversation and WS-Trust seed it.
    /// </summary>
    internal static class WsSecurityKeyDerivation
    {
        /// <summary>
        ///     The longest derived key a derivation accepts, in bytes.
        /// </summary>
        internal const int MaxDerivedKeyLength = 256;

        /// <summary>
        ///     Derives a WS-SecureConversation key from a secret: <c>P_SHA1(secret, label + nonce)</c>,
        ///     of which the <paramref name="length" /> bytes starting at <paramref name="offset" />
        ///     form the key, exactly as a <c>DerivedKeyToken</c> carrying that label, nonce, offset and
        ///     length describes.
        /// </summary>
        /// <param name="secret">The shared secret.</param>
        /// <param name="label">The derivation label, UTF-8 encoded into the seed.</param>
        /// <param name="nonce">The raw nonce bytes appended to the label.</param>
        /// <param name="offset">The offset of the key in the derived stream, in bytes.</param>
        /// <param name="length">The length of the key, in bytes.</param>
        /// <returns>
        ///     The derived key, or null when any input cannot describe a key.
        /// </returns>
        internal static byte[] DeriveKey(byte[] secret, string label, byte[] nonce, int offset, int length)
        {
            if (secret.IsNullOrEmptyEnumerable() || nonce.IsNullOrEmptyEnumerable() || label.IsNull())
                return null;

            if (offset < 0 || length <= 0 || length > MaxDerivedKeyLength || offset > MaxDerivedKeyLength)
                return null;

            var labelBytes = Encoding.UTF8.GetBytes(label);
            var seed = new byte[labelBytes.Length + nonce!.Length];
            Buffer.BlockCopy(labelBytes, 0, seed, 0, labelBytes.Length);
            Buffer.BlockCopy(nonce, 0, seed, labelBytes.Length, nonce.Length);

            var stream = PSha1(secret, seed, offset + length);

            try
            {
                var key = new byte[length];
                Buffer.BlockCopy(stream, offset, key, 0, length);

                return key;
            }
            finally
            {
                Array.Clear(stream, 0, stream.Length);
                Array.Clear(seed, 0, seed.Length);
            }
        }

        /// <summary>
        ///     Computes the WS-Trust <c>PSHA1</c> computed key from the two entropies of a token
        ///     issuance: <c>P_SHA1(requestorEntropy, issuerEntropy)</c> truncated to the key size.
        /// 
        /// </summary>
        /// <param name="requestorEntropy">The entropy the requestor contributed.</param>
        /// <param name="issuerEntropy">The entropy the issuer contributed.</param>
        /// <param name="length">The key size, in bytes.</param>
        /// <returns>
        ///     The computed key, or null when any input cannot describe a key.
        /// </returns>
        internal static byte[] ComputeKey(byte[] requestorEntropy, byte[] issuerEntropy, int length)
        {
            if (requestorEntropy.IsNullOrEmptyEnumerable() || issuerEntropy.IsNullOrEmptyEnumerable())
                return null;

            if (length <= 0 || length > MaxDerivedKeyLength)
                return null;

            return PSha1(requestorEntropy, issuerEntropy, length);
        }

        /// <summary>
        ///     The <c>P_SHA1</c> pseudo-random function, the concatenation of
        ///     <c>HMAC_SHA1(secret, A(i) + seed)</c> where <c>A(0) = seed</c> and
        ///     <c>A(i) = HMAC_SHA1(secret, A(i - 1))</c>, truncated to the requested length.
        /// </summary>
        /// <param name="secret">The HMAC key.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="length">The number of output bytes.</param>
        /// <returns>
        ///     The output bytes.
        /// </returns>
        internal static byte[] PSha1(byte[] secret, byte[] seed, int length)
        {
            var output = new byte[length];
            var produced = 0;

            using (var hmac = new HMACSHA1(secret))
            {
                var a = seed;
                var block = new byte[hmac.HashSize / 8 + seed.Length];

                try
                {
                    while (produced < length)
                    {
                        var next = hmac.ComputeHash(a);

                        if (ReferenceEquals(a, seed).IsFalse())
                            Array.Clear(a, 0, a.Length);

                        a = next;

                        Buffer.BlockCopy(a, 0, block, 0, a.Length);
                        Buffer.BlockCopy(seed, 0, block, a.Length, seed.Length);

                        var chunk = hmac.ComputeHash(block);
                        var take = Math.Min(chunk.Length, length - produced);
                        Buffer.BlockCopy(chunk, 0, output, produced, take);
                        produced += take;

                        Array.Clear(chunk, 0, chunk.Length);
                    }
                }
                finally
                {
                    Array.Clear(block, 0, block.Length);

                    if (ReferenceEquals(a, seed).IsFalse())
                        Array.Clear(a, 0, a.Length);
                }
            }

            return output;
        }
    }
}
