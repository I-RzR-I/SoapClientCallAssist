// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssist
//  Author           : RzR
//  Created On       : 2024-09-15 17:20
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-15 18:41
// ***********************************************************************
//  <copyright file="BaseSoapRequestDto.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using DomainCommonExtensions.CommonExtensions.TypeParam;
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
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A base SOAP request data transfer object.
    /// </summary>
    /// =================================================================================================
    public class BaseSoapRequestDto
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     The body encoding.
        /// </summary>
        /// =================================================================================================
        private Encoding _bodyEncoding;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets URI of the SOAP.
        /// </summary>
        /// <value>
        ///     The SOAP URI.
        /// </value>
        /// =================================================================================================
        public Uri SoapUri { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the SOAP protocol.
        /// </summary>
        /// <value>
        ///     The SOAP protocol.
        /// </value>
        /// =================================================================================================
        public SoapProtocolType SoapProtocol { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the SOAP name space.
        /// </summary>
        /// <value>
        ///     The SOAP name space.
        /// </value>
        /// =================================================================================================
        public XNamespace SoapNameSpaceEnvelope { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the HTTP method.
        /// </summary>
        /// <value>
        ///     The method.
        /// </value>
        /// =================================================================================================
        public HttpMethod Method { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the type of the media.
        /// </summary>
        /// <value>
        ///     The type of the media.
        /// </value>
        /// =================================================================================================
        public string MediaType { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the body encoding.
        /// </summary>
        /// <value>
        ///     The body encoding.
        /// </value>
        /// =================================================================================================
        public Encoding BodyEncoding
        {
            get => _bodyEncoding;
            set => _bodyEncoding = value.IfIsNull(Encoding.UTF8);
        }

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
        public IEnumerable<XElement> Headers { get; set; } = null;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the action.
        /// </summary>
        /// <value>
        ///     The action.
        /// </value>
        /// =================================================================================================
        public string Action { get; set; } = null;

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
        ///     Gets or sets the HTTP client headers.
        /// </summary>
        /// <value>
        ///     The HTTP client headers.
        /// </value>
        /// =================================================================================================
        public IDictionary<string, IEnumerable<string>> HttpClientHeaders { get; set; } = null;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets a value indicating whether the build get request as parametrized URL with slash.
        /// </summary>
        /// <value>
        ///     True if build get request as URL with slash between params, false if not.
        /// </value>
        /// =================================================================================================
        public bool BuildGetRequestAsSlashUrl { get; set; } = false;
    }
}