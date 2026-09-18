// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-10 22:10
//  ***********************************************************************
//  <copyright file="SoapResponseReader.cs" company="RzR SOFT & TECH">
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
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Exceptions;
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helpers;
using SoapClientCallAssist.Helpers.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Failures = SoapClientCallAssist.Helpers.Map.SoapMappingFailure;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;

#endregion

namespace SoapClientCallAssist.Readers
{
    /// <summary>
    ///     Binds a SOAP response envelope onto a decorated CLR model.
    /// </summary>
    internal static class SoapResponseReader
    {
        /// <summary>
        ///     (Immutable)
        ///     The marker returned in place of a value when the response carries no element for a member. 
        /// </summary>
        private static readonly object Unbound = new();

        /// <summary>
        ///     Reads a SOAP response envelope into a new instance of <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">
        ///     The decorated model; needs a public parameterless constructor.
        /// </typeparam>
        /// <param name="soapResponse">The raw response envelope.</param>
        /// <param name="protocolNamespace">
        ///     The namespace hint, or null; either protocol is accepted.
        /// </param>
        /// <param name="bodyTagOverride">The caller's Body tag, prefix optional, or null.</param>
        /// <returns>
        ///     An IResult&lt;T&gt; carrying the bound instance.
        /// </returns>
        internal static IResult<T> Read<T>(string soapResponse, XNamespace protocolNamespace, string bodyTagOverride)
        {
            var read = Read(typeof(T), soapResponse, protocolNamespace, bodyTagOverride);

            return read.IsSuccess.IsFalse()
                ? read.Propagate<T>()
                : Result<T>.Success((T)read.Response);
        }

        /// <summary>
        ///     Reads a SOAP response envelope into a new instance of the supplied type. A Fault counts
        ///     only as a direct child of the single Body under the envelope.
        /// </summary>
        /// <param name="targetType">The decorated model to bind.</param>
        /// <param name="soapResponse">The raw response envelope.</param>
        /// <param name="protocolNamespace">The namespace hint, or null.</param>
        /// <param name="bodyTagOverride">The caller supplied Body tag, or null.</param>
        /// <returns>
        ///     The bound instance, or a failed result for an unreadable document, a missing or empty
        ///     Body, a fault, or an operation element not matching the type.
        /// </returns>
        internal static IResult<object> Read(Type targetType, string soapResponse,
            XNamespace protocolNamespace, string bodyTagOverride)
        {
            if (targetType.IsNull())
                return ResponseError<object>("(null)", "no target type was supplied", null);

            try
            {
                if (soapResponse.IsMissing())
                    return ResponseError<object>(targetType.Name, "the response is empty", null);

                if (soapResponse.Length > SoapContracts.MaxDocumentCharacters)
                    return ResponseError<object>(targetType.Name, DocumentSizeReason(), null);

                var parsed = Parse(soapResponse, targetType.Name);
                if (parsed.IsSuccess.IsFalse())
                    return parsed.Propagate<object>();

                var body = LocateBody(parsed.Response, bodyTagOverride);
                if (body.IsNull())
                    return Failures.Validation<object>(MessageCodes.V_MAP_009);

                if (body.HasChildInOwnNamespace(SoapContracts.FaultLocalName))
                    return FaultFailure(targetType.Name);

                var anchor = body.Elements().FirstOrDefault();
                if (anchor.IsNull())
                    return Failures.Validation<object>(MessageCodes.V_MAP_010, targetType.Name);

                var map = SoapTypeMapCache.GetMap(targetType, InheritedNamespace(anchor, protocolNamespace), 0);
                if (map.IsSuccess.IsFalse())
                    return map.Propagate<object>();

                var expectedName = map.Response.ElementName.LocalName;
                if (string.Equals(anchor!.Name.LocalName, expectedName, StringComparison.OrdinalIgnoreCase).IsFalse())
                {
                    return Failures.Validation<object>(
                        MessageCodes.V_MAP_012, anchor.Name.LocalName, targetType.Name, expectedName);
                }

                return Bind(map.Response, targetType, anchor, 0);
            }
            catch (Exception ex)
            {
                return ResponseError<object>(targetType.Name, "an unexpected reader error", ex);
            }
        }

