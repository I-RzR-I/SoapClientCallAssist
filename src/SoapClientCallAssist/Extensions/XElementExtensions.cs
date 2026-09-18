// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 16:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-10 20:40
//  ***********************************************************************
//  <copyright file="XElementExtensions.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Text;
using SoapClientCallAssist.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Extensions
{
    /// <summary>
    ///     Element helpers for nil detection, local-name lookup and SOAP envelope structure.
    /// </summary>
    internal static class XElementExtensions
    {
        /// <summary>
        ///     Determines whether an element is marked nil.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <returns>
        ///     True when the element is nil.
        /// </returns>
        internal static bool IsNil(this XElement element)
        {
            var nil = element.Attribute(XName.Get(SoapContracts.NilLocalName, SoapContracts.SchemaInstanceNamespace));
            if (nil.IsNull())
                return false;

            var value = nil!.Value.Trim();

            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "1", StringComparison.Ordinal);
        }

        /// <summary>
        ///     Enumerates the element children of a parent that carry the supplied local name; matching
        ///     is namespace agnostic.
        /// </summary>
        /// <param name="parent">The parent element.</param>
        /// <param name="localName">The local name to match.</param>
        /// <returns>
        ///     The matching elements.
        /// </returns>
        internal static IEnumerable<XElement> ByLocalName(this XElement parent, string localName)
            => parent
                .Elements()
                .Where(x => string.Equals(x.Name.LocalName, localName, StringComparison.Ordinal));

        /// <summary>
        ///     Returns the first element child carrying the supplied local name.
        /// </summary>
        /// <param name="parent">The parent element.</param>
        /// <param name="localName">The local name to match.</param>
        /// <returns>
        ///     The first matching element, or null.
        /// </returns>
        internal static XElement FirstByLocalName(this XElement parent, string localName)
            => ByLocalName(parent, localName).FirstOrDefault();

        /// <summary>
        ///     Enumerates the item elements of a collection wrapper.
        /// </summary>
        /// <param name="wrapper">The wrapper element.</param>
        /// <param name="itemName">The configured item name, or null to accept any element child.</param>
        /// <returns>
        ///     The item elements.
        /// </returns>
        internal static IEnumerable<XElement> ItemElements(this XElement wrapper, string itemName)
            => itemName.IsMissing() ? wrapper.Elements() : wrapper.ByLocalName(itemName);

        /// <summary>
        ///     Determines whether an element is a SOAP envelope in either protocol namespace.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <returns>
        ///     True when the element is a SOAP envelope.
        /// </returns>
        internal static bool IsEnvelope(this XElement element)
            => string.Equals(element.Name.LocalName, SoapContracts.EnvelopeLocalName, StringComparison.Ordinal)
               && element.Name.NamespaceName.IsProtocolNamespace();

        /// <summary>
        ///     Returns the only element child of a parent that carries a local name in the parent's own
        ///     namespace.
        /// </summary>
        /// <param name="parent">The parent element.</param>
        /// <param name="localName">The local name to match.</param>
        /// <returns>
        ///     The single matching child, or null when there is none or more than one.
        /// </returns>
        internal static XElement SingleChildInOwnNamespace(this XElement parent, string localName)
        {
            var matched = parent.Elements(parent.Name.Namespace + localName).Take(2).ToList();

            return matched.Count == 1 ? matched[0] : null;
        }

        /// <summary>
        ///     Determines whether a parent carries at least one element child with a local name in the
        ///     parent's own namespace.
        /// </summary>
        /// <param name="parent">The parent element.</param>
        /// <param name="localName">The local name to match.</param>
        /// <returns>
        ///     True when such a child is present.
        /// </returns>
        internal static bool HasChildInOwnNamespace(this XElement parent, string localName)
            => parent.Elements(parent.Name.Namespace + localName).Any();
    }
}