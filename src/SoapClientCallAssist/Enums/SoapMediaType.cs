﻿// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssist
//  Author           : RzR
//  Created On       : 2024-09-15 18:14
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-15 18:40
// ***********************************************************************
//  <copyright file="SoapMediaType.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System.ComponentModel;

#endregion

namespace SoapClientCallAssist.Enums
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Values that represent SOAP media types.
    /// </summary>
    /// =================================================================================================
    internal enum SoapMediaType
    {
        /// <summary>
        ///     An enum constant representing the SOAP 1.1 option.
        /// </summary>
        [Description("text/xml")] 
        Soap11,

        /// <summary>
        ///     An enum constant representing the SOAP 1.2 option.
        /// </summary>
        [Description("application/soap+xml")] 
        Soap12
    }
}