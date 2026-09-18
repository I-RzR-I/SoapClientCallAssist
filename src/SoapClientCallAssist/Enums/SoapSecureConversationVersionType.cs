// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapSecureConversationVersionType.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.ComponentModel;

#endregion

namespace SoapClientCallAssist.Enums
{
    /// <summary>
    ///     The WS-SecureConversation and WS-Trust version pair a symmetric binding speaks. It names
    ///     the namespace of every <c>DerivedKeyToken</c> and <c>SecurityContextToken</c>, and the
    ///     WS-Trust dialect a session was issued in.
    /// </summary>
    public enum SoapSecureConversationVersionType
    {
        /// <summary>
        ///     The February 2005 drafts (<c>http://schemas.xmlsoap.org/ws/2005/02/sc</c> and
        ///     <c>http://schemas.xmlsoap.org/ws/2005/02/trust</c>). The default, and what a WCF binding
        ///     configured with <c>WSSecurity10</c> or <c>WSSecurity11</c> without <c>WSTrust13</c>
        ///     expects.
        /// </summary>
        [Description("http://schemas.xmlsoap.org/ws/2005/02/sc")]
        February2005 = 0,

        /// <summary>
        ///     The OASIS December 2005 standards
        ///     (<c>http://docs.oasis-open.org/ws-sx/ws-secureconversation/200512</c> and
        ///     <c>http://docs.oasis-open.org/ws-sx/ws-trust/200512</c>). What a WCF binding
        ///     configured with <c>WSTrust13</c> / <c>WSSecureConversation13</c> expects.
        /// </summary>
        [Description("http://docs.oasis-open.org/ws-sx/ws-secureconversation/200512")]
        December2005 = 1
    }
}