        /// <summary>
        ///     Parses the response through the shared hardened reader, reporting a depth stop and
        ///     malformed XML as a read failure.
        /// </summary>
        /// <param name="soapResponse">The raw response envelope.</param>
        /// <param name="targetTypeName">Name of the target type, used only for diagnostics.</param>
        /// <returns>
        ///     An IResult&lt;XElement&gt; carrying the document element.
        /// </returns>
        private static IResult<XElement> Parse(string soapResponse, string targetTypeName)
        {
            try
            {
                using (var reader = SoapXmlDocumentLoader.CreateReader(soapResponse, true))
                    return Result<XElement>.Success(XElement.Load(reader));
            }
            catch (XmlDepthExceededException ex)
            {
                return ResponseError<XElement>(targetTypeName, DepthReason(), ex);
            }
            catch (XmlException ex)
            {
                return ResponseError<XElement>(targetTypeName, "malformed XML, a DTD or a reader limit", ex);
            }
        }

        /// <summary>
        ///     Locates the only Body child of a document element that is an Envelope in either protocol
        ///     namespace; any prefix is accepted.
        /// </summary>
        /// <param name="documentElement">The document element.</param>
        /// <param name="bodyTagOverride">The caller supplied Body tag, or null.</param>
        /// <returns>
        ///     The Body element, or null for a non-envelope document, a tag not naming the Body, or no
        ///     single Body.
        /// </returns>
        private static XElement LocateBody(XElement documentElement, string bodyTagOverride)
        {
            if (SoapXmlHelper.AcceptsBodyTag(bodyTagOverride).IsFalse() || documentElement.IsEnvelope().IsFalse())
                return null;

            return documentElement.SingleChildInOwnNamespace(SoapContracts.BodyLocalName);
        }

        /// <summary>
        ///     Resolves the namespace a target type inherits when it declares none of its own.
        /// </summary>
        /// <param name="anchor">The operation response element.</param>
        /// <param name="protocolNamespace">The expected envelope namespace, or null.</param>
        /// <returns>
        ///     The inherited namespace, or null.
        /// </returns>
        private static string InheritedNamespace(XElement anchor, XNamespace protocolNamespace)
            => anchor.Name.NamespaceName.IsPresent()
                ? anchor.Name.NamespaceName
                : protocolNamespace.IsNull()
                    ? null
                    : protocolNamespace.NamespaceName;

        /// <summary>
        ///     Binds an element onto a new instance of the supplied type.
        /// </summary>
        /// <param name="clrType">The type to bind.</param>
        /// <param name="element">The element the members are located from.</param>
        /// <param name="inheritedNamespace">
        ///     The namespace inherited by a type that declares none.
        /// </param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <returns>
        ///     An IResult&lt;object&gt; carrying the bound instance.
        /// </returns>
        private static IResult<object> BindElement(Type clrType, XElement element, 
            string inheritedNamespace, int depth)
        {
            var map = SoapTypeMapCache.GetMap(clrType, inheritedNamespace, depth);

            return map.IsSuccess.IsFalse()
                ? map.Propagate<object>()
                : Bind(map.Response, clrType, element, depth);
        }

        /// <summary>
        ///     Binds an element onto a new instance of the supplied type through its resolved map.
        /// </summary>
        /// <param name="map">The resolved map of the type.</param>
        /// <param name="clrType">The type to bind.</param>
        /// <param name="element">The element the members are located from.</param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <returns>
        ///     An IResult&lt;object&gt; carrying the bound instance.
        /// </returns>
        private static IResult<object> Bind(SoapTypeMap map, Type clrType, XElement element, int depth)
        {
            var instance = clrType.CreateInstance();
            if (instance.IsNull())
                return Failures.Validation<object>(MessageCodes.V_MAP_008, clrType.Name);

            foreach (var member in map.Members)
            {
                var bound = BindMember(clrType, member, element, depth);
                if (bound.IsSuccess.IsFalse())
                    return bound;

                if (ReferenceEquals(bound.Response, Unbound))
                    continue;

                try
                {
                    member.Property.SetValue(instance, bound.Response, null);
                }
                catch (Exception ex)
                {
                    return BindError(
                        member.WireName.LocalName, clrType.Name, member.Property.Name, ex);
                }
            }

            return Result<object>.Success(instance);
        }

