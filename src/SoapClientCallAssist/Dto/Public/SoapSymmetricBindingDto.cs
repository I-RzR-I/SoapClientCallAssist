// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapSymmetricBindingDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Enums;
using System.Security.Cryptography.X509Certificates;

// ReSharper disable RedundantDefaultMemberInitializer

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     A WS-Security symmetric binding keyed by an <c>EncryptedKey</c> wrapped for the service
    ///     certificate. The signing and encryption keys of the request and of the response are
    ///     derived from its secret.
    /// </summary>
    public class SoapSymmetricBindingDto
    {
        /// <summary>
        ///     The service certificate the secret is wrapped for, required; only its public key is used.
        ///     Sharing a public key with <see cref="SoapSecurityDto.SigningCertificate" /> is refused.
        /// 
        /// </summary>
        /// <value>
        ///     The service certificate.
        /// </value>
        public X509Certificate2 ServiceCertificate { get; set; }

        /// <summary>
        ///     How the service certificate is identified inside the encrypted key. Defaults to
        ///     <see cref="SoapServiceKeyIdentifierType.ThumbprintSha1" />, and any other value is
        ///     refused.
        /// </summary>
        /// <value>
        ///     The identifier of the service key.
        /// </value>
        public SoapServiceKeyIdentifierType ServiceKeyIdentifier { get; set; } = SoapServiceKeyIdentifierType.ThumbprintSha1;

        /// <summary>
        ///     The WS-SecureConversation version the derived key tokens are written in. Defaults to
        ///     <see cref="SoapSecureConversationVersionType.February2005" />.
        /// </summary>
        /// <value>
        ///     The version.
        /// </value>
        public SoapSecureConversationVersionType Version { get; set; } = SoapSecureConversationVersionType.February2005;

        /// <summary>
        ///     The HMAC algorithm the message signature is computed with. Defaults to
        ///     <see cref="SoapSymmetricSignatureAlgorithmType.HmacSha256" />; HMAC-SHA1 needs
        ///     <see cref="SoapVerificationPolicyDto.AllowSha1Algorithms" />.
        /// </summary>
        /// <value>
        ///     The signature algorithm.
        /// </value>
        public SoapSymmetricSignatureAlgorithmType SignatureAlgorithm { get; set; } = SoapSymmetricSignatureAlgorithmType.HmacSha256;

        /// <summary>
        ///     The length in bytes of the key derived for signing, 24 by default; 16 to 64 is accepted.
        ///     WCF derives 24 bytes for every <c>Basic*</c> suite.
        /// </summary>
        /// <value>
        ///     The length of the signature key.
        /// </value>
        public int SignatureKeyLength { get; set; } = 24;

        /// <summary>
        ///     The length in bytes of the key derived for encryption, 32 by default. It must match the
        ///     block cipher, 32 for AES-256, 24 for AES-192, 16 for AES-128.
        /// </summary>
        /// <value>
        ///     The length of the encryption key.
        /// </value>
        public int EncryptionKeyLength { get; set; } = 32;

        /// <summary>
        ///     Whether the primary signature is endorsed by a second signature computed with
        ///     <see cref="SoapSecurityDto.SigningCertificate" />, whose single reference is the primary
        ///     signature. Defaults to false.
        /// </summary>
        /// <value>
        ///     True if endorse with signing certificate, false if not.
        /// </value>
        public bool EndorseWithSigningCertificate { get; set; } = false;

        /// <summary>
        ///     The label the keys are derived with, or <see langword="null" /> for the specification's
        ///     default label.
        /// </summary>
        /// <value>
        ///     The key derivation label.
        /// </value>
        public string KeyDerivationLabel { get; set; }
    }
}
