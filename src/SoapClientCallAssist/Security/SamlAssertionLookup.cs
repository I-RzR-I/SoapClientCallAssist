// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 22:15
//
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 22:15
//  ***********************************************************************
//  <copyright file="SamlAssertionLookup.cs" company="RzR SOFT & TECH">
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
    ///     Resolves <c>saml:Assertion</c> elements by walking the document and comparing the
    ///     identifier attribute of their version ordinally, <c>ID</c> for SAML 2.0 and
    ///     <c>AssertionID</c> for SAML 1.1.
    /// </summary>
    internal static class SamlAssertionLookup
    {
        /// <summary>
        ///     Finds every assertion in the document whose identifier holds the supplied value, in
        ///     document order.
        /// </summary>
        /// <param name="document">The document to search. A null document yields no matches.</param>
        /// <param name="id">The id to match ordinally; a null or empty id yields no matches.</param>
        /// <returns>
        ///     The matching assertions. The list is empty when nothing matched; it is never null.
        /// </returns>
        internal static List<XmlElement> FindById(XmlDocument document, string id)
        {
            var matches = new List<XmlElement>();

            if (document.IsNull() || id.IsNullOrEmpty())
                return matches;

            foreach (var candidate in document.GetElementsByTagName("*"))
            {
                if (candidate is XmlElement element && string.Equals(IdOf(element), id, StringComparison.Ordinal))
                    matches.Add(element);
            }

            return matches;
        }

        /// <summary>
        ///     Determines whether an element is a <c>saml:Assertion</c> of SAML 1.1 or SAML 2.0.
        /// </summary>
        /// <param name="element">The element, or null.</param>
        /// <returns>
        ///     True when the element is an assertion of either version.
        /// </returns>
        internal static bool IsAssertion(XmlElement element)
            => element.IsNotNull()
               && string.Equals(element!.LocalName, SamlNames.AssertionLocalName, StringComparison.Ordinal)
               && SamlNames.IsAssertionNamespace(element.NamespaceURI);

        /// <summary>
        ///     Reads the identifier of an assertion: the unqualified <c>ID</c> of a SAML 2.0 assertion
        ///     or the unqualified <c>AssertionID</c> of a SAML 1.1 one.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>
        ///     The identifier, or null when the element is not an assertion or carries none.
        /// </returns>
        internal static string IdOf(XmlElement element)
        {
            if (IsAssertion(element).IsFalse())
                return null;

            var attributeName = SamlNames.IdAttributeName(SamlNames.IsSaml20(element.NamespaceURI));

            return element.HasAttribute(attributeName) ? element.GetAttribute(attributeName) : null;
        }
    }
}
