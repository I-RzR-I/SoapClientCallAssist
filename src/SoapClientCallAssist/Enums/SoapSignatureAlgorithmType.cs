// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-03 19:33
//  ***********************************************************************
//  <copyright file="SoapSignatureAlgorithmType.cs" company="RzR SOFT & TECH">
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

// ReSharper disable InconsistentNaming

#endregion

namespace SoapClientCallAssist.Enums
{
    /// <summary>
    ///     The XML signature algorithms supported when signing or verifying a WS-Security message.
    /// </summary>
    public enum SoapSignatureAlgorithmType
    {
        /// <summary>
        ///     RSA with SHA-256. The default signature algorithm.
        /// </summary>
        [Description(SignedXml.XmlDsigRSASHA256Url)] 
        RsaSha256 = 0,

        /// <summary>
        ///     RSA with SHA-512.
        /// </summary>
        [Description(SignedXml.XmlDsigRSASHA512Url)] 
        RsaSha512 = 1,

        /// <summary>
        ///     RSA with SHA-1, accepted in a response only when
        ///     <see cref="Dto.Public.SoapVerificationPolicyDto.AllowSha1Algorithms" /> opts in.
        /// </summary>
        [Description(SignedXml.XmlDsigRSASHA1Url)]
        RsaSha1 = 2
    }
}