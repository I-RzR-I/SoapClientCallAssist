// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapValueKind.cs" company="RzR SOFT & TECH">
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
    ///     Describes how the value of a mapped member is represented in XML.
    /// </summary>
    internal enum SoapValueKind
    {
        /// <summary>
        ///     A scalar value written as element text. Covers primitives, string and byte arrays.
        /// </summary>
        Simple,

        /// <summary>
        ///     A nested type that carries its own map, resolved on demand.
        /// </summary>
        Complex,

        /// <summary>
        ///     A repeated value written as a wrapper element containing one child element per item.
        /// </summary>
        Collection
    }
}