// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-08 23:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-08 23:10
//  ***********************************************************************
//  <copyright file="WsuIdLookup.cs" company="RzR SOFT & TECH">
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
using System.Xml;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     Resolves elements by their WS-Security <c>wsu:Id</c> attribute by walking the document
    ///     and comparing the namespace-qualified attribute ordinally, never through an XPath
    ///     expression.
    /// </summary>
    internal static class WsuIdLookup
    {
        /// <summary>
        ///     Finds every element in the document whose <c>wsu:Id</c> attribute holds the supplied
        ///     value, in document order.
        /// </summary>
        /// <param name="document">The document to search. A null document yields no matches.</param>
        /// <param name="id">The id to match ordinally; a null or empty id yields no matches.</param>
        /// <returns>
        ///     The matching elements, in document order. The list is empty when nothing matched; it is
        ///     never null.
        /// </returns>
        internal static List<XmlElement> FindById(XmlDocument document, string id)
        {
            var matches = new List<XmlElement>();

            if (document.IsNull() || id.IsNullOrEmpty())
                return matches;

            foreach (var candidate in document.GetElementsByTagName("*"))
            {
                if (candidate is XmlElement element && CarriesId(element, id))
                    matches.Add(element);
            }

            return matches;
        }

        /// <summary>
        ///     Determines whether an element carries a <c>wsu:Id</c> attribute holding the supplied
        ///     value, reading the attribute by namespace and local name, whatever its prefix.
        /// </summary>
        /// <param name="element">The element to inspect.</param>
        /// <param name="id">The id to match, ordinally.</param>
        /// <returns>
        ///     True when the element is identified by the supplied id.
        /// </returns>
        private static bool CarriesId(XmlElement element, string id)
        {
            if (element.HasAttribute(WsSecurityNames.IdLocalName, WsSecurityNames.WsuNamespace).IsFalse())
                return false;

            return string.Equals(
                element.GetAttribute(WsSecurityNames.IdLocalName, WsSecurityNames.WsuNamespace), id, StringComparison.Ordinal);
        }
    }
}
