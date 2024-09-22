// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssist
//  Author           : RzR
//  Created On       : 2024-09-22 18:43
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-22 18:50
// ***********************************************************************
//  <copyright file="HttpClientDto.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using DomainCommonExtensions.CommonExtensions.TypeParam;
using System;
using System.Collections.Generic;
using System.Text;

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A HTTP client data transfer object.
    /// </summary>
    /// =================================================================================================
    public class HttpClientDto
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the endpoint.
        /// </summary>
        /// <value>
        ///     The endpoint.
        /// </value>
        /// =================================================================================================
        public Uri Endpoint { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the body encoding.
        /// </summary>
        /// <value>
        ///     The body encoding.
        /// </value>
        /// =================================================================================================
        public Encoding BodyEncoding { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets the HTTP client headers.
        /// </summary>
        /// <value>
        ///     The HTTP client headers.
        /// </value>
        /// =================================================================================================
        public Dictionary<string, IEnumerable<string>> HttpClientHeaders { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets or sets a value indicating whether the build get request as slash URL.
        ///     Build current SOAP GET request as URL with separated param by slash ex: 'http:/site.local/GetDocuments/1'
        /// </summary>
        /// <value>
        ///     True if build get request as slash url, false if not.
        /// </value>
        /// =================================================================================================
        public bool BuildGetRequestAsSlashUrl { get; set; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="HttpClientDto"/> class.
        /// </summary>
        /// =================================================================================================
        public HttpClientDto()
        {
            BuildGetRequestAsSlashUrl = false;
            BodyEncoding = Encoding.UTF8;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="HttpClientDto"/> class.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="bodyEncoding">The body encoding.</param>
        /// <param name="buildGetRequestAsSlashUrl">
        ///     (Optional) True to build get request as slash URL.
        /// </param>
        /// <param name="httpClientHeaders">
        ///     (Optional)
        ///     The HTTP client headers.
        /// </param>
        /// =================================================================================================
        public HttpClientDto(
            Uri endpoint,
            Encoding bodyEncoding = null,
            bool buildGetRequestAsSlashUrl = false,
            Dictionary<string, IEnumerable<string>> httpClientHeaders = null)
        {
            Endpoint = endpoint;
            BodyEncoding = bodyEncoding.IfIsNull(Encoding.UTF8);
            BuildGetRequestAsSlashUrl = buildGetRequestAsSlashUrl.IfIsNull(false);
            HttpClientHeaders = httpClientHeaders ?? new Dictionary<string, IEnumerable<string>>();
        }
    }
}