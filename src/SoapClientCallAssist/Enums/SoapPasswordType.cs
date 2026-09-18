// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapPasswordType.cs" company="RzR SOFT & TECH">
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
    ///     How the password of a WS-Security <c>UsernameToken</c> is carried, per the Username Token
    ///     Profile 1.0.
    /// </summary>
    public enum SoapPasswordType
    {
        /// <summary>
        ///     The password is sent as text. Only meaningful over a transport that protects it, or when
        ///     the token is encrypted.
        /// </summary>
        [Description("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText")]
        Text = 0,

        /// <summary>
        ///     The password is sent as <c>Base64(SHA-1(nonce + created + password))</c>. A nonce and
        ///     a creation instant are always included.
        /// </summary>
        [Description("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest")]
        Digest = 1
    }
}
