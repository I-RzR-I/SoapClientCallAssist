// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapTypeMap.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Readers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Dto.Map
{
    /// <summary>
    ///     The resolved mapping of a CLR type onto a SOAP element and its ordered members. Instances
    ///     are immutable once constructed and are safe to publish across threads.
    /// </summary>
    internal sealed class SoapTypeMap
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapTypeMap" /> class.
        /// </summary>
        /// <param name="elementName">
        ///     The fully resolved, namespace qualified element name of the type.
        /// </param>
        /// <param name="ns">The resolved XML namespace inherited by members that declare none.</param>
        /// <param name="members">The members in emit order.</param>
        /// <param name="declaresOwnNamespace">
        ///     True when the type declares its own namespace and therefore resolves the same way at
        ///     every call site.
        /// </param>
        internal SoapTypeMap(XName elementName, XNamespace ns,
            IList<SoapMemberMap> members, bool declaresOwnNamespace)
        {
            ElementName = elementName;
            Namespace = ns;
            Members = new ReadOnlyCollection<SoapMemberMap>(new List<SoapMemberMap>(members));
            DeclaresOwnNamespace = declaresOwnNamespace;
        }

        /// <summary>
        ///     Gets the fully resolved, namespace qualified element name of the type.
        /// </summary>
        /// <value>
        ///     The name of the element.
        /// </value>
        internal XName ElementName { get; }

        /// <summary>
        ///     Gets the resolved XML namespace inherited by members that declare none.
        /// </summary>
        /// <value>
        ///     The namespace.
        /// </value>
        internal XNamespace Namespace { get; }

        /// <summary>
        ///     Gets the members in emit order: members declared by a base type first, then the declared
        ///     order, then a stable tiebreak. <see cref="SoapMetadataReader" /> is the only place that
        ///     order is decided, so a writer and a reader of this map always walk it the same way and
        ///     neither re-sorts it.
        /// </summary>
        /// <value>
        ///     The members.
        /// </value>
        internal IReadOnlyList<SoapMemberMap> Members { get; }

        /// <summary>
        ///     Gets a value indicating whether the type declares its own namespace. When it does not,
        ///     the type inherits the call site namespace, so its cached map is only reusable for a
        ///     request that inherits the same one.
        /// </summary>
        /// <value>
        ///     True if declares own namespace, false if not.
        /// </value>
        internal bool DeclaresOwnNamespace { get; }
    }
}