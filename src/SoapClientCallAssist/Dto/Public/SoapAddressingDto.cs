// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapAddressingDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Enums;
using System;

// ReSharper disable RedundantDefaultMemberInitializer

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     The WS-Addressing headers (<c>wsa:To</c>, <c>wsa:Action</c>, <c>wsa:MessageID</c>,
    ///     <c>wsa:ReplyTo</c>) added to a request. Every emitted header is signed, and a symmetric
    ///     binding requires them.
    /// </summary>
    public class SoapAddressingDto
    {
        /// <summary>
        ///     The absolute destination written into <c>wsa:To</c>, or <see langword="null" /> to use
        ///     the endpoint the request is built for.
        /// </summary>
        public Uri To { get; set; }

        /// <summary>
        ///     The action written into <c>wsa:Action</c>, or <see langword="null" /> to take the
        ///     request's own action. When both are supplied they must be equal; two different values
        ///     are refused.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        ///     Whether a fresh <c>wsa:MessageID</c> (<c>urn:uuid:</c>) is generated, true by default.
        ///     When present, a verified response must carry a signed <c>wsa:RelatesTo</c> equal to it.
        /// </summary>
        public bool IncludeMessageId { get; set; } = true;

        /// <summary>
        ///     Whether a <c>wsa:ReplyTo</c> naming the anonymous address is included. Defaults to true.
        /// </summary>
        public bool IncludeReplyTo { get; set; } = true;

        /// <summary>
        ///     The WS-Addressing version the headers are written in. Defaults to
        ///     <see cref="SoapAddressingVersionType.WsAddressing10" />.
        /// </summary>
        public SoapAddressingVersionType Version { get; set; } = SoapAddressingVersionType.WsAddressing10;
    }
}
