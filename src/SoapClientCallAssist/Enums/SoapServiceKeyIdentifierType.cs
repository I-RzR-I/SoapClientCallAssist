// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapServiceKeyIdentifierType.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

namespace SoapClientCallAssist.Enums
{
    /// <summary>
    ///     How the service certificate an <c>EncryptedKey</c> is wrapped for is identified inside the
    ///     key's <c>KeyInfo</c>. Only <see cref="ThumbprintSha1" /> is emitted; the others are refused.
    /// </summary>
    public enum SoapServiceKeyIdentifierType
    {
        /// <summary>
        ///     A <c>wsse:KeyIdentifier</c> carrying the SHA-1 thumbprint of the service certificate.
        ///     The default, and what WCF emits.
        /// </summary>
        ThumbprintSha1 = 0,

        /// <summary>
        ///     A <c>wsse:KeyIdentifier</c> carrying the certificate's Subject Key Identifier extension.
        /// </summary>
        SubjectKeyIdentifier = 1,

        /// <summary>
        ///     A <c>ds:X509IssuerSerial</c> pair naming the issuer and the serial number.
        /// </summary>
        IssuerSerial = 2,

        /// <summary>
        ///     The whole service certificate embedded as a <c>wsse:BinarySecurityToken</c> and
        ///     referenced by <c>wsu:Id</c>.
        /// </summary>
        BinarySecurityToken = 3
    }
}
