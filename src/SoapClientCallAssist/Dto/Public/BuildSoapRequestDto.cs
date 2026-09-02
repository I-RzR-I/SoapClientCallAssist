// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-22 18:33
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
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
    ///     A build SOAP request data transfer object.
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
        /// <param name="client">The client.</param>
        /// <param name="envelope">The envelope.</param>
        public BuildSoapRequestDto(HttpClientDto client, SoapEnvelopeDto envelope)
        {
            Client = client;
            Envelope = envelope;
        }

        /// <summary>
        ///     Gets or sets the client.
        /// </summary>
        /// <value>
        ///     The client.
        /// </value>
        public HttpClientDto Client { get; set; }

        /// <summary>
        ///     Gets or sets the envelope.
        /// </summary>
        /// <value>
        ///     The envelope.
        /// </value>
        public SoapEnvelopeDto Envelope { get; set; }
    }
}