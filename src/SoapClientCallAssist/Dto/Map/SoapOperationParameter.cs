// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapOperationParameter.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

namespace SoapClientCallAssist.Dto.Map
{
    /// <summary>
    ///     One named argument of a SOAP operation. <see cref="Name" /> is the parameter name the
    ///     service declares, emitted as an element inside the operation wrapper.
    /// </summary>
    public sealed class SoapOperationParameter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapOperationParameter" /> class.
        /// </summary>
        /// <param name="name">The wire name of the parameter element.</param>
        /// <param name="value">The value to emit, or null to omit the element.</param>
        public SoapOperationParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        ///     Gets the wire name of the parameter element.
        /// </summary>
        /// <value>
        ///     The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        ///     Gets the value to emit. A <see langword="null" /> value omits the element entirely.
        /// </summary>
        /// <value>
        ///     The value.
        /// </value>
        public object Value { get; }
    }
}