        /// <summary>
        ///     Resolves the value of a single member from the response.
        /// </summary>
        /// <param name="declaringType">The type declaring the member.</param>
        /// <param name="member">The member map.</param>
        /// <param name="anchor">The element the member path is walked from.</param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <returns>
        ///     An IResult&lt;object&gt; carrying the value, or the unbound marker when no element
        ///     matched.
        /// </returns>
        private static IResult<object> BindMember(Type declaringType, SoapMemberMap member, 
            XElement anchor, int depth)
        {
            var container = anchor;

            // The path is a chain of local names, not an XPath expression, and is namespace agnostic.
            foreach (var segment in member.PathSegments)
            {
                container = container.FirstByLocalName(segment);
                if (container.IsNull())
                    return Absent();
            }

            var localName = member.WireName.LocalName;

            if (member.Kind == SoapValueKindType.Collection)
                return BindCollection(declaringType, member, container, localName, depth);

            var element = container.FirstByLocalName(localName);
            if (element.IsNull())
                return Absent();

            if (element.IsNil())
            {
                return member.MemberType.AcceptsNull()
                    ? Result<object>.Success()
                    : Failures.Validation<object>(MessageCodes.V_MAP_005, localName, member.Property.Name);
            }

            if (member.Kind == SoapValueKindType.Complex)
                return BindElement(member.MemberType, element, member.WireName.NamespaceName, depth + 1);

            return ReadValue(member.MemberType, element.Value, declaringType, member, localName);
        }

        /// <summary>
        ///     Resolves the value of a collection member, in either of the two shapes a service sends.
        /// </summary>
        /// <param name="declaringType">The type declaring the member.</param>
        /// <param name="member">The member map.</param>
        /// <param name="container">The element the leaf name is looked up in.</param>
        /// <param name="localName">The expected leaf local name.</param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <returns>
        ///     An IResult&lt;object&gt; carrying the collection, or the unbound marker.
        /// </returns>
        private static IResult<object> BindCollection(Type declaringType, SoapMemberMap member, XElement container,
            string localName, int depth)
        {
            var matched = container.ByLocalName(localName).ToList();
            if (matched.Count == 0)
                return Absent();

            var wrappers = matched.Where(x => x.IsNil().IsFalse()).ToList();
            if (wrappers.Count == 0)
                return Result<object>.Success();

            var itemType = member.CollectionItemType;
            var itemIsSimple = itemType.IsSimple();
            var expectedItemName = member.ItemName.IsPresent() ? member.ItemName : localName;
            var items = new List<object>();

            var sources = IsUnwrapped(member, matched, itemIsSimple, depth)
                ? matched
                : wrappers.SelectMany(x => x.ItemElements(member.ItemName));

            foreach (var item in sources)
            {
                if (item.IsNil())
                {
                    if (itemType.AcceptsNull().IsFalse())
                    {
                        return Failures.Validation<object>(
                            MessageCodes.V_MAP_005, expectedItemName, member.Property.Name);
                    }

                    items.Add(null);

                    continue;
                }

                if (itemIsSimple)
                {
                    var value = ReadValue(itemType, item.Value, declaringType, member, expectedItemName);
                    if (value.IsSuccess.IsFalse())
                        return value;

                    items.Add(value.Response);

                    continue;
                }

                var bound = BindElement(itemType, item, member.WireName.NamespaceName, depth + 1);
                if (bound.IsSuccess.IsFalse())
                    return bound;

                items.Add(bound.Response);
            }

            if (items.Count == 0 && wrappers.Any(CarriesContent))
                return ShapeMismatch(declaringType, member, localName);

            return Materialize(declaringType, member, items, localName);
        }

        /// <summary>
        ///     Determines whether the elements matching the leaf name are the items themselves rather
        ///     than wrappers around them.
        /// </summary>
        /// <param name="member">The member map.</param>
        /// <param name="matched">Every element matching the leaf name.</param>
        /// <param name="itemIsSimple">True when the item type is read from element text.</param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <returns>
        ///     True when the matched elements are the items.
        /// </returns>
        private static bool IsUnwrapped(SoapMemberMap member, List<XElement> matched, 
            bool itemIsSimple, int depth)
        {
            if (matched.All(x => x.Elements().Any().IsFalse()))
                return itemIsSimple && (matched.Count > 1 || matched.Any(x => x.Value.IsPresent()));

            if (itemIsSimple || matched.Any(x => x.Elements().Any().IsFalse()))
                return false;

            if (member.ItemName.IsPresent() && matched.All(x => x.ByLocalName(member.ItemName).Any()))
                return false;

            return CarriesItemMembers(member, matched, depth);
        }

        /// <summary>
        ///     Determines whether every matched element holds a child named after a mapped member of the
        ///     item type.
        /// </summary>
        /// <param name="member">The member map.</param>
        /// <param name="matched">Every element matching the leaf name.</param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <returns>
        ///     True when the matched elements carry the members of the item type.
        /// </returns>
        private static bool CarriesItemMembers(SoapMemberMap member, List<XElement> matched, int depth)
        {
            var map = SoapTypeMapCache.GetMap(
                member.CollectionItemType, member.WireName.NamespaceName, depth + 1);

            if (map.IsSuccess.IsFalse())
                return matched.Count > 1;

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in map.Response.Members)
                names.Add(item.WireName.LocalName);

