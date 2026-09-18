// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapSymmetricSignatureAlgorithmType.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.ComponentModel;
using System.Security.Cryptography.Xml;

#endregion

namespace SoapClientCallAssist.Enums
{
    /// <summary>
    ///     The HMAC signature algorithms supported on a symmetric binding. An HMAC can never be chosen
    ///     for the asymmetric binding.
    /// </summary>
    public enum SoapSymmetricSignatureAlgorithmType
    {
        /// <summary>
        ///     HMAC with SHA-256. The default.
        /// </summary>
        [Description("http://www.w3.org/2001/04/xmldsig-more#hmac-sha256")]
        HmacSha256 = 0,

        /// <summary>
        ///     HMAC with SHA-1, the signature of a WCF <c>Basic256</c> or <c>Basic128</c> suite. It
        ///     needs <see cref="Dto.Public.SoapVerificationPolicyDto.AllowSha1Algorithms" /> and is
        ///     refused otherwise.
        /// </summary>
        [Description(SignedXml.XmlDsigHMACSHA1Url)]
        HmacSha1 = 1
    }
}
