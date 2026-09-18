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
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SoapMemberAttribute : Attribute
    {
        /// <summary>
        ///     The wire name of the element. When <see langword="null" />, the property name is used.
        /// </summary>
        /// <value>
        ///     The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the XML namespace of this member. When <see langword="null" />, the resolved
        ///     namespace of the declaring type is inherited.
        /// </summary>
        /// <value>
        ///     The namespace.
        /// </value>
        public string Namespace { get; set; }

        /// <summary>
        ///     The emit order of this member, lower values first. The default of <c>-1</c> means
        ///     unspecified.
        /// </summary>
        /// <value>
        ///     The order.
        /// </value>
        public int Order { get; set; } = -1;

        /// <summary>
        ///     A slash separated chain of element local names (<c>ReceivedProduct/Detail</c>) that
        ///     locates this member when binding a response. It is not an XPath expression, and
        ///     <see langword="null" /> binds directly by <see cref="Name" />.
        /// </summary>
        /// <value>
        ///     The full pathname of the file.
        /// </value>
        public string Path { get; set; }

        /// <summary>
        ///     The element name emitted for each item of a collection member; <c>int</c> emits
        ///     <c>&lt;ids&gt;&lt;int&gt;1&lt;/int&gt;&lt;/ids&gt;</c>. When
        ///     <see langword="null" />, the contract name of the item type is used.
        /// </summary>
        /// <value>
        ///     The name of the item.
        /// </value>
        public string ItemName { get; set; }
    }
}