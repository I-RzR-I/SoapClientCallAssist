// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-15 18:16
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapNamespaceType.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.ComponentModel;

#endregion

namespace SoapClientCallAssist.Enums
{
    /// <summary>
    ///     The SOAP envelope namespaces, one per protocol version.
    /// </summary>
    internal enum SoapNamespaceType
    {
        /// <summary>
        ///     The SOAP 1.1 envelope namespace.
        /// </summary>
        [Description("http://schemas.xmlsoap.org/soap/envelope/")]
        Soap11,

        /// <summary>
        ///     The SOAP 1.2 envelope namespace.
        /// </summary>
        [Description("http://www.w3.org/2003/05/soap-envelope")]
        Soap12
    }
}