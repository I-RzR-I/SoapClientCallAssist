// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 20:35
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 00:40
//  ***********************************************************************
//  <copyright file="SoapVerificationPolicyDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;

// ReSharper disable RedundantDefaultMemberInitializer

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     The conditions a WS-Security signature must meet before verification is reported as
    ///     successful.
    /// </summary>
    public class SoapVerificationPolicyDto
    {
        /// <summary>
        ///     Whether verification fails unless the SOAP Body at <c>/Envelope/Body</c> is the very
        ///     element a signature reference covers. Defaults to true.
        /// </summary>
        /// <value>
        ///     True if require body signed, false if not.
        /// </value>
        public bool RequireBodySigned { get; set; } = true;

        /// <summary>
        ///     Whether verification fails unless the signature covers a <c>wsu:Timestamp</c> with a
        ///     readable UTC <c>wsu:Created</c> and <c>wsu:Expires</c> pair whose window contains the
        ///     current instant within <see cref="ClockSkew" />. Defaults to true.
        /// </summary>
        /// <value>
        ///     True if require valid timestamp, false if not.
        /// </value>
        public bool RequireValidTimestamp { get; set; } = true;

        /// <summary>
        ///     The tolerance applied to both ends of the timestamp validity window. Defaults to five
        ///     minutes.
        /// </summary>
        /// <value>
        ///     The clock skew.
        /// </value>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        ///     Whether SHA-1 is accepted as a signature method (<c>RSA-SHA1</c>) or as a reference
        ///     digest method. Defaults to false.
        /// </summary>
        /// <value>
        ///     True if allow sha 1 algorithms, false if not.
        /// </value>
        public bool AllowSha1Algorithms { get; set; } = false;

        /// <summary>
        ///     Whether a decryption failure names the stage it failed in, false by default and not for
        ///     production. Otherwise all of them share one generic failure with no cause.
        /// </summary>
        /// <value>
        ///     True if diagnostic detail, false if not.
        /// </value>
        public bool DiagnosticDetail { get; set; } = false;
    }
}
