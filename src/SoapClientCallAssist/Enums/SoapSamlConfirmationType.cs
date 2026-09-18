// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapSamlConfirmationType.cs" company="RzR SOFT & TECH">
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
    ///     The subject confirmation method a SAML assertion is presented under.
    /// </summary>
    public enum SoapSamlConfirmationType
    {
        /// <summary>
        ///     Bearer: possession of the assertion is the proof. The assertion is carried in the header
        ///     and nothing else is required of the caller.
        /// </summary>
        Bearer = 0,

        /// <summary>
        ///     Holder-of-key: the assertion names a key the caller must prove possession of, so the
        ///     message is also signed with <see cref="Dto.Public.SoapSecurityDto.SigningCertificate" />
        ///     and the signature's <c>KeyInfo</c> references the assertion.
        /// </summary>
        HolderOfKey = 1
    }
}
