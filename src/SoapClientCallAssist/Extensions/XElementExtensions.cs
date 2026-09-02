// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 16:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
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
using SoapClientCallAssist.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Extensions
{
    /// <summary>
    ///     An element extensions.
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
        ///     Enumerates the element children of a parent that carry the supplied local name. Matching
        ///     is namespace agnostic, because a service is free to qualify a payload element differently
        ///     from the contract it publishes.
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
        ///     Determines whether an element is a SOAP body in either protocol namespace.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <returns>
        ///     True when the element is a SOAP body.
        /// </returns>
        internal static bool IsBody(this XElement element)
            => string.Equals(element.Name.LocalName, SoapContracts.BodyLocalName, StringComparison.Ordinal)
               && element.Name.NamespaceName.IsProtocolNamespace();
    }
}