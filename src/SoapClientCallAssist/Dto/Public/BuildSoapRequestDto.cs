// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-22 18:33
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-03 00:37
//  ***********************************************************************
//  <copyright file="BuildSoapRequestDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     The input of a request build, grouping the transport settings, the envelope content and
    ///     the optional WS-Security options.
    /// </summary>
    public class BuildSoapRequestDto
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BuildSoapRequestDto" /> class.
        /// </summary>
        public BuildSoapRequestDto()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BuildSoapRequestDto" /> class.
        /// </summary>
        /// <param name="client">The transport settings of the call.</param>
        /// <param name="envelope">The envelope content of the call.</param>
        public BuildSoapRequestDto(HttpClientDto client, SoapEnvelopeDto envelope)
        {
            Client = client;
            Envelope = envelope;
        }

        /// <summary>
        ///     The endpoint, body encoding, GET URL style and extra HTTP headers of the call.
        /// </summary>
        public HttpClientDto Client { get; set; }

        /// <summary>
        ///     The bodies, headers, action and Envelope attributes of the message.
        /// </summary>
        public SoapEnvelopeDto Envelope { get; set; }

        /// <summary>
        ///     The WS-Security message signing options, or <see langword="null" /> to send the request
        ///     unsigned.
        /// </summary>
        public SoapSecurityDto Security { get; set; }
    }
}