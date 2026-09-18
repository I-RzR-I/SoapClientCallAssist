// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 02:30
//  ***********************************************************************
//  <copyright file="SoapUsernameTokenDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Enums;

// ReSharper disable RedundantDefaultMemberInitializer

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     A WS-Security <c>UsernameToken</c> (Username Token Profile 1.0) carried in the Security
    ///     header of a request. Alone it produces a token-only header, and a symmetric binding
    ///     always encrypts it.
    /// </summary>
    public class SoapUsernameTokenDto
    {
        /// <summary>
        ///     Gets or sets the username. Required; a token naming nobody is refused.
        /// </summary>
        /// <value>
        ///     The username.
        /// </value>
        public string Username { get; set; }

        /// <summary>
        ///     The password, or <see langword="null" /> to emit no <c>wsse:Password</c> element.
        ///     Required when <see cref="PasswordType" /> is <see cref="SoapPasswordType.Digest" />, and
        ///     never copied into any failure message.
        /// </summary>
        /// <value>
        ///     The password.
        /// </value>
        public string Password { get; set; }

        /// <summary>
        ///     How the password is carried. Defaults to <see cref="SoapPasswordType.Text" />.
        /// </summary>
        /// <value>
        ///     The type of the password.
        /// </value>
        public SoapPasswordType PasswordType { get; set; } = SoapPasswordType.Text;

        /// <summary>
        ///     Whether a fresh 16-byte random <c>wsse:Nonce</c> is included, true by default. A digest
        ///     token always includes one, whatever this says.
        /// </summary>
        /// <value>
        ///     True if include nonce, false if not.
        /// </value>
        public bool IncludeNonce { get; set; } = true;

        /// <summary>
        ///     Whether the token carries a UTC <c>wsu:Created</c> instant, true by default. A digest
        ///     token always includes one, whatever this says.
        /// </summary>
        /// <value>
        ///     True if include created, false if not.
        /// </value>
        public bool IncludeCreated { get; set; } = true;

        /// <summary>
        ///     Whether the token is covered by the message signature, false by default. A token-only
        ///     header has no signature, and asking for one there is refused.
        /// </summary>
        /// <value>
        ///     True if sign token, false if not.
        /// </value>
        public bool SignToken { get; set; } = false;

        /// <summary>
        ///     Whether an unencrypted text password may travel to an endpoint that is not
        ///     <c>https</c>, false by default; the request is refused otherwise.
        ///     Digest passwords and symmetric bindings are unaffected.
        /// </summary>
        /// <value>
        ///     True if allow text password over insecure transport, false if not.
        /// </value>
        public bool AllowTextPasswordOverInsecureTransport { get; set; } = false;
    }
}
