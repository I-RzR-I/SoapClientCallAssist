// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="ConsumedKeyMaterial.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using System;
using System.Collections.Generic;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     One-time view of a request's key material, handed to the single verification or
    ///     decryption that consumes it. Its byte arrays are zeroed on <see cref="Dispose" /> whether
    ///     that operation succeeds or not.
    /// </summary>
    internal sealed class ConsumedKeyMaterial : IDisposable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumedKeyMaterial" /> class.
        /// </summary>
        /// <param name="mode">The mode the request was built in.</param>
        /// <param name="keyFamily">The key family of the request's primary signature, or null.</param>
        /// <param name="policy">The response verification policy snapshot.</param>
        /// <param name="requireSignatureConfirmation">Whether a signature confirmation is required.</param>
        /// <param name="allowDecryption">Whether the response may be decrypted.</param>
        /// <param name="allowUnboundResponse">Whether a response the check cannot bind to the request may be accepted.</param>
        /// <param name="maxPlaintextBytes">The largest plaintext accepted.</param>
        /// <param name="dataEncryptionAlgorithm">The data encryption algorithm URI the request encrypts with.</param>
        /// <param name="keyDerivationLabel">The derivation label the request's derived key tokens carry.</param>
        /// <param name="expectedCertificateThumbprint">The expected response certificate's thumbprint, or null.</param>
        /// <param name="expectedCertificate">The expected response certificate's public DER, or null.</param>
        /// <param name="nonces">The nonces the request generated.</param>
        /// <param name="signatureValue">The request's primary signature value, or null.</param>
        /// <param name="encryptedKeySha1">The SHA-1 of the request's encrypted key, or null.</param>
        /// <param name="sessionSecret">The symmetric secret the request's keys derive from, or null.</param>
        /// <param name="requestDerivedKeys">Every key the request derived from its secret, or null.</param>
        /// <param name="messageId">The request's <c>wsa:MessageID</c>, or null.</param>
        /// <param name="sctIdentifier">The security context token identifier of the session, or null.</param>
        /// <param name="sessionUnavailable">Whether the secure conversation session was disposed before the check.</param>
        internal ConsumedKeyMaterial(SoapSecurityModeType mode, SignatureKeyFamily? keyFamily, SoapVerificationPolicyDto policy,
            bool requireSignatureConfirmation, bool allowDecryption, bool allowUnboundResponse, int maxPlaintextBytes,
            string dataEncryptionAlgorithm, string keyDerivationLabel, string expectedCertificateThumbprint, byte[] expectedCertificate,
            IReadOnlyList<string> nonces, byte[] signatureValue, byte[] encryptedKeySha1, byte[] sessionSecret, byte[][] requestDerivedKeys,
            string messageId, string sctIdentifier, bool sessionUnavailable)
        {
            Mode = mode;
            KeyFamily = keyFamily;
            Policy = policy;
            RequireSignatureConfirmation = requireSignatureConfirmation;
            AllowDecryption = allowDecryption;
            AllowUnboundResponse = allowUnboundResponse;
            MaxPlaintextBytes = maxPlaintextBytes;
            DataEncryptionAlgorithm = dataEncryptionAlgorithm;
            KeyDerivationLabel = keyDerivationLabel;
            ExpectedCertificateThumbprint = expectedCertificateThumbprint;
            ExpectedCertificate = expectedCertificate;
            Nonces = nonces;
            SignatureValue = signatureValue;
            EncryptedKeySha1 = encryptedKeySha1;
            SessionSecret = sessionSecret;
            RequestDerivedKeys = requestDerivedKeys;
            MessageId = messageId;
            SctIdentifier = sctIdentifier;
            SessionUnavailable = sessionUnavailable;
        }

        /// <summary>
        ///     Gets the mode the request was built in.
        /// </summary>
        internal SoapSecurityModeType Mode { get; }

        /// <summary>
        ///     The key family of the request's primary signature, or null for a token-only request.
        /// </summary>
        internal SignatureKeyFamily? KeyFamily { get; }

        /// <summary>
        ///     Gets the response verification policy snapshot.
        /// </summary>
        internal SoapVerificationPolicyDto Policy { get; }

        /// <summary>
        ///     Gets a value indicating whether the response must carry a signed signature confirmation.
        /// </summary>
        internal bool RequireSignatureConfirmation { get; }

        /// <summary>
        ///     Gets a value indicating whether the response may be decrypted.
        /// </summary>
        internal bool AllowDecryption { get; }

        /// <summary>
        ///     Whether a response the check cannot bind to the request may be accepted. Otherwise a
        ///     request with no <c>wsa:MessageID</c> and no required confirmation is refused before any
        ///     verifier runs.
        /// </summary>
        internal bool AllowUnboundResponse { get; }

        /// <summary>
        ///     Whether the response can be bound to the request through its <c>wsa:MessageID</c>, a
        ///     required signature confirmation, or explicit acceptance of an unbound response.
        /// </summary>
        internal bool IsBindable => MessageId.IsNotNull() || RequireSignatureConfirmation || AllowUnboundResponse;

        /// <summary>
        ///     The security context token identifier the session was established under, which the
        ///     response's derived key token must reference. Null on any mode other than a secure
        ///     conversation.
        /// </summary>
        internal string SctIdentifier { get; }

        /// <summary>
        ///     Whether the secure conversation session was disposed before the response could be
        ///     checked, leaving no way to derive the response key.
        /// </summary>
        internal bool SessionUnavailable { get; }

        /// <summary>
        ///     Gets the largest decrypted plaintext accepted, in bytes.
        /// </summary>
        internal int MaxPlaintextBytes { get; }

        /// <summary>
        ///     Gets the data encryption algorithm URI the request encrypts with. A reply's encrypted
        ///     parts must name exactly this algorithm to be decrypted.
        /// </summary>
        internal string DataEncryptionAlgorithm { get; }

        /// <summary>
        ///     The derivation label the request's derived key tokens carry. A reply's tokens must carry
        ///     exactly this label.
        /// </summary>
        internal string KeyDerivationLabel { get; }

        /// <summary>
        ///     The expected response certificate's thumbprint, or null when none was configured.
        /// </summary>
        internal string ExpectedCertificateThumbprint { get; }

        /// <summary>
        ///     The expected response certificate's public DER encoding, or null when none was
        ///     configured.
        /// </summary>
        internal byte[] ExpectedCertificate { get; private set; }

        /// <summary>
        ///     Gets the nonces the request generated, base64 encoded as written on the wire.
        /// </summary>
        internal IReadOnlyList<string> Nonces { get; }

        /// <summary>
        ///     The request's primary signature value, or null for a token-only request.
        /// </summary>
        internal byte[] SignatureValue { get; private set; }

        /// <summary>
        ///     The SHA-1 of the request's encrypted key cipher value, or null when no encrypted key was
        ///     emitted.
        /// </summary>
        internal byte[] EncryptedKeySha1 { get; private set; }

        /// <summary>
        ///     The symmetric secret the request's keys derive from, or null on an asymmetric or
        ///     token-only request.
        /// </summary>
        internal byte[] SessionSecret { get; private set; }

        /// <summary>
        ///     Every key the request derived from its secret, or null on a mode that derives none. A
        ///     reply whose token derives one of them is refused as a reflection.
        /// </summary>
        internal byte[][] RequestDerivedKeys { get; private set; }

        /// <summary>
        ///     The request's <c>wsa:MessageID</c>, or null when none was emitted.
        /// </summary>
        internal string MessageId { get; }

        /// <summary>
        ///     Zeroes every byte array the view holds.
        /// </summary>
        public void Dispose()
        {
            ExpectedCertificate = Zero(ExpectedCertificate);
            SignatureValue = Zero(SignatureValue);
            EncryptedKeySha1 = Zero(EncryptedKeySha1);
            SessionSecret = Zero(SessionSecret);

            if (RequestDerivedKeys.IsNotNull())
            {
                foreach (var key in RequestDerivedKeys!)
                    Zero(key);

                RequestDerivedKeys = null;
            }
        }

        /// <summary>
        ///     Zeroes an array and forgets it.
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
