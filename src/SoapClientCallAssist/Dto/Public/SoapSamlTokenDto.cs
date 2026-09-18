// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapSamlTokenDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Enums;
using System;
using System.Xml;

// ReSharper disable RedundantDefaultMemberInitializer

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     A SAML assertion, issued elsewhere, to carry in the Security header of a request.
    /// </summary>
    public class SoapSamlTokenDto
    {
        /// <summary>
        ///     The <c>saml:Assertion</c> element exactly as issued, required. Load it with an
        ///     <see cref="XmlDocument" /> whose <see cref="XmlDocument.PreserveWhitespace" /> is true;
        ///     LINQ to XML drops the whitespace the enveloped signature covers.
        /// </summary>
        /// <value>
        ///     The assertion.
        /// </value>
        public XmlElement Assertion { get; set; }

        /// <summary>
        ///     The subject confirmation the assertion is presented under,
        ///     <see cref="SoapSamlConfirmationType.Bearer" /> by default.
        ///     <see cref="SoapSamlConfirmationType.HolderOfKey" /> requires
        ///     <see cref="SoapSecurityDto.SigningCertificate" /> and is refused without it.
        /// </summary>
        /// <value>
        ///     The confirmation.
        /// </value>
        public SoapSamlConfirmationType Confirmation { get; set; } = SoapSamlConfirmationType.Bearer;

        /// <summary>
        ///     Whether the message signature covers a holder-of-key assertion, true by default; a bearer
        ///     one is never signed. Clear this and <see cref="SoapSecurityDto.SignBody" /> for a WCF
        ///     issued-token-over-transport service, which cannot resolve the reference.
        /// </summary>
        /// <value>
        ///     True if sign assertion, false if not.
        /// </value>
        public bool SignAssertion { get; set; } = true;

        /// <summary>
        ///     Whether a bearer assertion may be sent to a non-<c>https</c> endpoint, false by default;
        ///     the request is refused otherwise. It has no effect on a holder-of-key confirmation. 
        /// </summary>
        /// <value>
        ///     True if allow bearer over insecure transport, false if not.
        /// </value>
        public bool AllowBearerOverInsecureTransport { get; set; } = false;

        /// <summary>
        ///     The clock skew tolerated when the assertion's <c>NotOnOrAfter</c> is checked, five
        ///     minutes by default; a negative value counts as none. A lapsed assertion is refused. 
        /// </summary>
        /// <value>
        ///     The clock skew.
        /// </value>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
    }
}
