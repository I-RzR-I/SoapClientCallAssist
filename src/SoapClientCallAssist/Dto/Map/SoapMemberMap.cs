// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-04 22:44
//  ***********************************************************************
//  <copyright file="SoapMemberMap.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Collections;
using SoapClientCallAssist.Enums;
using System;
using System.Reflection;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Dto.Map
{
    /// <summary>
    ///     The resolved mapping of a single property onto a SOAP element. Instances are immutable
    ///     once constructed and are safe to publish across threads.
    /// </summary>
    internal sealed class SoapMemberMap
    {
        /// <summary>
        ///     The shared empty path of a member that binds directly by name.
        /// </summary>
        private static readonly string[] EmptyPath = new string[0];

        /// <summary>
        ///     The response binding path segments.
        /// </summary>
        private readonly string[] _pathSegments;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoapMemberMap" /> class.
        /// </summary>
        /// <param name="property">The reflected property this map is built from.</param>
        /// <param name="wireName">The fully resolved, namespace qualified element name.</param>
        /// <param name="order">The emit order, or -1 when unspecified.</param>
        /// <param name="pathSegments">Response binding local names, or null to bind by name.</param>
        /// <param name="itemName">Per item element name, or null for the item contract name.</param>
        /// <param name="memberType">The declared CLR type of the property.</param>
        /// <param name="collectionItemType">The item type of a collection member, or null.</param>
        /// <param name="kind">The XML representation of the value.</param>
        internal SoapMemberMap(PropertyInfo property, XName wireName, int order, string[] pathSegments,
            string itemName, Type memberType, Type collectionItemType, SoapValueKindType kind)
        {
            Property = property;
            WireName = wireName;
            Order = order;
            _pathSegments = pathSegments.IsNullOrEmptyEnumerable()
                ? EmptyPath
                : (string[])pathSegments.Clone();
            ItemName = itemName;
            MemberType = memberType;
            CollectionItemType = collectionItemType;
            Kind = kind;
        }

        /// <summary>
        ///     Gets the reflected property this map is built from.
        /// </summary>
        /// <value>
        ///     The property.
        /// </value>
        internal PropertyInfo Property { get; }

        /// <summary>
        ///     Gets the fully resolved, namespace qualified element name.
        /// </summary>
        /// <value>
        ///     The name of the wire.
        /// </value>
        internal XName WireName { get; }

        /// <summary>
        ///     Gets the emit order, or -1 when unspecified.
        /// </summary>
        /// <value>
        ///     The order.
        /// </value>
        internal int Order { get; }

        /// <summary>
        ///     Gets the response binding local names. Empty when the member binds directly by
        ///     <see cref="WireName" />.
        /// </summary>
        /// <value>
        ///     The path segments.
        /// </value>
        internal string[] PathSegments => _pathSegments;

        /// <summary>
        ///     Gets the per item element name for a collection, or null to fall back to the item
        ///     contract name.
        /// </summary>
        /// <value>
        ///     The name of the item.
        /// </value>
        internal string ItemName { get; }

        /// <summary>
        ///     Gets the declared CLR type of the property.
        /// </summary>
        /// <value>
        ///     The type of the member.
        /// </value>
        internal Type MemberType { get; }

        /// <summary>
        ///     The CLR item type of a collection member, or null when the member is not a collection.
        /// </summary>
        /// <value>
        ///     The type of the collection item.
        /// </value>
        internal Type CollectionItemType { get; }

        /// <summary>
        ///     Gets the XML representation of the value.
        /// </summary>
        /// <value>
        ///     The kind.
        /// </value>
        internal SoapValueKindType Kind { get; }
    }
}