// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssist
//  Author           : RzR
//  Created On       : 2024-09-22 18:39
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-22 18:50
// ***********************************************************************
//  <copyright file="SoapEnvelopeDto.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System;
using System.Collections.Generic;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A SOAP envelope data transfer object.
    /// </summary>
    /// =================================================================================================
    public class SoapEnvelopeDto
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the action.
        /// </summary>
        /// <value>
        ///     The action.
        /// </value>
        /// =================================================================================================
        public string Action { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the bodies.
        /// </summary>
        /// <value>
        ///     The bodies.
        /// </value>
        /// =================================================================================================
        public IEnumerable<XElement> Bodies { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the headers.
        /// </summary>
        /// <value>
        ///     The headers.
        /// </value>
        /// =================================================================================================
        public IEnumerable<XElement> Headers { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the own SOAP envelope attributes.
        /// </summary>
        /// <value>
        ///     The own SOAP envelope attributes.
        /// </value>
        /// =================================================================================================
        public IEnumerable<XAttribute> OwnSoapEnvelopeAttributes { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapEnvelopeDto"/> class.
        /// </summary>
        /// =================================================================================================
        public SoapEnvelopeDto()
        {
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapEnvelopeDto"/> class.
        /// </summary>
        /// <param name="bodies">The bodies.</param>
        /// <param name="headers">
        ///     (Optional)
        ///     The headers.
        /// </param>
        /// <param name="action">
        ///     (Optional)
        ///     The action.
        /// </param>
        /// <param name="ownSoapEnvelopeAttributes">
        ///     (Optional)
        ///     The own SOAP envelope attributes.
        /// </param>
        /// =================================================================================================
        public SoapEnvelopeDto(
            IEnumerable<XElement> bodies, 
            IEnumerable<XElement> headers = null,
            string action = null,
            IEnumerable<XAttribute> ownSoapEnvelopeAttributes = null)
        {
            Action = action;
            Bodies = bodies;
            Headers = headers ?? Array.Empty<XElement>();
            OwnSoapEnvelopeAttributes = ownSoapEnvelopeAttributes ?? Array.Empty<XAttribute>();
        }
    }
}