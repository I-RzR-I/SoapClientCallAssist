// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="RequestKeyMaterial.cs" company="RzR SOFT & TECH">
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
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helpers;
using SoapClientCallAssist.Security.WsSecurity;
using System;
using System.Collections.Generic;
using System.Threading;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     Single-use snapshot of what a response check needs from its request, stored on the
    ///     <see cref="System.Net.Http.HttpRequestMessage" /> under
    ///     <see cref="SoapClientEndpointExtensions.RequestSecurityStateKey" />. The first verify or
    ///     decrypt call consumes it; its key bytes are zeroed when that call finishes.
    /// </summary>
    internal sealed class RequestKeyMaterial : IDisposable
    {
        /// <summary>
        ///     Placeholder text <see cref="ToString" /> returns in place of the material's content.
        /// </summary>
        private const string Redacted = "RequestKeyMaterial";

        /// <summary>
        ///     The length of a secure conversation signature derived key, in bytes.
        /// </summary>
        private const int SecureConversationSignatureKeyLength = 24;

        /// <summary>
        ///     The length of a secure conversation encryption derived key, in bytes.
        /// </summary>
        private const int SecureConversationEncryptionKeyLength = 32;

        /// <summary>
        ///     The mode the request was built in.
        /// </summary>
        private readonly SoapSecurityModeType _mode;

        /// <summary>
        ///     The key family of the primary signature, or null.
        /// </summary>
        private readonly SignatureKeyFamily? _keyFamily;

        /// <summary>
        ///     The response verification policy, copied field by field.
        /// </summary>
        private readonly SoapVerificationPolicyDto _policy;

        /// <summary>
        ///     Whether the response must carry a signature confirmation.
        /// </summary>
        private readonly bool _requireSignatureConfirmation;

        /// <summary>
        ///     Whether the response may be decrypted.
        /// </summary>
        private readonly bool _allowDecryption;

        /// <summary>
        ///     Whether a response the check cannot bind to the request may be accepted.
        /// </summary>
        private readonly bool _allowUnboundResponse;

        /// <summary>
        ///     The largest plaintext accepted, in bytes.
        /// </summary>
        private readonly int _maxPlaintextBytes;

        /// <summary>
        ///     The data encryption algorithm URI the request encrypts with, which a reply's encrypted
        ///     parts must name.
        /// </summary>
        private readonly string _dataEncryptionAlgorithm;

        /// <summary>
        ///     The derivation label the request's derived key tokens carry, which a reply's tokens must
        ///     carry unchanged.
        /// </summary>
        private readonly string _keyDerivationLabel;

        /// <summary>
        ///     Every key the request derived from its secret, or null; moved out on consumption. A
        ///     reply whose token derives one of these is a reflection of the request's own keys.
        /// </summary>
        private byte[][] _requestDerivedKeys;

        /// <summary>
        ///     The expected response certificate's thumbprint, or null.
        /// </summary>
        private readonly string _expectedCertificateThumbprint;

        /// <summary>
        ///     The expected response certificate's public DER, or null; moved out on consumption.
        /// </summary>
        private byte[] _expectedCertificate;

        /// <summary>
        ///     The nonces the build generated.
        /// </summary>
        private readonly IReadOnlyList<string> _nonces;

        /// <summary>
        ///     The primary signature value, or null; moved out on consumption.
        /// </summary>
        private byte[] _signatureValue;

        /// <summary>
        ///     The SHA-1 of the encrypted key, or null; moved out on consumption.
        /// </summary>
        private byte[] _encryptedKeySha1;

        /// <summary>
        ///     The symmetric secret, or null; moved out on consumption.
        /// </summary>
        private byte[] _sessionSecret;

        /// <summary>
        ///     The <c>wsa:MessageID</c> emitted, or null.
        /// </summary>
        private readonly string _messageId;

        /// <summary>
        ///     The secure conversation session the request was keyed by, or null on any other mode.
        ///     Borrowed by reference.
        /// </summary>
        private readonly SoapSecureConversationSession _session;

        /// <summary>
        ///     The security context token identifier of the session, or null on any other mode. The
        ///     response's derived key token must reference this identifier.
        /// </summary>
        private readonly string _sctIdentifier;

        /// <summary>
        ///     Non-zero once consumed.
        /// </summary>
        private int _consumed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RequestKeyMaterial" /> class.
        /// </summary>
        /// <param name="mode">The mode the request was built in.</param>
        /// <param name="keyFamily">The key family of the primary signature, or null.</param>
        /// <param name="policy">The response verification policy copy.</param>
        /// <param name="requireSignatureConfirmation">
        ///     Whether a signature confirmation is required.
        /// </param>
        /// <param name="allowDecryption">Whether the response may be decrypted.</param>
        /// <param name="allowUnboundResponse">
        ///     Whether a response the check cannot bind to the request may be accepted.
        /// </param>
        /// <param name="maxPlaintextBytes">The largest plaintext accepted.</param>
        /// <param name="dataEncryptionAlgorithm">
        ///     The data encryption algorithm URI the request encrypts with.
        /// </param>
        /// <param name="keyDerivationLabel">
        ///     The derivation label the request's derived key tokens carry.
        /// </param>
        /// <param name="expectedCertificateThumbprint">
        ///     The expected certificate's thumbprint, or null.
        /// </param>
        /// <param name="expectedCertificate">The expected certificate's public DER, or null.</param>
        /// <param name="nonces">The nonces the build generated.</param>
        /// <param name="signatureValue">The primary signature value, or null.</param>
        /// <param name="encryptedKeySha1">The SHA-1 of the encrypted key, or null.</param>
        /// <param name="sessionSecret">The symmetric secret, or null. The material owns it.</param>
        /// <param name="requestDerivedKeys">
        ///     Every key the request derived from its secret, or null.
        /// </param>
        /// <param name="messageId">The message id, or null.</param>
        /// <param name="session">
        ///     The secure conversation session the request was keyed by, or null.
        /// </param>
        /// <param name="sctIdentifier">
        ///     The security context token identifier of the session, or null.
        /// </param>
        private RequestKeyMaterial(SoapSecurityModeType mode, SignatureKeyFamily? keyFamily, SoapVerificationPolicyDto policy,
            bool requireSignatureConfirmation, bool allowDecryption, bool allowUnboundResponse, int maxPlaintextBytes,
            string dataEncryptionAlgorithm, string keyDerivationLabel, string expectedCertificateThumbprint, byte[] expectedCertificate,
            IReadOnlyList<string> nonces, byte[] signatureValue, byte[] encryptedKeySha1, byte[] sessionSecret, byte[][] requestDerivedKeys,
            string messageId, SoapSecureConversationSession session, string sctIdentifier)
        {
            _mode = mode;
            _keyFamily = keyFamily;
            _policy = policy;
            _requireSignatureConfirmation = requireSignatureConfirmation;
            _allowDecryption = allowDecryption;
            _allowUnboundResponse = allowUnboundResponse;
            _maxPlaintextBytes = maxPlaintextBytes;
            _dataEncryptionAlgorithm = dataEncryptionAlgorithm;
            _keyDerivationLabel = keyDerivationLabel;
            _expectedCertificateThumbprint = expectedCertificateThumbprint;
            _expectedCertificate = expectedCertificate;
            _nonces = nonces;
            _signatureValue = signatureValue;
            _encryptedKeySha1 = encryptedKeySha1;
            _sessionSecret = sessionSecret;
            _requestDerivedKeys = requestDerivedKeys;
            _messageId = messageId;
            _session = session;
            _sctIdentifier = sctIdentifier;
        }

        /// <summary>
        ///     Snapshots a build's material, copying every value except the symmetric secret, which
        ///     moves from the build result. On a symmetric mode it re-derives the request's keys under
        ///     every nonce.
        /// </summary>
        /// <param name="plan">The plan the request was built from.</param>
        /// <param name="built">What the header build produced.</param>
        /// <returns>
        ///     The single-use material for the built request.
        /// </returns>
        internal static RequestKeyMaterial Snapshot(SecurityHeaderPlan plan, WsSecurityHeaderBuildResult built)
        {
            var options = plan.Options;
            var expected = options.ExpectedResponseCertificate;
            var response = options.ResponseSecurity;
            var binding = options.SymmetricBinding;
            var label = binding?.KeyDerivationLabel ?? WsSecureConversationNames.DefaultLabel;
            var secureConversation = plan.Mode == SoapSecurityModeType.SecureConversation;
            var session = secureConversation ? options.SecureConversation?.Session : null;
            var sessionSecret = built.TakeSessionSecret();

            var requestDerivedKeys = secureConversation
                ? DeriveSecureConversationRequestKeys(session, built.Nonces)
                : DeriveRequestKeys(sessionSecret, label, built.Nonces, binding);

            return new RequestKeyMaterial(
                plan.Mode,
                plan.KeyFamily,
                CopyPolicy(options.ResponseVerificationPolicy),
                plan.RequireSignatureConfirmation,
                response.IsNotNull() && response!.AllowDecryption,
                response.IsNotNull() && response!.AllowUnboundResponse,
                ResolveMaxPlaintextBytes(response),
                (options.Encryption?.DataAlgorithm ?? SoapDataEncryptionAlgorithmType.Aes256Cbc).GetDescription(),
                label,
                expected?.Thumbprint,
                expected.IsNull() ? null : (byte[])expected!.RawData.Clone(),
                built.Nonces,
                Clone(built.PrimarySignatureValue),
                Clone(built.EncryptedKeySha1),
                sessionSecret,
                requestDerivedKeys,
                built.MessageId,
                session,
                session?.ContextIdentifier);
        }

        /// <summary>
        ///     Derives every key the request could have derived from a copy of the session secret, per
        ///     nonce, at the session signature and encryption key lengths, then zeroes the copy.
        /// </summary>
        /// <param name="session">The session, or null.</param>
        /// <param name="nonces">The nonces the build generated, base64.</param>
        /// <returns>
        ///     The derived keys, or null when there is no session or no nonce.
        /// </returns>
        private static byte[][] DeriveSecureConversationRequestKeys(SoapSecureConversationSession session,
            IReadOnlyList<string> nonces)
        {
            if (session.IsNull() || nonces.IsNullOrEmptyEnumerable())
                return null;

            var secret = session!.CopySecret();
            if (secret.IsNull())
                return null;

            try
            {
                var keys = new List<byte[]>();

                foreach (var encoded in nonces)
                {
                    byte[] nonce;

                    try
                    {
                        nonce = Convert.FromBase64String(encoded);
                    }
                    catch (FormatException)
                    {
                        continue;
                    }

                    foreach (var length in new[]
                             {
                                 SecureConversationSignatureKeyLength,
                                 SecureConversationEncryptionKeyLength
                             })
                    {
                        var key = WsSecurityKeyDerivation.DeriveKey(secret, WsSecureConversationNames.DefaultLabel, nonce, 0, length);
                        if (key.IsNotNull())
                            keys.Add(key);
                    }
                }

                return keys.ToArray();
            }
            finally
            {
                Array.Clear(secret, 0, secret.Length);
            }
        }

        /// <summary>
        ///     Derives every key the request could have derived: from the secret, under the label, for
        ///     each nonce the build generated, at the signature key length and at the encryption key
        ///     length.
        /// </summary>
        /// <param name="secret">The symmetric secret, or null on a mode keyed by a certificate.</param>
        /// <param name="label">The derivation label.</param>
        /// <param name="nonces">The nonces the build generated, base64.</param>
        /// <param name="binding">The symmetric binding options, or null.</param>
        /// <returns>
        ///     The derived keys, or null when the mode derives none.
        /// </returns>
        private static byte[][] DeriveRequestKeys(byte[] secret, string label, IReadOnlyList<string> nonces,
            SoapSymmetricBindingDto binding)
        {
            if (secret.IsNull() || binding.IsNull() || nonces.IsNullOrEmptyEnumerable())
                return null;

            var keys = new List<byte[]>();

            foreach (var encoded in nonces)
            {
                byte[] nonce;

                try
                {
                    nonce = Convert.FromBase64String(encoded);
                }
                catch (FormatException)
                {
                    continue;
                }

                foreach (var length in new[] { binding!.SignatureKeyLength, binding.EncryptionKeyLength })
                {
                    var key = WsSecurityKeyDerivation.DeriveKey(secret, label, nonce, 0, length);
                    if (key.IsNotNull())
                        keys.Add(key);
                }
            }

            return keys.ToArray();
        }

        /// <summary>
        ///     Consumes the material. The first call wins and receives the one-time view; every later
        ///     call is refused.
        /// </summary>
        /// <param name="material">[out] The one-time view, or null when already consumed.</param>
        /// <returns>
        ///     True when this call consumed the material.
        /// </returns>
        internal bool TryConsume(out ConsumedKeyMaterial material)
        {
            material = null;

            if (Interlocked.Exchange(ref _consumed, 1) != 0)
                return false;

            material = new ConsumedKeyMaterial(_mode, _keyFamily, _policy, _requireSignatureConfirmation,
                _allowDecryption, _allowUnboundResponse, _maxPlaintextBytes, _dataEncryptionAlgorithm,
                _keyDerivationLabel, _expectedCertificateThumbprint, _expectedCertificate, _nonces,
                _signatureValue, _encryptedKeySha1, ResolveConsumableSecret(), _requestDerivedKeys, _messageId,
                _sctIdentifier, SecureConversationUnavailable());

            _expectedCertificate = null;
            _signatureValue = null;
            _encryptedKeySha1 = null;
            _sessionSecret = null;
            _requestDerivedKeys = null;

            return true;
        }

        /// <summary>
        ///     Resolves the secret the one-time view derives its response keys from, copying it from the
        ///     live session on a secure conversation and moving the build's secret otherwise.
        /// </summary>
        /// <returns>
        ///     The secret, or null when a secure conversation session has been disposed.
        /// </returns>
        private byte[] ResolveConsumableSecret()
            => _session.IsNotNull() ? _session!.CopySecret() : _sessionSecret;

        /// <summary>
        ///     Determines whether the secure conversation session the request was keyed by was disposed
        ///     or lost its secret before the check.
        /// </summary>
        /// <returns>
        ///     True when the session is disposed or holds no secret; false on any other mode.
        /// </returns>
        private bool SecureConversationUnavailable()
            => _session.IsNotNull() && (_session!.IsDisposed || _session.HasSecret.IsFalse());

        /// <summary>
        ///     Consumes the material without a response check and zeroes every array it holds, for
        ///     material minted by a build that produced no request. A later consumption is refused.
        /// 
        /// </summary>
        public void Dispose()
        {
            if (TryConsume(out var consumed))
                consumed.Dispose();
        }

        /// <summary>
        ///     Returns the <see cref="Redacted" /> constant in place of the material's content.
        /// </summary>
        /// <returns>
        ///     The redacted name.
        /// </returns>
        public override string ToString() => Redacted;

        /// <summary>
        ///     Copies a verification policy field by field, substituting the strict defaults for null.
        /// </summary>
        /// <param name="policy">The caller's policy, or null for the strict defaults.</param>
        /// <returns>
        ///     The copy.
        /// </returns>
        private static SoapVerificationPolicyDto CopyPolicy(SoapVerificationPolicyDto policy)
        {
            if (policy.IsNull())
                return new SoapVerificationPolicyDto();

            return new SoapVerificationPolicyDto
            {
                RequireBodySigned = policy!.RequireBodySigned,
                RequireValidTimestamp = policy.RequireValidTimestamp,
                ClockSkew = policy.ClockSkew,
                AllowSha1Algorithms = policy.AllowSha1Algorithms,
                DiagnosticDetail = policy.DiagnosticDetail
            };
        }

        /// <summary>
        ///     Resolves the plaintext cap a snapshot carries, substituting
        ///     <see cref="WsSecurityResponseDecryptor.DefaultMaxPlaintextBytes" /> for a cap that is
        ///     unset or not positive.
        /// </summary>
        /// <param name="response">The response security options, or null.</param>
        /// <returns>
        ///     The cap, in bytes.
        /// </returns>
        private static int ResolveMaxPlaintextBytes(SoapResponseSecurityDto response)
        {
            var cap = response?.MaxPlaintextBytes;

            return cap.HasValue && cap.Value > 0
                ? cap.Value
                : WsSecurityResponseDecryptor.DefaultMaxPlaintextBytes;
        }

        /// <summary>
        ///     Clones an array.
        /// </summary>
        /// <param name="bytes">The array, or null.</param>
        /// <returns>
        ///     The clone, or null.
        /// </returns>
        private static byte[] Clone(byte[] bytes)
            => bytes.IsNull() ? null : (byte[])bytes!.Clone();
    }
}
