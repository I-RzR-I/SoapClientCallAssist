// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-12 18:46
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapProtocolType.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

// ReSharper disable InconsistentNaming

namespace SoapClientCallAssist.Enums
{
    /// <summary>
    ///     The SOAP protocol versions a client can speak.
    /// </summary>
    public enum SoapProtocolType
    {
        /// <summary>
        ///     The SOAP 1.1 protocol.
        /// </summary>
        SOAP_1_1,

        /// <summary>
        ///     The SOAP 1.2 protocol.
        /// </summary>
        SOAP_1_2
    }
}