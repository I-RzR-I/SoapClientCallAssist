// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="WsSecurityHeaderBuilder.Timestamp.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Helpers;
using System;
using System.Globalization;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     The timestamp concern of the header builder.
    /// </summary>
    internal sealed partial class WsSecurityHeaderBuilder
    {
        /// <summary>
        ///     Emits the <c>wsu:Timestamp</c> and, when the options ask for it and the mode signs,
        ///     records it for the primary signature.
        /// </summary>
        private void EmitTimestamp()
        {
            var timestamp = _document.CreateElement(WsSecurityNames.WsuPrefix, WsSecurityNames.TimestampLocalName, WsSecurityNames.WsuNamespace);
            var timestampId = _ids.Stamp(_document, timestamp, "ts");

            var created = DateTime.UtcNow;
            var expires = created.Add(Options.TimestampTimeToLive);

            AppendTextChild(timestamp, WsSecurityNames.WsuPrefix, WsSecurityNames.CreatedLocalName, WsSecurityNames.WsuNamespace, Instant(created));
            AppendTextChild(timestamp, WsSecurityNames.WsuPrefix, WsSecurityNames.ExpiresLocalName, WsSecurityNames.WsuNamespace, Instant(expires));

            _security.AppendChild(timestamp);

            if (_plan.EmitsSignature && Options.SignTimestamp)
                _referenceIds.Add(timestampId);
        }

        /// <summary>
        ///     Formats a UTC instant the way every <c>wsu:Created</c> and <c>wsu:Expires</c> this
        ///     library writes is formatted.
        /// </summary>
        /// <param name="instant">The UTC instant.</param>
        /// <returns>
        ///     The formatted instant.
        /// </returns>
        private static string Instant(DateTime instant)
            => instant.ToString(WsSecurityNames.DateTimeFormat, CultureInfo.InvariantCulture);
    }
}
