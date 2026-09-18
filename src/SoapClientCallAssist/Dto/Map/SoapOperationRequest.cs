// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapOperationRequest.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.Collections.Generic;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Dto.Map
{
    /// <summary>
    ///     A document/literal wrapped operation call, made of the operation element, the namespace
    ///     it and its parameter elements are emitted in, and the ordered arguments.
    /// </summary>
    public sealed class SoapOperationRequest
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapOperationRequest" /> class.
        /// </summary>
        public SoapOperationRequest()
            => Parameters = new List<SoapOperationParameter>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapOperationRequest" /> class.
        /// </summary>
        /// <param name="operationName">The local name of the operation element.</param>
        /// <param name="operationNamespace">Namespace of the operation and its parameters.</param>
        public SoapOperationRequest(string operationName, XNamespace operationNamespace)
            : this()
        {
            OperationName = operationName;
            OperationNamespace = operationNamespace;
        }

        /// <summary>
        ///     Gets or sets the local name of the operation element.
        /// </summary>
        /// <value>
        ///     The name of the operation.
        /// </value>
        public string OperationName { get; set; }

        /// <summary>
        ///     The namespace of the operation element and of its parameter elements. It must not be
        ///     empty; the emitter refuses an empty one.
        /// </summary>
        /// <value>
        ///     The operation namespace.
        /// </value>
        public XNamespace OperationNamespace { get; set; }

        /// <summary>
        ///     Gets the arguments of the operation, in the order the service declares them.
        /// </summary>
        /// <value>
        ///     The parameters.
        /// </value>
        public IList<SoapOperationParameter> Parameters { get; }

        /// <summary>
        ///     Appends one argument to <see cref="Parameters" />.
        /// </summary>
        /// <param name="name">The wire name of the parameter element.</param>
        /// <param name="value">The value to emit, or null to omit the element.</param>
        /// <returns>
        ///     This instance, for chained calls.
        /// </returns>
        public SoapOperationRequest AddParameter(string name, object value)
        {
            Parameters.Add(new SoapOperationParameter(name, value));

            return this;
        }
    }
}