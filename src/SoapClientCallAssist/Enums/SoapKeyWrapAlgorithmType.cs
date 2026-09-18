// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapKeyWrapAlgorithmType.cs" company="RzR SOFT & TECH">
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
    ///     The RSA key transport algorithms for wrapping the symmetric secret inside an
    ///     <c>EncryptedKey</c>.
    /// </summary>
    public enum SoapKeyWrapAlgorithmType
    {
        /// <summary>
        ///     RSA-OAEP with MGF1 and SHA-1. The default, matching the WCF <c>Basic256</c> algorithm
        ///     suite.
        /// </summary>
        [Description(EncryptedXml.XmlEncRSAOAEPUrl)]
        RsaOaepMgf1pSha1 = 0,

        /// <summary>
        ///     RSA PKCS#1 v1.5, the wrap of a WCF <c>*Rsa15</c> algorithm suite. Selecting it is refused.
        /// </summary>
        [Description(EncryptedXml.XmlEncRSA15Url)]
        Rsa15 = 1
    }
}
