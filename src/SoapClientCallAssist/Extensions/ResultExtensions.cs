// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 00:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="ResultExtensions.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Collections;
using RzR.Extensions.Domain.Primitives;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using RzR.ResultMessage.Extensions.Result;
using System;
using System.Reflection;

#endregion

namespace SoapClientCallAssist.Extensions
{
    /// <summary>
    ///     Extensions that carry a failure across result types and attach a sanitized exception to it.
    /// </summary>
    internal static class ResultExtensions
    {
        /// <summary>
        ///     Re-raises a failure under a different result type, carrying over every message it holds.
        ///     A source with no messages yields a bare failure.
        /// </summary>
        /// <typeparam name="T">The result type to raise the failure under.</typeparam>
        /// <param name="source">The failed result.</param>
        /// <returns>
        ///     A failed IResult&lt;T&gt;.
        /// </returns>
        internal static IResult<T> Propagate<T>(this IResult source)
        {
            var failure = Result<T>.Failure();

            return source.Messages.IsNullOrEmptyEnumerable()
                ? failure
                : failure.WithMessages(source.Messages);
        }

        /// <summary>
        ///     Attaches a captured exception to a failure, stripped of its own text.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="failure">The failed result.</param>
        /// <param name="exception">The captured exception, or null when there is none.</param>
        /// <param name="context">What the library was doing when the exception was raised.</param>
        /// <returns>
        ///     A failed IResult&lt;T&gt;.
        /// </returns>
        internal static IResult<T> WithOptionalError<T>(this Result<T> failure, Exception exception, string context)
            => exception.IsNull() ? failure : failure.WithError(Sanitized(exception, context));

        /// <summary>
        ///     Rebuilds a captured exception as an <see cref="InvalidOperationException" /> naming only
        ///     its type and what the library was doing, with none of its own text and no inner exception. 
        /// </summary>
        /// <param name="exception">The captured exception.</param>
        /// <param name="context">What the library was doing when the exception was raised.</param>
        /// <returns>
        ///     The sanitized exception.
        /// </returns>
        private static Exception Sanitized(Exception exception, string context)
            => new InvalidOperationException(
                $"'{Unwrap(exception).GetType().FullName}' was raised while {context}. Its own message is "
                + "withheld because a message built over a value can carry that value.");

        /// <summary>
        ///     Unwraps a <see cref="TargetInvocationException" /> to its inner exception; any other
        ///     exception is returned as is.
        /// </summary>
        /// <param name="exception">The captured exception.</param>
        /// <returns>
        ///     The unwrapped exception.
        /// </returns>
        private static Exception Unwrap(Exception exception)
            => exception is TargetInvocationException invocation && invocation.InnerException.IsNotNull()
                ? invocation.InnerException
                : exception;
    }
}