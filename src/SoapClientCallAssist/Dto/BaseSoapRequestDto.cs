// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-15 17:20
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-03 00:37
//  ***********************************************************************
//  <copyright file="BaseSoapRequestDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Reflection.TypeParam;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

// ReSharper disable RedundantDefaultMemberInitializer

#endregion

namespace SoapClientCallAssist.Dto
{
    /// <summary>
    ///     The resolved SOAP request a client assembles the envelope and the HTTP message from.
    /// </summary>
    public class BaseSoapRequestDto
    {
        /// <summary>
        ///     The body encoding.
        /// </summary>
        private Encoding _bodyEncoding;

        /// <summary>
        ///     The endpoint URI the request is sent to.
        /// </summary>
        /// <value>
        ///     The SOAP URI.
        /// </value>
        public Uri SoapUri { get; set; }

        /// <summary>
        ///     The SOAP protocol version the envelope is built for.
        /// </summary>
        /// <value>
        ///     The SOAP protocol.
        /// </value>
        public SoapProtocolType SoapProtocol { get; set; }

        /// <summary>
        ///     The envelope namespace of the protocol in use.
        /// </summary>
        /// <value>
        ///     The SOAP name space envelope.
        /// </value>
        public XNamespace SoapNameSpaceEnvelope { get; set; }

        /// <summary>
        ///     The HTTP method, POST or GET; any other method fails validation.
        /// </summary>
        /// <value>
        ///     The method.
        /// </value>
        public HttpMethod Method { get; set; }

        /// <summary>
        ///     The media type of the request content.
        /// </summary>
        /// <value>
        ///     The type of the media.
        /// </value>
        public string MediaType { get; set; }

        /// <summary>
        ///     The encoding of the request body; assigning <see langword="null" /> falls back to UTF-8.
        /// </summary>
        /// <value>
        ///     The body encoding.
        /// </value>
        public Encoding BodyEncoding
        {
            get => _bodyEncoding;
            set => _bodyEncoding = value.IfIsNull(Encoding.UTF8);
        }

        /// <summary>
        ///     The elements placed inside the SOAP Body.
        /// </summary>
        /// <value>
        ///     The bodies.
        /// </value>
        public IEnumerable<XElement> Bodies { get; set; }

        /// <summary>
        ///     The elements placed inside the SOAP Header, or <see langword="null" /> for no custom
        ///     headers.
        /// </summary>
        /// <value>
        ///     The headers.
        /// </value>
        public IEnumerable<XElement> Headers { get; set; } = null;

        /// <summary>
        ///     The SOAP action, sent as the <c>SOAPAction</c> and <c>Action</c> HTTP headers and as an
        ///     <c>Action</c> header element; <see langword="null" /> sends none.
        /// </summary>
        /// <value>
        ///     The action.
        /// </value>
        public string Action { get; set; } = null;

        /// <summary>
        ///     Extra attributes added to the Envelope element beside the protocol namespace declaration.
        /// </summary>
        /// <value>
        ///     The own SOAP envelope attributes.
        /// </value>
        public IEnumerable<XAttribute> OwnSoapEnvelopeAttributes { get; set; }

        /// <summary>
        ///     Additional HTTP headers added to the request message, or <see langword="null" /> for none.
        /// </summary>
        /// <value>
        ///     The HTTP client headers.
        /// </value>
        public IDictionary<string, IEnumerable<string>> HttpClientHeaders { get; set; } = null;

        /// <summary>
        ///     Whether a GET request appends body values as path segments (<c>/GetDocuments/1</c>), not
        ///     query string parameters. Defaults to false.
        /// </summary>
        /// <value>
        ///     True if build get request as slash url, false if not.
        /// </value>
        public bool BuildGetRequestAsSlashUrl { get; set; } = false;

        /// <summary>
        ///     The WS-Security message signing options, or <see langword="null" /> to send the request
        ///     unsigned.
        /// </summary>
        /// <value>
        ///     The security.
        /// </value>
        public SoapSecurityDto Security { get; set; }
    }
}