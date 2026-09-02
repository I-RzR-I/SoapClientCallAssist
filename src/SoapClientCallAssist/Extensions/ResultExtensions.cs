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
    ///     A result extensions.
    /// </summary>
    internal static class ResultExtensions
    {
        /// <summary>
        ///     Re-raises a failure under a different result type, carrying over every message it holds.
        ///     A failure that names a concrete cause keeps naming it after it crosses a stage boundary,
        ///     including the sanitized exception a captured error appends after the first message. Every
        ///     failure this layer builds carries at least one message, so the empty branch exists only
        ///     so that a source with no message list cannot throw here; it is not a supported shape. 
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
        ///     Rebuilds a captured exception as one that names its type and what the library was doing,
        ///     but carries none of its own text and does not chain the original. An attached exception
        ///     reaches the consumer through <see cref="IResult.Messages" />, which renders both its
        ///     message and its full string, and an exception raised over a value routinely quotes that
        ///     value (<c>FormatException</c> and <c>XmlException</c> both do). The value may be a remote
        ///     response or a consumer secret, so neither the text nor the original may travel with the
        ///     failure.
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
        ///     Unwraps the exception a reflected call reports, so that the cause rather than the
        ///     invocation wrapper is named on the failure.
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