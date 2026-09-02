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
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Values that represent SOAP namespace types.
    /// </summary>
    /// =================================================================================================
    internal enum SoapNamespaceType
    {
        /// <summary>
        ///     An enum constant representing the SOAP 1.1 option.
        /// </summary>
        [Description("http://schemas.xmlsoap.org/soap/envelope/")]
        Soap11,

        /// <summary>
        ///     An enum constant representing the SOAP 1.2 option.
        /// </summary>
        [Description("http://www.w3.org/2003/05/soap-envelope")]
        Soap12
    }
}