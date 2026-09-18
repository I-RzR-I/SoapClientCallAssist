// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 22:40
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 02:30
//  ***********************************************************************
//  <copyright file="SymmetricKeySource.cs" company="RzR SOFT & TECH">
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
using SoapClientCallAssist.Security.WsSecurity;
using System;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     The secret one symmetric build derives its keys from, with the token reference and nonces
    ///     of its derived key tokens. It is disposed whether the build succeeded or failed.
    /// </summary>
    internal sealed class SymmetricKeySource : IDisposable
    {
        /// <summary>
        ///     The shared secret, or null once taken. Zeroed in place on disposal.
        /// </summary>
        private byte[] _secret;

        /// <summary>
        ///     True once disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SymmetricKeySource" /> class.
        /// </summary>
        /// <param name="secret">The shared secret. The source owns it and zeroes it on disposal.</param>
        /// <param name="tokenReference">The token reference every derived key token carries.</param>
        /// <param name="version">
        ///     The WS-SecureConversation version the derived key tokens are written in.
        /// </param>
        /// <param name="label">The derivation label, or null for the specification default.</param>
        /// <param name="signatureKeyLength">The length of the signature key, in bytes.</param>
        /// <param name="encryptionKeyLength">The length of the encryption key, in bytes.</param>
        internal SymmetricKeySource(byte[] secret, SecurityTokenReferenceClause tokenReference,
            SoapSecureConversationVersionType version, string label, int signatureKeyLength, int encryptionKeyLength)
        {
            _secret = secret;
            TokenReference = tokenReference;
            Version = version;
            Label = label ?? WsSecureConversationNames.DefaultLabel;
            SignatureKeyLength = signatureKeyLength;
            EncryptionKeyLength = encryptionKeyLength;
        }

        /// <summary>
        ///     The token reference every derived key token carries.
        /// </summary>
        /// <value>
        ///     The token reference.
        /// </value>
        internal SecurityTokenReferenceClause TokenReference { get; }

        /// <summary>
        ///     The WS-SecureConversation version the derived key tokens are written in.
        /// </summary>
        /// <value>
        ///     The version.
        /// </value>
        internal SoapSecureConversationVersionType Version { get; }

        /// <summary>
        ///     The derivation label.
        /// </summary>
        /// <value>
        ///     The label.
        /// </value>
        internal string Label { get; }

        /// <summary>
        ///     The length of the signature key, in bytes.
        /// </summary>
        /// <value>
        ///     The length of the signature key.
        /// </value>
        internal int SignatureKeyLength { get; }

        /// <summary>
        ///     The length of the encryption key, in bytes.
        /// </summary>
        /// <value>
        ///     The length of the encryption key.
        /// </value>
        internal int EncryptionKeyLength { get; }

        /// <summary>
        ///     The SHA-1 of the encrypted key's cipher value, by which the service names the secret in
        ///     its reply, or null for a source not keyed by an encrypted key.
        /// </summary>
        /// <value>
        ///     The encrypted key sha 1.
        /// </value>
        internal byte[] EncryptedKeySha1 { get; set; }

        /// <summary>
        ///     The raw nonce bytes of the signature derived key token, or null until emitted.
        /// </summary>
        /// <value>
        ///     The signature nonce.
        /// </value>
        internal byte[] SignatureNonce { get; set; }

        /// <summary>
        ///     The raw nonce bytes of the encryption derived key token, or null until emitted or when
        ///     the build encrypts nothing.
        /// </summary>
        /// <value>
        ///     The encryption nonce.
        /// </value>
        internal byte[] EncryptionNonce { get; set; }

        /// <summary>
        ///     The <c>wsu:Id</c> of the signature derived key token, or null until emitted.
        /// </summary>
        /// <value>
        ///     The identifier of the signature token.
        /// </value>
        internal string SignatureTokenId { get; set; }

        /// <summary>
        ///     The <c>wsu:Id</c> of the encryption derived key token, or null until emitted or when the
        ///     build encrypts nothing.
        /// </summary>
        /// <value>
        ///     The identifier of the encryption token.
        /// </value>
        internal string EncryptionTokenId { get; set; }

        /// <summary>
        ///     True while the secret is still held, until taken or disposed.
        /// </summary>
        /// <value>
        ///     True if this object has secret, false if not.
        /// </value>
        internal bool HasSecret => _disposed.IsFalse() && _secret.IsNotNull();

        /// <summary>
        ///     Hands the secret over, ownership included. The source derives no further key afterwards
        ///     and disposing it then zeroes only the nonces.
        /// </summary>
        /// <returns>
        ///     The secret, or null once taken or disposed.
        /// </returns>
        internal byte[] TakeSecret()
        {
            if (_disposed)
                return null;

            var secret = _secret;
            _secret = null;

            return secret;
        }

        /// <summary>
        ///     Derives the signature key from the secret and the signature nonce.
        /// </summary>
        /// <returns>
        ///     The key, or null when the source holds no secret or no signature nonce.
        /// </returns>
        internal byte[] DeriveSignatureKey()
            => WsSecurityKeyDerivation.DeriveKey(HasSecret ? _secret : null, Label, SignatureNonce, 0, SignatureKeyLength);

        /// <summary>
        ///     Derives the encryption key from the secret and the encryption nonce.
        /// </summary>
        /// <returns>
        ///     The key, or null when the source holds no secret or no encryption nonce.
        /// </returns>
        internal byte[] DeriveEncryptionKey()
            => WsSecurityKeyDerivation.DeriveKey(HasSecret ? _secret : null, Label, EncryptionNonce, 0, EncryptionKeyLength);

        /// <summary>
        ///     Zeroes the secret, if still held, and both nonces. Idempotent.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Zero(_secret);
            SignatureNonce = Zero(SignatureNonce);
            EncryptionNonce = Zero(EncryptionNonce);
        }

        /// <summary>
        ///     Zeroes an array.
        /// </summary>
        /// <param name="bytes">The array, or null.</param>
        /// <returns>
        ///     Always null.
        /// </returns>
        private static byte[] Zero(byte[] bytes)
        {
            if (bytes.IsNotNull())
                Array.Clear(bytes!, 0, bytes.Length);

            return null;
        }
    }
}
