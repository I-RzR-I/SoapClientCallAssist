// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapSecureConversationDto.cs" company="RzR SOFT & TECH">
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
    ///     A WS-SecureConversation binding, keying the request by an established session. Setting it
    ///     selects the secure conversation mode, which requires a non-expired <see cref="Session" />
    ///     and <see cref="SoapSecurityDto.Addressing" /> and excludes
    ///     <see cref="SoapSecurityDto.SymmetricBinding" />.
    /// </summary>
    public class SoapSecureConversationDto
    {
        /// <summary>
        ///     The established session the request is keyed by. Required, and it must not be expired or
        ///     disposed.
        /// </summary>
        /// <value>
        ///     The session.
        /// </value>
        public SoapSecureConversationSession Session { get; set; }
    }
}
