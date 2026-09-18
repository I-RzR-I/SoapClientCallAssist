// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapSecureConversationSession.cs" company="RzR SOFT & TECH">
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
using System;
using System.Threading;

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     An established WS-SecureConversation session holding its context identifier, expiry,
    ///     version and the shared secret derived keys come from. The secret is copied in, never
    ///     exposed, and zeroed on <see cref="Dispose" />.
    /// </summary>
    /// <seealso cref="T:IDisposable"/>
    public sealed class SoapSecureConversationSession : IDisposable
    {
        /// <summary>
        ///     (Immutable)
        ///     Placeholder text <see cref="ToString" /> returns in place of the session's content. 
        /// </summary>
        private const string Redacted = "SoapSecureConversationSession";

        /// <summary>
        ///     The shared secret, or null once disposed.
        /// </summary>
        private byte[] _secret;

        /// <summary>
        ///     Non-zero once the session has been disposed.
        /// </summary>
        private int _disposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapSecureConversationSession" /> class.
        /// </summary>
        /// <param name="contextIdentifier">The context identifier the token names.</param>
        /// <param name="secret">The shared secret, copied in; the caller keeps its array.</param>
        /// <param name="expiresUtc">The instant the session expires.</param>
        /// <param name="version">The WS-SecureConversation version the session was issued in.</param>
        public SoapSecureConversationSession(string contextIdentifier, byte[] secret, 
            DateTimeOffset expiresUtc, SoapSecureConversationVersionType version)
        {
            ContextIdentifier = contextIdentifier;
            ExpiresUtc = expiresUtc.ToUniversalTime();
            Version = version;
            _secret = secret.IsNull() ? null : (byte[])secret!.Clone();
        }

        /// <summary>
        ///     Gets the context identifier the <c>SecurityContextToken</c> names.
        /// </summary>
        /// <value>
        ///     The identifier of the context.
        /// </value>
        public string ContextIdentifier { get; }

        /// <summary>
        ///     Gets the instant, in UTC, the session expires.
        /// </summary>
        /// <value>
        ///     The expires UTC.
        /// </value>
        public DateTimeOffset ExpiresUtc { get; }

        /// <summary>
        ///     Gets the WS-SecureConversation version the session was issued in.
        /// </summary>
        /// <value>
        ///     The version.
        /// </value>
        public SoapSecureConversationVersionType Version { get; }

        /// <summary>
        ///     Gets a value indicating whether the session has expired. An expired session cannot key a
        ///     request; the caller renews it through the issuer.
        /// </summary>
        /// <value>
        ///     True if this object is expired, false if not.
        /// </value>
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresUtc;

        /// <summary>
        ///     Whether the session has been disposed and its secret zeroed. A disposed session cannot
        ///     key a request.
        /// </summary>
        /// <value>
        ///     True if this object is disposed, false if not.
        /// </value>
        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

        /// <summary>
        ///     Whether a non-empty secret was supplied and the session is not disposed.
        /// </summary>
        /// <value>
        ///     True if this object has secret, false if not.
        /// </value>
        internal bool HasSecret => IsDisposed.IsFalse() && _secret.IsNotNull() && _secret!.Length > 0;

        /// <summary>
        ///     Copies the shared secret out for a single key derivation. The copy belongs to the caller,
        ///     who zeroes it when done.
        /// </summary>
        /// <returns>
        ///     A copy of the secret, or null when the session is disposed or holds none.
        /// </returns>
        internal byte[] CopySecret()
            => HasSecret ? (byte[])_secret!.Clone() : null;

        /// <summary>
        ///     Marks the session disposed and zeroes the shared secret.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            var secret = _secret;
            _secret = null;

            if (secret.IsNotNull())
                Array.Clear(secret!, 0, secret.Length);
        }

        /// <summary>
        ///     Returns a constant placeholder in place of the session's content.
        /// </summary>
        /// <returns>
        ///     The redacted name.
        /// </returns>
        public override string ToString() => Redacted;
    }
}
