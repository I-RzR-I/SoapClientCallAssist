// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapAddressingVersionType.cs" company="RzR SOFT & TECH">
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
    ///     The WS-Addressing version the addressing headers are written in.
    /// </summary>
    public enum SoapAddressingVersionType
    {
        /// <summary>
        ///     WS-Addressing 1.0 (<c>http://www.w3.org/2005/08/addressing</c>). The default, and what a
        ///     WCF <c>WSHttpBinding</c> expects.
        /// </summary>
        [Description("http://www.w3.org/2005/08/addressing")]
        WsAddressing10 = 0,

        /// <summary>
        ///     WS-Addressing August 2004 (<c>http://schemas.xmlsoap.org/ws/2004/08/addressing</c>), used
        ///     by <c>WSHttpBinding</c> configured with <c>WSAddressingAugust2004</c>.
        /// </summary>
        [Description("http://schemas.xmlsoap.org/ws/2004/08/addressing")]
        WsAddressingAugust2004 = 1
    }
}
