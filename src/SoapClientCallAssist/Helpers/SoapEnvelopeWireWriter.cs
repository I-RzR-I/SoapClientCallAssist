// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapEnvelopeWireWriter.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using System;
using System.Xml.Linq;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     The single place a built SOAP envelope is turned into the exact string sent on the wire.
    /// </summary>
    internal static class SoapEnvelopeWireWriter
    {
        /// <summary>
        ///     Writes a SOAP envelope compact and, when security is enabled, signs it and mints the
        ///     response check's key material. A custom signer is refused on a symmetric binding or a
        ///     secure conversation.
        /// </summary>
        /// <param name="soapEnvelope">The built SOAP envelope, not yet serialized.</param>
        /// <param name="security">The message security options, or null to sign nothing.</param>
        /// <param name="transportAction">The request's own action, or null.</param>
        /// <param name="endpoint">The endpoint the request is built for, or null.</param>
        /// <param name="material">[out] Key material, or null when the library signed nothing.</param>
        /// <returns>
        ///     An IResult&lt;string&gt; carrying the wire representation, or a failed result for a
        ///     custom signer on a symmetric binding or a secure conversation.
        /// </returns>
        internal static IResult<string> Write(XElement soapEnvelope, SoapSecurityDto security, string transportAction,
            Uri endpoint, out RequestKeyMaterial material)
        {
            material = null;

            if (security.IsNull() || security!.Enabled.IsFalse())
                return Result<string>.Success(soapEnvelope.ToString(SaveOptions.DisableFormatting));

            if (security.Signer.IsNull())
                return new WsSecurityMessageSigner().SignCore(soapEnvelope, security, transportAction, endpoint, out material);

            if (SoapSecurityPlanner.IsSymmetricFamily(security))
                return Result<string>.Failure(MessageCodes.V_SEC_035.GetDescription(), Messages.GetValidationMessage(MessageCodes.V_SEC_035));

            return security.Signer.Sign(soapEnvelope, security);
        }
    }
}
