// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapContractAttribute.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;

#endregion

namespace SoapClientCallAssist.Attributes
{
    /// <summary>
    ///     Marks a CLR type as a SOAP contract and customizes the XML element that represents it.
    /// </summary>
    /// <seealso cref="Attribute">
    ///     =================================================================================================
    /// </seealso>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SoapContractAttribute : Attribute
    {
        /// <summary>
        ///     Gets or sets the XML element local name used for this type. When <see langword="null" />,
        ///     the CLR type name is used.
        /// </summary>
        /// <value>
        ///     The element local name, or <see langword="null" /> to use the CLR type name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the XML namespace used for this type and, by default, for its members. When <see langword="null" />
        ///     , the namespace supplied by the call site is inherited.
        /// </summary>
        /// <value>
        ///     The XML namespace, or <see langword="null" /> to inherit from the call site.
        /// </value>
        public string Namespace { get; set; }
    }
}