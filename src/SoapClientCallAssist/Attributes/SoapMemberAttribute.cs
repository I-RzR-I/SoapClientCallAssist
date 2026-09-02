// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapMemberAttribute.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;

#endregion

namespace SoapClientCallAssist.Attributes
{
    /// <summary>
    ///     Maps a property to a SOAP element. The wire name is frequently different from the CLR
    ///     property name, so <see cref="Name" /> is the authoritative element name.
    /// </summary>
    /// <seealso cref="Attribute">
    ///     =================================================================================================
    /// </seealso>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SoapMemberAttribute : Attribute
    {
        /// <summary>
        ///     Gets or sets the wire name of the element, which is often different from the CLR property
        ///     name (for example <c>product</c> or <c>associatedLocationIds</c>). When <see langword="null" />
        ///     , the property name is used.
        /// </summary>
        /// <value>
        ///     The element local name on the wire, or <see langword="null" /> to use the property name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the XML namespace of this member. When <see langword="null" />, the resolved
        ///     namespace of the declaring type is inherited.
        /// </summary>
        /// <value>
        ///     The XML namespace, or <see langword="null" /> to inherit from the declaring type.
        /// </value>
        public string Namespace { get; set; }

        /// <summary>
        ///     Gets or sets the emit/serialization order of this member. Lower values are emitted first.
        ///     The default of <c>-1</c> means unspecified.
        /// </summary>
        /// <value>
        ///     The order, or <c>-1</c> when unspecified.
        /// </value>
        public int Order { get; set; } = -1;

        /// <summary>
        ///     Gets or sets a slash separated chain of element local names used to locate this member
        ///     when binding a response, for example <c>ReceivedProduct/Detail</c>. This is not an XPath
        ///     expression; each segment is compared as a local name. When <see langword="null" />, the
        ///     member is bound directly by <see cref="Name" />.
        /// </summary>
        /// <value>
        ///     The slash separated local name chain, or <see langword="null" /> to bind by name.
        /// </value>
        public string Path { get; set; }

        /// <summary>
        ///     Gets or sets the element name emitted for each item of a collection member, for example
        ///     <c>int</c> so that a list emits <c>&lt;ids&gt;&lt;int&gt;1&lt;/int&gt;&lt;/ids&gt;</c>.
        ///     When <see langword="null" />, the contract name of the item type is used.
        /// </summary>
        /// <value>
        ///     The per item element name, or <see langword="null" /> to use the item contract name.
        /// </value>
        public string ItemName { get; set; }
    }
}