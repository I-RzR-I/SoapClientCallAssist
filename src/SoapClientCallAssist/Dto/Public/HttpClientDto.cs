// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-22 18:43
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="HttpClientDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Reflection.TypeParam;
using System;
using System.Collections.Generic;
using System.Text;

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     The transport settings of a request, made of the endpoint, the body encoding, the GET URL
    ///     style and any extra HTTP headers.
    /// </summary>
    public class HttpClientDto
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="HttpClientDto" /> class.
        /// </summary>
        public HttpClientDto()
        {
            BuildGetRequestAsSlashUrl = false;
            BodyEncoding = Encoding.UTF8;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="HttpClientDto" /> class.
        /// </summary>
        /// <param name="endpoint">The endpoint URI the request is sent to.</param>
        /// <param name="bodyEncoding">The body encoding, or null for UTF-8.</param>
        /// <param name="buildGetRequestAsSlashUrl">True to send GET values as path segments.</param>
        /// <param name="httpClientHeaders">Extra HTTP headers, or null for none.</param>
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

        /// <summary>
        ///     The endpoint URI the request is sent to.
        /// </summary>
        public Uri Endpoint { get; set; }

        /// <summary>
        ///     The encoding of the request body. Defaults to UTF-8.
        /// </summary>
        public Encoding BodyEncoding { get; set; }

        /// <summary>
        ///     Additional HTTP headers added to the request message.
        /// </summary>
        public Dictionary<string, IEnumerable<string>> HttpClientHeaders { get; set; }

        /// <summary>
        ///     Whether a GET request appends body values as path segments
        ///     (<c>http://site.local/GetDocuments/1</c>), not query string parameters. Defaults to
        ///     false.
        /// </summary>
        public bool BuildGetRequestAsSlashUrl { get; set; }
    }
}