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
    ///     One named argument of a SOAP operation. The name is the wire name of the element emitted
    ///     inside the operation wrapper, which is the parameter name declared by the service, not
    ///     the name of the CLR type supplied as the value.
    /// </summary>
    public sealed class SoapOperationParameter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapOperationParameter" /> class.
        /// </summary>
        /// <param name="name">The wire name of the parameter element.</param>
        /// <param name="value">
        ///     The value to emit, or <see langword="null" /> to omit the element.
        /// </param>
        public SoapOperationParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        ///     Gets the wire name of the parameter element.
        /// </summary>
        /// <value>
        ///     The parameter name.
        /// </value>
        public string Name { get; }

        /// <summary>
        ///     Gets the value to emit. A <see langword="null" /> value omits the element entirely.
        /// </summary>
        /// <value>
        ///     The parameter value.
        /// </value>
        public object Value { get; }
    }
}