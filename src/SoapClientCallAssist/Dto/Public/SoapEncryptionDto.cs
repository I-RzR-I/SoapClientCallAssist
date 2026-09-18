// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapEncryptionDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Enums;

// ReSharper disable RedundantDefaultMemberInitializer

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     Which parts of a symmetric-bound request are encrypted, and with what. It requires
    ///     <see cref="SoapSecurityDto.SymmetricBinding" /> or
    ///     <see cref="SoapSecurityDto.SecureConversation" /> and is refused otherwise.
    /// </summary>
    public class SoapEncryptionDto
    {
        /// <summary>
        ///     Whether the <c>UsernameToken</c> is encrypted, true by default. On a symmetric binding
        ///     it always is, and false there is refused.
        /// </summary>
        public bool EncryptUsernameToken { get; set; } = true;

        /// <summary>
        ///     Whether the SOAP Body is encrypted, false by default. It requires
        ///     <see cref="SoapResponseSecurityDto.AllowDecryption" /> on
        ///     <see cref="SoapSecurityDto.ResponseSecurity" /> and is refused without it.
        /// </summary>
        public bool EncryptBody { get; set; } = false;

        /// <summary>
        ///     Whether the primary signature is encrypted after it is computed. Defaults to false.
        /// </summary>
        public bool EncryptSignature { get; set; } = false;

        /// <summary>
        ///     The block cipher message parts are encrypted with. Defaults to
        ///     <see cref="SoapDataEncryptionAlgorithmType.Aes256Cbc" />.
        /// </summary>
        public SoapDataEncryptionAlgorithmType DataAlgorithm { get; set; } = SoapDataEncryptionAlgorithmType.Aes256Cbc;

        /// <summary>
        ///     The RSA key transport algorithm the secret is wrapped with. Defaults to
        ///     <see cref="SoapKeyWrapAlgorithmType.RsaOaepMgf1pSha1" />, and any other value is refused.
        /// </summary>
        public SoapKeyWrapAlgorithmType KeyWrap { get; set; } = SoapKeyWrapAlgorithmType.RsaOaepMgf1pSha1;
    }
}
