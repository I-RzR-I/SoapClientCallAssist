// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 02:30
//  ***********************************************************************
//  <copyright file="SoapResponseSecurityDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

// ReSharper disable RedundantDefaultMemberInitializer

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     What a response is allowed and required to carry when it is checked through
    ///     <see cref="Abstractions.ISoapResponseSecurity" /> against the request it answers.
    /// </summary>
    public class SoapResponseSecurityDto
    {
        /// <summary>
        ///     Whether an encrypted response may be decrypted, false by default. It is accepted only on
        ///     a symmetric binding or a secure conversation, and decryption never runs without
        ///     verification.
        /// </summary>
        /// <value>
        ///     True if allow decryption, false if not.
        /// </value>
        public bool AllowDecryption { get; set; } = false;

        /// <summary>
        ///     Whether the response must carry a signed WS-Security 1.1 <c>SignatureConfirmation</c>
        ///     echoing the request's signature value. <see langword="null" /> requires it on a symmetric
        ///     binding or secure conversation, not on the asymmetric one.
        /// </summary>
        /// <value>
        ///     The require signature confirmation.
        /// </value>
        public bool? RequireSignatureConfirmation { get; set; }

        /// <summary>
        ///     Whether a response that cannot be bound to its request is accepted, false by default.
        ///     Otherwise a request with no <c>wsa:MessageID</c> and no required
        ///     <c>SignatureConfirmation</c> fails its check.
        /// </summary>
        /// <value>
        ///     True if allow unbound response, false if not.
        /// </value>
        public bool AllowUnboundResponse { get; set; } = false;

        /// <summary>
        ///     The largest decrypted plaintext accepted, in bytes, or <see langword="null" /> for the
        ///     default of 4 MiB. A value not greater than zero is refused when the request is built.
        /// 
        /// </summary>
        /// <value>
        ///     The maximum plaintext bytes.
        /// </value>
        public int? MaxPlaintextBytes { get; set; }
    }
}
