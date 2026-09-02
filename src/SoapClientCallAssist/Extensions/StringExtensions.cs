// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 15:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="StringExtensions.cs" company="RzR SOFT & TECH">
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
using RzR.Extensions.Domain.Text;
using SoapClientCallAssist.Enums;
using System;
using System.Linq;

#endregion

namespace SoapClientCallAssist.Extensions
{
    /// <summary>
    ///     A string extensions.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        ///     Removes the arity suffix from a generic CLR type name, so that a name such as
        ///     <c>Wrapper`1</c> becomes a legal XML element name.
        /// </summary>
        /// <param name="name">The CLR type name.</param>
        /// <returns>
        ///     The name without its arity suffix.
        /// </returns>
        internal static string StripGenericArity(this string name)
        {
            var index = name.IndexOf('`');

            return index < 0 ? name : name.Substring(0, index);
        }

        /// <summary>
        ///     Returns the first value that carries content. An attribute property that was never
        ///     assigned is null, which is how an unset name or namespace falls through to the next
        ///     source.
        /// </summary>
        /// <param name="values">The candidate values, in precedence order.</param>
        /// <returns>
        ///     The first populated value, or null.
        /// </returns>
        internal static string FirstPresent(this string[] values) 
            => values.FirstOrDefault(x => x.IsPresent());

        /// <summary>
        ///     Returns the local part of a possibly prefixed element name.
        /// </summary>
        /// <param name="tag">The element name.</param>
        /// <returns>
        ///     The local part.
        /// </returns>
        internal static string LocalPartOf(this string tag)
        {
            var trimmed = tag.IfNullThenEmpty().Trim();
            var separator = trimmed.LastIndexOf(':');

            return separator < 0 ? trimmed : trimmed.Substring(separator + 1);
        }

        /// <summary>
        ///     Rewrites a flag list into the single form <see cref="Enum.Parse(Type,string)" />
        ///     understands. Both separators are accepted, so the XSD list form and the CLR form read the
        ///     same way.
        /// </summary>
        /// <param name="value">The trimmed element text.</param>
        /// <returns>
        ///     The comma separated flag list.
        /// </returns>
        internal static string NormalizeFlagList(this string value)
        {
            var parts = value
                .Replace(',', ' ')
                .Split(default(char[]), StringSplitOptions.RemoveEmptyEntries);

            return parts.Length < 2 ? value : string.Join(",", parts);
        }

        /// <summary>
        ///     Formats a message template, falling back to the raw template when the arguments do not
        ///     match its placeholders. Message construction must not be able to fail.
        /// </summary>
        /// <param name="template">The message template.</param>
        /// <param name="args">The message arguments.</param>
        /// <returns>
        ///     The formatted message.
        /// </returns>
        internal static string TryFormatWith(this string template, params object[] args)
        {
            if (args.IsNullOrEmptyEnumerable())
                return template;

            try
            {
                return template.FormatWith(args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        /// <summary>
        ///     Determines whether a namespace is one of the two SOAP envelope namespaces.
        /// </summary>
        /// <param name="ns">The namespace to test.</param>
        /// <returns>
        ///     True when the namespace is a SOAP envelope namespace.
        /// </returns>
        internal static bool IsProtocolNamespace(this string ns)
            => string.Equals(ns, SoapNamespaceType.Soap11.GetDescription(), StringComparison.Ordinal)
               || string.Equals(ns, SoapNamespaceType.Soap12.GetDescription(), StringComparison.Ordinal);
    }
}