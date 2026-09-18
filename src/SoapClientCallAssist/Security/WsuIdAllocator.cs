// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="WsuIdAllocator.cs" company="RzR SOFT & TECH">
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
using System.Xml;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     Mints the <c>wsu:Id</c> values of one request. One allocator serves one build.
    /// </summary>
    internal sealed class WsuIdAllocator
    {
        /// <summary>
        ///     Generates a fresh, collision-resistant id.
        /// </summary>
        /// <param name="prefix">A short, human-readable prefix naming the element kind.</param>
        /// <returns>
        ///     The generated id.
        /// </returns>
        internal string Next(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

        /// <summary>
        ///     Stamps a fresh <c>wsu:Id</c> on an element.
        /// </summary>
        /// <param name="document">The document the element belongs to.</param>
        /// <param name="element">The element to stamp.</param>
        /// <param name="prefix">A short, human-readable prefix naming the element kind.</param>
        /// <returns>
        ///     The id stamped.
        /// </returns>
        internal string Stamp(XmlDocument document, XmlElement element, string prefix)
        {
            var id = Next(prefix);

            SetWsuId(document, element, id);

            return id;
        }

        /// <summary>
        ///     Stamps a <c>wsu:Id</c> attribute carrying a given value on an element.
        /// </summary>
        /// <param name="document">The document the element belongs to.</param>
        /// <param name="element">The element to stamp.</param>
        /// <param name="id">The id value.</param>
        internal static void SetWsuId(XmlDocument document, XmlElement element, string id)
        {
            var attribute = document.CreateAttribute(WsSecurityNames.WsuPrefix, WsSecurityNames.IdLocalName, WsSecurityNames.WsuNamespace);
            attribute.Value = id;
            element.Attributes.Append(attribute);
        }
    }
}
