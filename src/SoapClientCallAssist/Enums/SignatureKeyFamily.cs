// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SignatureKeyFamily.cs" company="RzR SOFT & TECH">
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
    ///     The family of key an XML signature is computed or checked with, always stated explicitly
    ///     by the code that emits or inspects a signature and never defaulted.
    /// </summary>
    internal enum SignatureKeyFamily
    {
        /// <summary>
        ///     An RSA key pair: the request is signed with a certificate's private key and the response
        ///     is checked with a certificate's public key.
        /// </summary>
        Rsa = 1,

        /// <summary>
        ///     A shared symmetric secret: both sides compute an HMAC over the same derived key.
        /// </summary>
        Hmac = 2
    }
}
