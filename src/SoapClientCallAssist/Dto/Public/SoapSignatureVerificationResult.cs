// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-03 20:35
//  ***********************************************************************
//  <copyright file="SoapSignatureVerificationResult.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;
using System.Collections.Generic;

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     What a cryptographically valid WS-Security signature covered.
    /// </summary>
    public class SoapSignatureVerificationResult
    {
        /// <summary>
        ///     Whether the SOAP Body at <c>/Envelope/Body</c> is the element the signature covers.
        /// </summary>
        /// <value>
        ///     True if body signed, false if not.
        /// </value>
        public bool BodySigned { get; set; }

        /// <summary>
        ///     Whether the <c>wsu:Timestamp</c> in the <c>wsse:Security</c> header is the element the
        ///     signature covers.
        /// </summary>
        /// <value>
        ///     True if timestamp signed, false if not.
        /// </value>
        public bool TimestampSigned { get; set; }

        /// <summary>
        ///     The <c>wsu:Created</c> instant of the signed timestamp, normalized to UTC, or
        ///     <see langword="null" /> when the timestamp is not signed or the instant is unreadable. 
        /// </summary>
        /// <value>
        ///     The created.
        /// </value>
        public DateTimeOffset? Created { get; set; }

        /// <summary>
        ///     The <c>wsu:Expires</c> instant of the signed timestamp, normalized to UTC, or
        ///     <see langword="null" /> under the same conditions as <see cref="Created" />.
        /// </summary>
        /// <value>
        ///     The expires.
        /// </value>
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        ///     The <c>wsu:Id</c> values every signature reference resolved to.
        /// </summary>
        /// <value>
        ///     A list of identifiers of the signed elements.
        /// </value>
        public IEnumerable<string> SignedElementIds { get; set; }

        /// <summary>
        ///     The local names of the elements every signature reference resolved to, in the same order
        ///     as <see cref="SignedElementIds" />.
        /// </summary>
        /// <value>
        ///     A list of names of the signed element locals.
        /// </value>
        public IEnumerable<string> SignedElementLocalNames { get; set; }
    }
}