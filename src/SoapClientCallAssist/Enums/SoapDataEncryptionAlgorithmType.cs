// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapDataEncryptionAlgorithmType.cs" company="RzR SOFT & TECH">
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
    ///     The XML Encryption block ciphers supported for encrypting message parts.
    /// </summary>
    public enum SoapDataEncryptionAlgorithmType
    {
        /// <summary>
        ///     AES-256 in CBC mode. The default, matching the WCF <c>Basic256</c> algorithm suite.
        /// </summary>
        [Description(EncryptedXml.XmlEncAES256Url)]
        Aes256Cbc = 0,

        /// <summary>
        ///     AES-192 in CBC mode, matching the WCF <c>Basic192</c> algorithm suite.
        /// </summary>
        [Description(EncryptedXml.XmlEncAES192Url)]
        Aes192Cbc = 1,

        /// <summary>
        ///     AES-128 in CBC mode, matching the WCF <c>Basic128</c> algorithm suite.
        /// </summary>
        [Description(EncryptedXml.XmlEncAES128Url)]
        Aes128Cbc = 2
    }
}