            return matched.All(x => x.Elements().Any(child => names.Contains(child.Name.LocalName)));
        }

        /// <summary>
        ///     Determines whether an element carries anything a reader could have bound, being either an
        ///     element child or text.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <returns>
        ///     True when the element carries content.
        /// </returns>
        private static bool CarriesContent(XElement element)
            => element.Elements().Any() || element.Value.IsPresent();

        /// <summary>
        ///     Builds the declared collection type from the bound items.
        /// </summary>
        /// <param name="declaringType">The type declaring the member.</param>
        /// <param name="member">The member map.</param>
        /// <param name="items">The bound items.</param>
        /// <param name="localName">The expected leaf local name, used only for diagnostics.</param>
        /// <returns>
        ///     An IResult&lt;object&gt; carrying the collection.
        /// </returns>
        private static IResult<object> Materialize(Type declaringType, SoapMemberMap member, List<object> items, string localName)
        {
            var itemType = member.CollectionItemType;

            try
            {
                if (member.MemberType.IsArray)
                {
                    var array = Array.CreateInstance(itemType, items.Count);
                    for (var index = 0; index < items.Count; index++)
                        array.SetValue(items[index], index);

                    return Result<object>.Success(array);
                }

                var concrete = member.MemberType.IsInterface || member.MemberType.IsAbstract
                    ? typeof(List<>).MakeGenericType(itemType)
                    : member.MemberType;

                var contract = typeof(ICollection<>).MakeGenericType(itemType);
                if (member.MemberType.IsAssignableFrom(concrete).IsFalse()
                    || contract.IsAssignableFrom(concrete).IsFalse())
                {
                    return BindError(
                        localName, declaringType.Name, member.Property.Name,
                        new NotSupportedException(
                            $"Collection type '{member.MemberType.FullName}' cannot be built from items of type '{itemType.FullName}'."));
                }

                var collection = concrete.CreateInstance();
                if (collection.IsNull())
                    return Failures.Validation<object>(MessageCodes.V_MAP_008, concrete.Name);

                var add = contract.GetMethod("Add");
                foreach (var item in items)
                    add!.Invoke(collection, new[] { item });

                return Result<object>.Success(collection);
            }
            catch (Exception ex)
            {
                return BindError(localName, declaringType.Name, member.Property.Name, ex);
            }
        }

        /// <summary>
        ///     Reads a simple value from element text.
        /// </summary>
        /// <param name="declaredType">The declared CLR type of the value.</param>
        /// <param name="text">The element text.</param>
        /// <param name="declaringType">The type declaring the member.</param>
        /// <param name="member">The member map.</param>
        /// <param name="localName">The expected element local name, used only for diagnostics.</param>
        /// <returns>
        ///     An IResult&lt;object&gt; carrying the value.
        /// </returns>
        private static IResult<object> ReadValue(Type declaredType, string text, Type declaringType,
            SoapMemberMap member, string localName)
        {
            try
            {
                return Result<object>.Success(Convert(declaredType, text));
            }
            catch (Exception ex)
            {
                return BindError(localName, declaringType.Name, member.Property.Name, ex);
            }
        }

        /// <summary>
        ///     Converts element text to the declared CLR type, culture invariant.
        /// </summary>
        /// <exception cref="NotSupportedException">
        ///     Thrown when the declared type has no element-text conversion.
        /// </exception>
        /// <param name="declaredType">The declared CLR type of the value.</param>
        /// <param name="text">The element text.</param>
        /// <returns>
        ///     The converted value.
        /// </returns>
        private static object Convert(Type declaredType, string text)
        {
            var type = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

            if (type == typeof(string))
                return text;

            if (type == typeof(char))
                return XmlConvert.ToChar(text);

            var value = text.Trim();

            if (type == typeof(byte[]))
                return System.Convert.FromBase64String(value);

            if (type.IsEnum)
                return type.ConvertEnum(value);

            if (type == typeof(bool))
                return XmlConvert.ToBoolean(value);

            if (type == typeof(byte))
                return XmlConvert.ToByte(value);

            if (type == typeof(sbyte))
                return XmlConvert.ToSByte(value);

            if (type == typeof(short))
                return XmlConvert.ToInt16(value);

            if (type == typeof(ushort))
                return XmlConvert.ToUInt16(value);

            if (type == typeof(int))
                return XmlConvert.ToInt32(value);

            if (type == typeof(uint))
                return XmlConvert.ToUInt32(value);

