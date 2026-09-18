// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 02:30
//  ***********************************************************************
//  <copyright file="WsSecurityHeaderBuildResult.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using System;
using System.Collections.Generic;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     The wire one header build produced and the facts the request's key material snapshots. It
    ///     owns the symmetric secret until the key material takes it, zeroing it on disposal
    ///     otherwise.
    /// </summary>
    internal sealed class WsSecurityHeaderBuildResult : IDisposable
    {
        /// <summary>
        ///     The symmetric secret the build derived its keys from, or null; handed over on
        ///     <see cref="TakeSessionSecret" /> and zeroed on disposal otherwise.
        /// </summary>
        private byte[] _sessionSecret;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WsSecurityHeaderBuildResult" /> class.
        /// </summary>
        /// <param name="wire">The signed envelope's final wire representation.</param>
        /// <param name="primarySignatureValue">
        ///     The raw primary signature value, or null when none was emitted.
        /// </param>
        /// <param name="nonces">Every nonce the build generated, as written on the wire.</param>
        /// <param name="messageId">The <c>wsa:MessageID</c> emitted, or null.</param>
        /// <param name="encryptedKeySha1">
        ///     The SHA-1 of the emitted encrypted key's cipher value, or null.
        /// </param>
        /// <param name="sessionSecret">
        ///     The symmetric secret the build derived its keys from, or null. The result owns it.
        /// </param>
        internal WsSecurityHeaderBuildResult(string wire, byte[] primarySignatureValue, 
            IReadOnlyList<string> nonces, string messageId, byte[] encryptedKeySha1, byte[] sessionSecret)
        {
            Wire = wire;
            PrimarySignatureValue = primarySignatureValue;
            Nonces = nonces;
            MessageId = messageId;
            EncryptedKeySha1 = encryptedKeySha1;
            _sessionSecret = sessionSecret;
        }

        /// <summary>
        ///     The signed envelope's final wire representation.
        /// </summary>
        /// <value>
        ///     The wire.
        /// </value>
        internal string Wire { get; }

        /// <summary>
        ///     The raw primary signature value, or null when the header carries no signature.
        /// </summary>
        internal byte[] PrimarySignatureValue { get; }

        /// <summary>
        ///     Every nonce the build generated, base64 encoded exactly as written on the wire.
        /// </summary>
        internal IReadOnlyList<string> Nonces { get; }

        /// <summary>
        ///     The <c>wsa:MessageID</c> emitted, or null when addressing is off or no id was asked for.
        /// </summary>
        internal string MessageId { get; }

        /// <summary>
        ///     The SHA-1 of the emitted encrypted key's cipher value, or null when no encrypted key was
        ///     emitted.
        /// </summary>
        internal byte[] EncryptedKeySha1 { get; }

        /// <summary>
        ///     True until the symmetric secret is taken or the result is disposed; always false on a
        ///     mode keyed by a certificate.
        /// </summary>
        internal bool HasSessionSecret => _sessionSecret.IsNotNull();

        /// <summary>
        ///     Hands the symmetric secret over, ownership included, to the request's key material. The
        ///     result holds nothing afterwards and its disposal zeroes nothing.
        /// </summary>
        /// <returns>
        ///     The secret, or null on a mode keyed by a certificate or once already taken.
        /// </returns>
        internal byte[] TakeSessionSecret()
        {
            var secret = _sessionSecret;
            _sessionSecret = null;

            return secret;
        }

        /// <summary>
        ///     Zeroes the symmetric secret if the key material never took it.
        /// </summary>
        public void Dispose()
        {
            if (_sessionSecret.IsNull())
                return;

            Array.Clear(_sessionSecret!, 0, _sessionSecret.Length);
            _sessionSecret = null;
        }
    }
}
