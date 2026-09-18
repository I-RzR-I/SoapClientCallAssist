// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-03 19:32
//  ***********************************************************************
//  <copyright file="SoapDigestAlgorithmType.cs" company="RzR SOFT & TECH">
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
    ///     The XML digest algorithms supported when signing or verifying a WS-Security reference.
    /// </summary>
    public enum SoapDigestAlgorithmType
    {
        /// <summary>
        ///     SHA-256. The default digest algorithm.
        /// </summary>
        [Description(SignedXml.XmlDsigSHA256Url)] 
        Sha256 = 0,

        /// <summary>
        ///     SHA-512.
        /// </summary>
        [Description(SignedXml.XmlDsigSHA512Url)]
        Sha512 = 1,

        /// <summary>
        ///     SHA-1, accepted in a response only when
        ///     <see cref="Dto.Public.SoapVerificationPolicyDto.AllowSha1Algorithms" /> opts in.
        /// </summary>
        [Description(SignedXml.XmlDsigSHA1Url)]
        Sha1 = 2
    }
}