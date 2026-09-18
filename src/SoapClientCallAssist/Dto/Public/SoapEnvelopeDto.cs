// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-22 18:39
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapEnvelopeDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;
using System.Collections.Generic;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     The content of a SOAP envelope, made of the Body elements, the Header elements, the action
    ///     and any extra Envelope attributes.
    /// </summary>
    public class SoapEnvelopeDto
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapEnvelopeDto" /> class.
        /// </summary>
        public SoapEnvelopeDto() { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapEnvelopeDto" /> class.
        /// </summary>
        /// <param name="bodies">The elements placed inside the SOAP Body.</param>
        /// <param name="headers">
        ///     (Optional) The elements placed inside the SOAP Header, or null for none.
        /// </param>
        /// <param name="action">(Optional) The SOAP action, or null to send none.</param>
        /// <param name="ownSoapEnvelopeAttributes">
        ///     (Optional) Extra Envelope attributes, or null for none.
        /// </param>
        public SoapEnvelopeDto(IEnumerable<XElement> bodies, IEnumerable<XElement> headers = null,
            string action = null, IEnumerable<XAttribute> ownSoapEnvelopeAttributes = null)
        {
            Action = action;
            Bodies = bodies;
            Headers = headers ?? Array.Empty<XElement>();
            OwnSoapEnvelopeAttributes = ownSoapEnvelopeAttributes ?? Array.Empty<XAttribute>();
        }

        /// <summary>
        ///     The SOAP action, sent as the <c>SOAPAction</c> and <c>Action</c> HTTP headers and as an
        ///     <c>Action</c> header element; <see langword="null" /> sends none.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        ///     The elements placed inside the SOAP Body.
        /// </summary>
        public IEnumerable<XElement> Bodies { get; set; }

        /// <summary>
        ///     The elements placed inside the SOAP Header.
        /// </summary>
        public IEnumerable<XElement> Headers { get; set; }

        /// <summary>
        ///     Extra attributes added to the Envelope element beside the protocol namespace declaration.
        /// </summary>
        public IEnumerable<XAttribute> OwnSoapEnvelopeAttributes { get; set; }
    }
}