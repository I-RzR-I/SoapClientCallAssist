// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-15 18:14
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapMediaType.cs" company="RzR SOFT & TECH">
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
    ///     The HTTP media types of a SOAP request body, one per protocol version.
    /// </summary>
    internal enum SoapMediaType
    {
        /// <summary>
        ///     The SOAP 1.1 media type, <c>text/xml</c>.
        /// </summary>
        [Description("text/xml")] 
        Soap11,

        /// <summary>
        ///     The SOAP 1.2 media type, <c>application/soap+xml</c>.
        /// </summary>
        [Description("application/soap+xml")] 
        Soap12
    }
}