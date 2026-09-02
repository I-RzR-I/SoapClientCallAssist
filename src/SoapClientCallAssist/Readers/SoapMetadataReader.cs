// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapMetadataReader.cs" company="RzR SOFT & TECH">
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
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Attributes;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helper.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Readers
{
    /// <summary>
    ///     Reads the SOAP mapping metadata of a CLR type into an immutable <see cref="SoapTypeMap" />
    ///     . Nested complex members are recorded by type only, never resolved here, so building one
    ///     map can never recurse into a cyclic type graph.
    /// </summary>
    internal static class SoapMetadataReader
    {
        /// <summary>
        ///     Reads the mapping metadata of the supplied type. This method never throws; every failure
        ///     is returned as a failed result.
        /// </summary>
        /// <param name="clrType">The type to read.</param>
        /// <param name="inheritedNamespace">
        ///     The call site namespace inherited by a type that declares none of its own.
        /// </param>
        /// <returns>
        ///     An IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> Read(Type clrType, string inheritedNamespace)
        {
            if (clrType.IsNull())
                return SoapMetadataResult.ReadError("(null)", new ArgumentNullException(nameof(clrType)));

            try
            {
                var contract = clrType.GetCustomAttribute<SoapContractAttribute>(true);
                var dataContract = clrType.GetCustomAttribute<DataContractAttribute>(true);

                var declaredNamespace = new[]
                {
                    contract.IsNull() ? null : contract.Namespace,
                    dataContract.IsNull() ? null : dataContract.Namespace
                }.FirstPresent();

                var typeNamespace = new[]
                {
                    declaredNamespace, 
                    inheritedNamespace
                }.FirstPresent();

                if (typeNamespace.IsMissing())
                    return SoapMetadataResult.MissingNamespace(clrType.Name);

                var localName = new[]
                {
                    contract.IsNull() ? null : contract.Name,
                    dataContract.IsNull() ? null : dataContract.Name,
                    clrType.Name.StripGenericArity()
                }.FirstPresent();

                var candidates = GetCandidateProperties(clrType);

                var soapMembers = candidates
                    .Where(x => x.GetCustomAttribute<SoapMemberAttribute>(true).IsNotNull())
                    .ToList();
                var dataMembers = candidates
                    .Where(x => x.GetCustomAttribute<DataMemberAttribute>(true).IsNotNull())
                    .ToList();

                if (soapMembers.Count > 0 && dataMembers.Count > 0)
                    return SoapMetadataResult.MixedConventions(clrType.Name);

                var soapMemberMode = soapMembers.Count > 0;
                var selected = soapMemberMode
                    ? soapMembers
                    : dataMembers
                        .Where(x => x.GetCustomAttribute<IgnoreDataMemberAttribute>(true).IsNull())
                        .ToList();

                if (selected.Count == 0)
                    return SoapMetadataResult.NoMappedMembers(clrType.Name);

                var members = new List<SoapMemberMap>(selected.Count);
                foreach (var property in selected)
                {
                    var member = BuildMember(property, typeNamespace, soapMemberMode, out var failure);
                    if (member.IsNull())
                        return failure;

                    members.Add(member);
                }

                var byContract = members
                    .OrderBy(InheritanceDepth)
                    .ThenBy(x => x.Order);
                var ordered = (soapMemberMode
                        ? byContract.ThenBy(x => x.Property.MetadataToken)
                        : byContract.ThenBy(x => x.WireName.LocalName, StringComparer.Ordinal))
                    .ToList();

                var duplicate = FindDuplicate(ordered);
                if (duplicate.IsNotNull())
                    return SoapMetadataResult.DuplicateWireName(duplicate, clrType.Name);

                var ns = XNamespace.Get(typeNamespace);

                return SoapMetadataResult.Success(
                    new SoapTypeMap(
                        ns.GetName(localName), ns, ordered, declaredNamespace.IsPresent()));
            }
            catch (Exception ex)
            {
                return SoapMetadataResult.ReadError(clrType.Name, ex);
            }
        }

        /// <summary>
        ///     Builds the map of a single property.
        /// </summary>
        /// <param name="property">The property to map.</param>
        /// <param name="typeNamespace">The resolved namespace of the declaring type.</param>
        /// <param name="soapMemberMode">True when the declaring type is in [SoapMember] mode.</param>
        /// <param name="failure">[out] The failure to return when the result is null.</param>
        /// <returns>
        ///     The member map, or null when <paramref name="failure" /> is set.
        /// </returns>
        private static SoapMemberMap BuildMember(
            PropertyInfo property, string typeNamespace,
            bool soapMemberMode, out IResult<SoapTypeMap> failure)
        {
            failure = null;

            if (property.GetSetMethod(false).IsNull())
            {
                failure = SoapMetadataResult.NotSettableMember(property.PropertyType.Name, property.Name);

                return null;
            }

            var soapMember = soapMemberMode ? property.GetCustomAttribute<SoapMemberAttribute>(true) : null;
            var dataMember = soapMemberMode ? null : property.GetCustomAttribute<DataMemberAttribute>(true);

            var localName = new[] { soapMember.IsNull() ? null : soapMember!.Name, dataMember.IsNull() ? null : dataMember!.Name, property.Name }.FirstPresent();

            var memberNamespace = new[] { soapMember.IsNull() ? null : soapMember!.Namespace, typeNamespace }.FirstPresent();

            var order = soapMember.IsNotNull()
                ? soapMember!.Order
                : dataMember.IsNotNull()
                    ? dataMember!.Order
                    : -1;

            if (TryResolveKind(property.PropertyType, out var itemType, out var kind).IsFalse())
            {
                failure = SoapMetadataResult.UnsupportedMemberType(property.PropertyType.Name, property.Name);

                return null;
            }

            return new SoapMemberMap(
                property,
                XNamespace.Get(memberNamespace).GetName(localName),
                order,
                SplitPath(soapMember.IsNull() ? null : soapMember!.Path),
                soapMember.IsNull() ? null : soapMember!.ItemName,
                property.PropertyType,
                itemType,
                kind);
        }

        /// <summary>
        ///     Counts how far the type declaring a member sits from the root of its hierarchy, which is
        ///     what puts a base declared member ahead of a derived one.
        /// </summary>
        /// <param name="member">The member to measure.</param>
        /// <returns>
        ///     The inheritance depth of the declaring type.
        /// </returns>
        private static int InheritanceDepth(SoapMemberMap member)
        {
            var depth = 0;
            var declaring = member.Property.DeclaringType;
            while (declaring.IsNotNull() && declaring != typeof(object))
            {
                depth++;
                declaring = declaring!.BaseType;
            }

            return depth;
        }

        /// <summary>
        ///     Enumerates the properties eligible for mapping: public instance properties, declared or
        ///     inherited, that expose a public getter and take no index parameters. A missing public
        ///     setter does not disqualify a property here; a decorated property that cannot be written
        ///     is reported by <see cref="BuildMember" /> rather than dropped, because dropping it would
        ///     leave a declared member silently unbound.
        /// </summary>
        /// <param name="clrType">The type to inspect.</param>
        /// <returns>
        ///     The candidate properties.
        /// </returns>
        private static List<PropertyInfo> GetCandidateProperties(Type clrType)
            => clrType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.GetIndexParameters().Length == 0 && x.GetGetMethod(false).IsNotNull())
                .ToList();

        /// <summary>
        ///     Finds the first member whose namespace, local name and binding path collide with an
        ///     earlier member.
        /// </summary>
        /// <param name="members">The ordered members.</param>
        /// <returns>
        ///     The duplicated wire name, or null when every member is distinct.
        /// </returns>
        private static string FindDuplicate(IEnumerable<SoapMemberMap> members)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in members)
            {
                var key = $"{member.WireName.NamespaceName}|{member.WireName.LocalName}|{string.Join("/", member.PathSegments)}";
                if (seen.Add(key).IsFalse())
                    return member.WireName.ToString();
            }

            return null;
        }

        /// <summary>
        ///     Determines how a member type is represented in XML. A nested complex type is accepted on
        ///     the strength of its shape alone; its own map is resolved later, on demand.
        /// </summary>
        /// <param name="memberType">The declared member type.</param>
        /// <param name="itemType">[out] The collection item type, or null.</param>
        /// <param name="kind">[out] The resolved value kind.</param>
        /// <returns>
        ///     True when the type is supported.
        /// </returns>
        private static bool TryResolveKind(Type memberType, out Type itemType, out SoapValueKind kind)
        {
            itemType = null;
            kind = SoapValueKind.Simple;

            if (memberType.IsNull())
                return false;

            var underlying = Nullable.GetUnderlyingType(memberType);
            if (underlying.IsNotNull())
                return underlying.IsSimple();

            if (memberType.IsSimple())
                return true;

            if (memberType.IsUnsupportedShape())
                return false;

            var ambiguous = false;
            var item = GetEnumerableItemType(memberType, ref ambiguous);
            if (ambiguous)
                return false;

            if (item.IsNotNull())
            {
                if (TryResolveKind(item, out _, out var itemKind).IsFalse()
                    || itemKind == SoapValueKind.Collection)
                    return false;

                itemType = item;
                kind = SoapValueKind.Collection;

                return true;
            }

            if (typeof(IEnumerable).IsAssignableFrom(memberType))
                return false;

            if (memberType.IsInterface)
                return false;

            kind = SoapValueKind.Complex;

            return true;
        }

        /// <summary>
        ///     Resolves the item type of a generic sequence. A type reaching this point is already known
        ///     not to be a string or a byte array, both of which are simple values rather than sequences.
        /// 
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="ambiguous">
        ///     [in,out] Set to true when the type offers more than one usable item type.
        /// </param>
        /// <returns>
        ///     The item type, or null when the type is not a generic sequence.
        /// </returns>
        private static Type GetEnumerableItemType(Type type, ref bool ambiguous)
        {
            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                {
                    ambiguous = true;

                    return null;
                }

                return type.GetElementType();
            }

            var arguments = new List<Type>();
            if (type.IsInterface && type.IsGenericType
                                 && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                arguments.Add(type.GetGenericArguments()[0]);

            foreach (var contract in type.GetInterfaces())
            {
                if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    arguments.Add(contract.GetGenericArguments()[0]);
            }

            var distinct = arguments.Distinct().ToList();
            if (distinct.Count == 1)
                return distinct[0];

            if (distinct.Count > 1)
                ambiguous = true;

            return null;
        }

        /// <summary>
        ///     Splits a slash separated binding path into its local name segments.
        /// </summary>
        /// <param name="path">The configured path.</param>
        /// <returns>
        ///     The segments, or null when no path is configured.
        /// </returns>
        private static string[] SplitPath(string path)
            => path.IsMissing()
                ? null
                : path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToArray();
    }
}