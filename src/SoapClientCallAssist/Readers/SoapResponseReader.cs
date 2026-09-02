// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
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
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helper;
using SoapClientCallAssist.Helper.Map;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Failures = SoapClientCallAssist.Helper.Map.SoapMappingFailure;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;
using Messages = SoapClientCallAssist.Helper.DefaultResultMessageHelper;

#endregion

namespace SoapClientCallAssist.Readers
{
    /// <summary>
    ///     Binds a SOAP response envelope onto a decorated CLR model. The envelope arrives from a
    ///     remote endpoint, so it is parsed through a reader that refuses a DTD, resolves nothing
    ///     externally and is bounded in both document size and nesting depth, and the target type is
    ///     taken only from the caller's generic argument, never from anything the response says
    ///     about itself. Every member is total and never throws.
    /// </summary>
    internal static class SoapResponseReader
    {
        /// <summary>
        ///     (Immutable) the marker returned in place of a value when the response carries no element
        ///     for a member. It is distinct from <see langword="null" />, which is the value of an
        ///     element explicitly marked nil.
        /// </summary>
        private static readonly object Unbound = new();

        /// <summary>
        ///     (Immutable) the signed types an enum may be built on. A value of one of these is widened
        ///     through <see cref="long" /> before it is read as a bit pattern.
        /// </summary>
        private static readonly HashSet<Type> SignedUnderlyingTypes = new()
        {
            typeof(sbyte), typeof(short), typeof(int), typeof(long)
        };

        /// <summary>
        ///     Reads a SOAP response envelope into a new instance of <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">
        ///     The decorated model to bind. It must expose a public parameterless constructor.
        /// </typeparam>
        /// <param name="soapResponse">The raw response envelope.</param>
        /// <param name="protocolNamespace">
        ///     The expected envelope namespace, used to disambiguate the body. Both SOAP 1.1 and SOAP
        ///     1.2 are recognised regardless of what is passed, and <see langword="null" /> is accepted.
        /// </param>
        /// <param name="bodyTagOverride">
        ///     The body element name supplied by the caller, with or without a prefix, or
        ///     <see langword="null" /> to locate the body by SOAP namespace.
        /// </param>
        /// <returns>
        ///     An IResult&lt;T&gt;.
        /// </returns>
        internal static IResult<T> Read<T>(string soapResponse, XNamespace protocolNamespace, string bodyTagOverride)
        {
            var read = Read(typeof(T), soapResponse, protocolNamespace, bodyTagOverride);

            return read.IsSuccess.IsFalse()
                ? read.Propagate<T>()
                : Result<T>.Success((T)read.Response);
        }

        /// <summary>
        ///     Reads a SOAP response envelope into a new instance of the supplied type. This is the core
        ///     the generic overload delegates to.
        /// </summary>
        /// <param name="targetType">The decorated model to bind.</param>
        /// <param name="soapResponse">The raw response envelope.</param>
        /// <param name="protocolNamespace">The expected envelope namespace, or null.</param>
        /// <param name="bodyTagOverride">The caller supplied body element name, or null.</param>
        /// <returns>
        ///     An IResult&lt;object&gt; carrying the bound instance.
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

                var envelope = parsed.Response;

                if (CarriesFault(envelope))
                    return FaultFailure(targetType.Name);

                var body = LocateBody(envelope, protocolNamespace, bodyTagOverride);
                if (body.IsNull())
                    return Failures.Validation<object>(MessageCodes.V_MAP_009);

                var anchor = body.Elements().FirstOrDefault();
                if (anchor.IsNull())
                    return Failures.Validation<object>(MessageCodes.V_MAP_010, targetType.Name);