            if (type == typeof(long))
                return XmlConvert.ToInt64(value);

            if (type == typeof(ulong))
                return XmlConvert.ToUInt64(value);

            if (type == typeof(float))
                return XmlConvert.ToSingle(value);

            if (type == typeof(double))
                return XmlConvert.ToDouble(value);

            if (type == typeof(decimal))
                return XmlConvert.ToDecimal(value);

            if (type == typeof(Guid))
                return XmlConvert.ToGuid(value);

            if (type == typeof(TimeSpan))
                return XmlConvert.ToTimeSpan(value);

            if (type == typeof(DateTime))
                return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind);

            if (type == typeof(DateTimeOffset))
                return XmlConvert.ToDateTimeOffset(value);

            throw new NotSupportedException($"Type '{type.FullName}' cannot be read from element text.");
        }

        /// <summary>
        ///     The marker result returned when the response carries no element for a member.
        /// </summary>
        /// <returns>
        ///     A successful IResult&lt;object&gt; carrying the unbound marker.
        /// </returns>
        private static IResult<object> Absent() => Result<object>.Success(Unbound);

        /// <summary>
        ///     Builds the failure raised while reading the response envelope; the reason names only a
        ///     condition this library controls.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="targetTypeName">Name of the target type.</param>
        /// <param name="reason">The condition that stopped the read.</param>
        /// <param name="exception">The captured exception, or null.</param>
        /// <returns>
        ///     A failed IResult&lt;T&gt;.
        /// </returns>
        private static IResult<T> ResponseError<T>(string targetTypeName, string reason, Exception exception)
            => Result<T>.Failure(
                    MessageCodes.ER_MAP_RSP.GetDescription(),
                    Messages.GetErrorMessage(MessageCodes.ER_MAP_RSP).TryFormatWith(targetTypeName, reason))
                .WithOptionalError(exception, $"reading the response into '{targetTypeName}'");

        /// <summary>
        ///     Builds the failure raised when the response carries a SOAP fault; the fault text is not
        ///     surfaced.
        /// </summary>
        /// <param name="targetTypeName">Name of the target type.</param>
        /// <returns>
        ///     A failed IResult&lt;object&gt;.
        /// </returns>
        private static IResult<object> FaultFailure(string targetTypeName)
            => Result<object>.Failure(
                MessageCodes.ER_MAP_FLT.GetDescription(),
                Messages.GetErrorMessage(MessageCodes.ER_MAP_FLT).TryFormatWith(targetTypeName));

        /// <summary>
        ///     Builds the failure raised while binding one member, naming only the CLR member, its
        ///     declaring type and the expected XML name.
        /// </summary>
        /// <param name="elementName">The expected element local name.</param>
        /// <param name="declaringTypeName">Name of the declaring type.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="exception">The captured exception.</param>
        /// <returns>
        ///     A failed IResult&lt;object&gt;.
        /// </returns>
        private static IResult<object> BindError(string elementName, string declaringTypeName,
            string memberName, Exception exception)
            => Result<object>.Failure(
                    MessageCodes.ER_MAP_BND.GetDescription(),
                    Messages.GetErrorMessage(MessageCodes.ER_MAP_BND).TryFormatWith(
                        elementName, declaringTypeName, memberName))
                .WithOptionalError(exception, $"binding '{declaringTypeName}.{memberName}' from the expected element '{elementName}'");

        /// <summary>
        ///     The failure raised when a collection element carries content that the chosen shape reads
        ///     no item from.
        /// </summary>
        /// <param name="declaringType">The type declaring the member.</param>
        /// <param name="member">The member map.</param>
        /// <param name="localName">The expected leaf local name.</param>
        /// <returns>
        ///     A failed IResult&lt;object&gt;.
        /// </returns>
        private static IResult<object> ShapeMismatch(Type declaringType, SoapMemberMap member, string localName)
            => Failures.Validation<object>(
                MessageCodes.V_MAP_011, localName, declaringType.Name, member.Property.Name);

        /// <summary>
        ///     Builds the reason text naming the document size limit.
        /// </summary>
        /// <returns>
        ///     The reason text.
        /// </returns>
        private static string DocumentSizeReason()
            => $"the document size limit of {SoapContracts.MaxDocumentCharacters} characters was exceeded";

        /// <summary>
        ///     Builds the reason text naming the nesting depth limit.
        /// </summary>
        /// <returns>
        ///     The reason text.
        /// </returns>
        private static string DepthReason()
            => $"the XML nesting depth limit of {SoapContracts.MaxXmlDepth} was exceeded";
    }
}