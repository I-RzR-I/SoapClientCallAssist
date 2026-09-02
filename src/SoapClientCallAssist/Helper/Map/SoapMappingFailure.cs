// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapMappingFailure.cs" company="RzR SOFT & TECH">
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
using SoapClientCallAssist.Extensions;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;
using Messages = SoapClientCallAssist.Helper.DefaultResultMessageHelper;

#endregion

namespace SoapClientCallAssist.Helper.Map
{
    /// <summary>
    ///     The single way the mapping layer builds a failed result. Every failure raised while
    ///     reading metadata, emitting a request or binding a response is constructed here, so
    ///     message text, message forwarding and the handling of a captured exception are decided
    ///     once.
    /// </summary>
    internal static class SoapMappingFailure
    {
        /// <summary>
        ///     Builds a validation failure for the supplied code and message arguments.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="code">The message code.</param>
        /// <param name="args">The message arguments.</param>
        /// <returns>
        ///     A failed IResult&lt;T&gt;.
        /// </returns>
        internal static IResult<T> Validation<T>(MessageCodes code, params object[] args)
            => Result<T>.Failure(
                code.GetDescription(),
                Messages.GetValidationMessage(code).TryFormatWith(args));
    }
}