                return BindElement(targetType, anchor, InheritedNamespace(anchor, protocolNamespace), 0);
            }
            catch (Exception ex)
            {
                return ResponseError<object>(targetType.Name, "an unexpected reader error", ex);
            }
        }

        /// <summary>
        ///     Parses the response through a hardened reader: no DTD, no resolver, a bounded document
        ///     size and a bounded nesting depth. Entity expansion is closed by prohibiting the DTD,
        ///     which is what stops an entity being declared at all; the character cap is a second bound
        ///     rather than the protection itself.
        /// </summary>
        /// <param name="soapResponse">The raw response envelope.</param>
        /// <param name="targetTypeName">Name of the target type, used only for diagnostics.</param>
        /// <returns>
        ///     An IResult&lt;XElement&gt; carrying the document element.
        /// </returns>
        private static IResult<XElement> Parse(string soapResponse, string targetTypeName)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = SoapContracts.MaxDocumentCharacters,
                MaxCharactersInDocument = SoapContracts.MaxDocumentCharacters,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                CloseInput = true,
                ConformanceLevel = ConformanceLevel.Document
            };

            try
            {
                using (var text = new StringReader(soapResponse))
                using (var reader = XmlReader.Create(text, settings))
                    return BuildTree(reader, targetTypeName);
            }
            catch (XmlException ex)
            {
                return ResponseError<XElement>(targetTypeName, "malformed XML, a DTD or a reader limit", ex);
            }
        }

        /// <summary>
        ///     Builds the element tree from a hardened reader, counting nesting depth as it goes.
        /// </summary>
        /// <param name="reader">The hardened reader.</param>
        /// <param name="targetTypeName">Name of the target type, used only for diagnostics.</param>
        /// <returns>
        ///     An IResult&lt;XElement&gt; carrying the document element.
        /// </returns>
        private static IResult<XElement> BuildTree(XmlReader reader, string targetTypeName)
        {
            XElement root = null;
            var open = new Stack<XElement>();

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Depth >= SoapContracts.MaxXmlDepth)
                            return ResponseError<XElement>(targetTypeName, DepthReason(), null);

                        var isEmpty = reader.IsEmptyElement;
                        var element = new XElement(XName.Get(reader.LocalName, reader.NamespaceURI));
                        CopyAttributes(reader, element);

                        if (open.Count == 0)
                            root = element;
                        else
                            open.Peek().Add(element);

                        if (isEmpty.IsFalse())
                            open.Push(element);

                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        if (open.Count > 0)
                            open.Peek().Add(new XText(reader.Value));

                        break;

                    case XmlNodeType.EndElement:
                        if (open.Count > 0)
                            open.Pop();

                        break;
                }
            }

            return root.IsNull()
                ? ResponseError<XElement>(targetTypeName, "the response holds no document element", null)
                : Result<XElement>.Success(root);
        }

        /// <summary>
        ///     Copies the attributes of the current element, skipping namespace declarations, which are
        ///     markup rather than data and are rebuilt by the element itself.
        /// </summary>
        /// <param name="reader">The hardened reader, positioned on an element.</param>
        /// <param name="element">The element being built.</param>
        private static void CopyAttributes(XmlReader reader, XElement element)
        {
            if (reader.HasAttributes.IsFalse() || reader.MoveToFirstAttribute().IsFalse())
                return;

            do
            {
                if (string.Equals(reader.NamespaceURI, SoapContracts.XmlnsNamespace, StringComparison.Ordinal)
                    || string.Equals(reader.Name, "xmlns", StringComparison.Ordinal))
                    continue;

                element.SetAttributeValue(XName.Get(reader.LocalName, reader.NamespaceURI), reader.Value);
            } while (reader.MoveToNextAttribute());

            reader.MoveToElement();
        }

        /// <summary>
        ///     Determines whether the envelope carries a SOAP fault, in either protocol namespace.
        /// </summary>
        /// <param name="envelope">The document element.</param>
        /// <returns>
        ///     True when a fault is present.
        /// </returns>
        private static bool CarriesFault(XElement envelope)
            => envelope
                .DescendantsAndSelf()
                .Any(x => string.Equals(x.Name.LocalName, SoapContracts.FaultLocalName, StringComparison.Ordinal)
                          && x.Name.NamespaceName.IsProtocolNamespace());

        /// <summary>
        ///     Locates the single body element. The body is matched by namespace and local name rather
        ///     than by a literal prefix, so any prefix a service chooses is accepted.
        /// </summary>
        /// <param name="envelope">The document element.</param>
        /// <param name="protocolNamespace">The expected envelope namespace, or null.</param>
        /// <param name="bodyTagOverride">The caller supplied body element name, or null.</param>
        /// <returns>
        ///     The body element, or null when there is not exactly one.
        /// </returns>
        private static XElement LocateBody(XElement envelope, XNamespace protocolNamespace, string bodyTagOverride)
        {
            if (bodyTagOverride.IsPresent())
            {
                var overrideName = bodyTagOverride.LocalPartOf();

                var overridden = Single(envelope
                    .DescendantsAndSelf()
                    .Where(x => string.Equals(x.Name.LocalName, overrideName, StringComparison.Ordinal)));

                return overridden.IsNotNull() && overridden.IsBody() ? overridden : null;
            }

            var candidates = envelope.Elements().Where(x => x.IsBody()).ToList();
            if (candidates.Count == 0)
                candidates = envelope.DescendantsAndSelf().Where(x => x.IsBody()).ToList();

            if (protocolNamespace.IsNull())
                return Single(candidates);

            var preferred = candidates.Where(x => x.Name.Namespace == protocolNamespace).ToList();

            return preferred.Count == 1 ? preferred[0] : Single(candidates);
        }

        /// <summary>
        ///     Resolves the namespace a target type inherits when it declares none of its own. The
        ///     operation response element carries the service contract namespace, which is the closest
        ///     thing the response offers.
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
        private static IResult<object> BindElement(
            Type clrType, XElement element, string inheritedNamespace, int depth)
        {
            var map = SoapTypeMapCache.GetMap(clrType, inheritedNamespace, depth);
            if (map.IsSuccess.IsFalse())
                return map.Propagate<object>();

            var instance = clrType.CreateInstance();
            if (instance.IsNull())
                return Failures.Validation<object>(MessageCodes.V_MAP_008, clrType.Name);

            foreach (var member in map.Response.Members)
            {
                var bound = BindMember(clrType, member, element, depth);
                if (bound.IsSuccess.IsFalse())
                    return bound;

                // An absent element leaves the member at its CLR default rather than failing.
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
        private static IResult<object> BindMember(
            Type declaringType, SoapMemberMap member, XElement anchor, int depth)
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

            if (member.Kind == SoapValueKind.Collection)
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

            if (member.Kind == SoapValueKind.Complex)
                return BindElement(member.MemberType, element, member.WireName.NamespaceName, depth + 1);

            return ReadValue(member.MemberType, element.Value, declaringType, member, localName);
        }

        /// <summary>
        ///     Resolves the value of a collection member, in either of the two shapes a service sends.
        ///     In the wrapped shape an element matching the leaf name holds the items as its element
        ///     children. In the unwrapped shape, which is what <c>maxOccurs="unbounded"</c> and an ASMX
        ///     <c>[XmlElement]</c> array produce, the leaf element is repeated once per item and carries
        ///     the value itself.
        /// </summary>
        /// <param name="declaringType">The type declaring the member.</param>
        /// <param name="member">The member map.</param>
        /// <param name="container">The element the leaf name is looked up in.</param>
        /// <param name="localName">The expected leaf local name.</param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <returns>
        ///     An IResult&lt;object&gt; carrying the collection, or the unbound marker.
        /// </returns>
        private static IResult<object> BindCollection(
            Type declaringType, SoapMemberMap member, XElement container, string localName, int depth)
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

            var sources = IsUnwrapped(member, matched)
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

            return Materialize(declaringType, member, items, localName);
        }

        /// <summary>
        ///     Determines whether the elements matching the leaf name are the items themselves rather
        ///     than wrappers around them. A declared item name settles the question on its own, since it
        ///     only has meaning inside a wrapper. Otherwise the shape decides: an element carrying text
        ///     but no element child cannot be a wrapper of anything, so reading it as one would bind an
        ///     empty collection and drop every value the service sent.
        /// </summary>
        /// <param name="member">The member map.</param>
        /// <param name="matched">Every element matching the leaf name.</param>
        /// <returns>
        ///     True when the matched elements are the items.
        /// </returns>
        private static bool IsUnwrapped(SoapMemberMap member, List<XElement> matched)
            => member.ItemName.IsMissing()
               && matched.All(x => x.Elements().Any().IsFalse())
               && matched.Any(x => x.Value.IsPresent());

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
        private static IResult<object> Materialize(
            Type declaringType, SoapMemberMap member, List<object> items, string localName)
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
        private static IResult<object> ReadValue(
            Type declaredType, string text, Type declaringType, SoapMemberMap member, string localName)
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
        ///     Converts element text to the declared CLR type. Every conversion is culture invariant, so
        ///     a response is read the same way on every machine.
        /// </summary>
        /// <exception cref="NotSupportedException">
        ///     Thrown when the requested operation is not supported.
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
        ///     Returns the only element of a sequence, or null when the sequence does not hold exactly
        ///     one.
        /// </summary>
        /// <param name="elements">The sequence.</param>
        /// <returns>
        ///     The single element, or null.
        /// </returns>
        private static XElement Single(IEnumerable<XElement> elements)
        {
            var matched = elements.Take(2).ToList();

            return matched.Count == 1 ? matched[0] : null;
        }

        /// <summary>
        ///     The marker result returned when the response carries no element for a member.
        /// </summary>
        /// <returns>
        ///     A successful IResult&lt;object&gt; carrying the unbound marker.
        /// </returns>
        private static IResult<object> Absent() => Result<object>.Success(Unbound);

        /// <summary>
        ///     A failure raised while reading the response envelope. The reason names a condition this
        ///     library controls; no part of the remote response is ever folded into the text.
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
        ///     The failure raised when the response carries a SOAP fault. The fault text is remote
        ///     content and is deliberately not surfaced.
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
        ///     A failure raised while binding one member. Only the CLR member, its declaring type and
        ///     the expected XML name are named; the element value is remote content and is never echoed.
        /// 
        /// </summary>
        /// <param name="elementName">The expected element local name.</param>
        /// <param name="declaringTypeName">Name of the declaring type.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="exception">The captured exception.</param>
        /// <returns>
        ///     A failed IResult&lt;object&gt;.
        /// </returns>
        private static IResult<object> BindError(
            string elementName, string declaringTypeName,
            string memberName, Exception exception)
            => Result<object>.Failure(
                    MessageCodes.ER_MAP_BND.GetDescription(),
                    Messages.GetErrorMessage(MessageCodes.ER_MAP_BND).TryFormatWith(
                        elementName, declaringTypeName, memberName))
                .WithOptionalError(exception, $"binding '{declaringTypeName}.{memberName}' from the expected element '{elementName}'");

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