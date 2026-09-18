// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 15:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-04 22:44
//  ***********************************************************************
//  <copyright file="SoapModelEmitter.cs" company="RzR SOFT & TECH">
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
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using EmitResult = RzR.ResultMessage.Abstractions.IResult<System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>>;
using Failures = SoapClientCallAssist.Helpers.Map.SoapMappingFailure;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;

#endregion

namespace SoapClientCallAssist.Helpers.Map
{
    /// <summary>
    ///     Emits the document/literal wrapped SOAP body of an operation call from decorated models.
    /// </summary>
    internal static class SoapModelEmitter
    {
        /// <summary>
        ///     The member name reported for a failure that belongs to the request itself.
        /// </summary>
        private const string RequestOwner = "(request)";

        /// <summary>
        ///     The schema type name emitted as the item element name for each simple CLR type.
        /// </summary>
        private static readonly Dictionary<Type, string> SimpleItemNames = new()
        {
            { typeof(string), "string" },
            { typeof(bool), "boolean" },
            { typeof(char), "char" },
            { typeof(sbyte), "byte" },
            { typeof(byte), "unsignedByte" },
            { typeof(short), "short" },
            { typeof(ushort), "unsignedShort" },
            { typeof(int), "int" },
            { typeof(uint), "unsignedInt" },
            { typeof(long), "long" },
            { typeof(ulong), "unsignedLong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(DateTime), "dateTime" },
            { typeof(DateTimeOffset), "dateTimeOffset" },
            { typeof(TimeSpan), "duration" },
            { typeof(Guid), "guid" },
            { typeof(byte[]), "base64Binary" }
        };

        /// <summary>
        ///     Emits the SOAP body elements of the supplied operation call. This method never throws;
        ///     every failure, including a reflection failure, is returned as a failed result.
        /// </summary>
        /// <param name="request">The operation call to emit.</param>
        /// <returns>
        ///     An IResult&lt;IEnumerable&lt;XElement&gt;&gt; carrying the single operation element.
        /// </returns>
        internal static EmitResult Emit(SoapOperationRequest request)
        {
            try
            {
                var validation = Validate(request);
                if (validation.IsNotNull())
                    return validation;

                var operationNamespace = request.OperationNamespace;
                var root = new XElement(operationNamespace.GetName(request.OperationName));

                var declared = new HashSet<string>(StringComparer.Ordinal);
                foreach (var parameter in request.Parameters)
                {
                    if (parameter.IsNull() || parameter.Name.IsMissing())
                        return EmitError("(parameter)", request.OperationName, null);

                    if (declared.Add(parameter.Name).IsFalse())
                        return Validation(MessageCodes.V_MAP_003, parameter.Name, request.OperationName);

                    if (TryEmitParameter(parameter, operationNamespace, out var child, out var failure).IsFalse())
                        return failure;

                    if (child.IsNotNull())
                        root.Add(child);
                }

                return Result<IEnumerable<XElement>>.Success(new[] { root });
            }
            catch (Exception ex)
            {
                return EmitError(RequestOwner, nameof(SoapOperationRequest), ex);
            }
        }

        /// <summary>
        ///     Validates the parts of the request the emitter cannot work without.
        /// </summary>
        /// <param name="request">The operation call to emit.</param>
        /// <returns>
        ///     The failure to return, or null when the request is usable.
        /// </returns>
        private static EmitResult Validate(SoapOperationRequest request)
        {
            if (request.IsNull())
                return EmitError(RequestOwner, nameof(SoapOperationRequest), new ArgumentNullException(nameof(request)));

            if (request.OperationName.IsMissing())
                return EmitError(nameof(SoapOperationRequest.OperationName), nameof(SoapOperationRequest), null);

            if (request.OperationNamespace.IsNull() || request.OperationNamespace.NamespaceName.IsMissing())
                return Validation(MessageCodes.V_MAP_004, nameof(SoapOperationRequest.OperationNamespace));

            return request.Parameters.IsNull()
                ? EmitError(nameof(SoapOperationRequest.Parameters), nameof(SoapOperationRequest), null)
                : null;
        }

        /// <summary>
        ///     Emits one argument of the operation as an element in the operation namespace. A null
        ///     argument is omitted.
        /// </summary>
        /// <param name="parameter">The argument to emit.</param>
        /// <param name="operationNamespace">The namespace of the operation element.</param>
        /// <param name="element">
        ///     [out] The emitted element, or null when the argument is omitted.
        /// </param>
        /// <param name="failure">[out] The failure to return when the result is false.</param>
        /// <returns>
        ///     True when the argument was emitted or deliberately omitted.
        /// </returns>
        private static bool TryEmitParameter(SoapOperationParameter parameter, XNamespace operationNamespace,
            out XElement element, out EmitResult failure)
        {
            element = null;
            failure = null;

            var value = parameter.Value;
            if (value.IsNull())
                return true;

            var valueType = value.GetType();

            try
            {
                var name = operationNamespace.GetName(parameter.Name);

                if (valueType.IsSimple())
                {
                    if (TryFormat(value, valueType, out var text).IsFalse())
                    {
                        failure = Validation(MessageCodes.V_MAP_002, valueType.Name, parameter.Name);

                        return false;
                    }

                    element = new XElement(name, text);

                    return true;
                }

                return value is IEnumerable sequence
                    ? TryEmitCollection(sequence, name, null, null, 0, out element, out failure)
                    : TryEmitComplex(value, valueType, name, 0, out element, out failure);
            }
            catch (Exception ex)
            {
                failure = EmitError(parameter.Name, valueType.Name, ex);

                return false;
            }
        }

        /// <summary>
        ///     Emits a complex value as an element carrying one child per mapped member.
        /// </summary>
        /// <param name="value">The value to emit.</param>
        /// <param name="declaredType">The declared CLR type the contract is read from.</param>
        /// <param name="elementName">The name of the emitted element.</param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <param name="element">[out] The emitted element.</param>
        /// <param name="failure">[out] The failure to return when the result is false.</param>
        /// <returns>
        ///     True when the value was emitted.
        /// </returns>
        private static bool TryEmitComplex(object value, Type declaredType, XName elementName, int depth,
            out XElement element, out EmitResult failure)
        {
            element = null;

            return TryResolveMap(declaredType, elementName.Namespace, depth, out var map, out failure)
                   && TryEmitMapped(value, map, elementName, depth, out element, out failure);
        }

        /// <summary>
        ///     Emits a value against an already resolved map.
        /// </summary>
        /// <param name="value">The value to emit.</param>
        /// <param name="map">The resolved map of the value.</param>
        /// <param name="elementName">The name of the emitted element.</param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <param name="element">[out] The emitted element.</param>
        /// <param name="failure">[out] The failure to return when the result is false.</param>
        /// <returns>
        ///     True when the value was emitted.
        /// </returns>
        private static bool TryEmitMapped(object value, SoapTypeMap map, XName elementName, int depth,
            out XElement element, out EmitResult failure)
        {
            element = null;
            failure = null;

            var container = new XElement(elementName);
            foreach (var member in map.Members)
            {
                if (TryEmitMember(value, member, depth + 1, out var child, out failure).IsFalse())
                    return false;

                if (child.IsNotNull())
                    container.Add(child);
            }

            element = container;

            return true;
        }

        /// <summary>
        ///     Emits one mapped member of a complex value. A null member is omitted, not written as a
        ///     nil element.
        /// </summary>
        /// <param name="owner">The instance the member is read from.</param>
        /// <param name="member">The member to emit.</param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <param name="element">[out] The emitted element, or null when the member is omitted.</param>
        /// <param name="failure">[out] The failure to return when the result is false.</param>
        /// <returns>
        ///     True when the member was emitted or deliberately omitted.
        /// </returns>
        private static bool TryEmitMember(object owner, SoapMemberMap member, int depth,
            out XElement element, out EmitResult failure)
        {
            element = null;
            failure = null;

            object value;
            try
            {
                value = member.Property.GetValue(owner);
            }
            catch (Exception ex)
            {
                failure = EmitError(member.WireName.LocalName, member.Property.DeclaringType.Name, ex);

                return false;
            }

            if (value.IsNull())
                return true;

            if (member.Kind == SoapValueKindType.Collection)
            {
                return TryEmitCollection(
                    (IEnumerable)value, member.WireName, member.ItemName,
                    member.CollectionItemType, depth, out element, out failure);
            }

            if (member.Kind == SoapValueKindType.Complex)
                return TryEmitComplex(value, member.MemberType, member.WireName, depth, out element, out failure);

            var valueType = value.GetType();
            if (TryFormat(value, valueType, out var text).IsFalse())
            {
                failure = Validation(MessageCodes.V_MAP_002, valueType.Name, member.WireName.LocalName);

                return false;
            }

            element = new XElement(member.WireName, text);

            return true;
        }

        /// <summary>
        ///     Emits a sequence as a wrapper element with one child per item, each qualified with the
        ///     wrapper's or its own contract namespace. Null items and an empty sequence are omitted.
        /// 
        /// </summary>
        /// <param name="values">The items to emit.</param>
        /// <param name="wrapperName">The name of the wrapper element.</param>
        /// <param name="itemName">The configured per item local name, or null to derive one.</param>
        /// <param name="declaredItemType">The declared item type, or null when it is unknown.</param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <param name="element">[out] The emitted wrapper, or null when nothing was emitted.</param>
        /// <param name="failure">[out] The failure to return when the result is false.</param>
        /// <returns>
        ///     True when the sequence was emitted or deliberately omitted.
        /// </returns>
        private static bool TryEmitCollection(IEnumerable values, XName wrapperName, string itemName, 
            Type declaredItemType, int depth,
            out XElement element, out EmitResult failure)
        {
            element = null;
            failure = null;

            var itemNamespace = wrapperName.Namespace;
            var configuredName = itemName.IsPresent() ? itemNamespace.GetName(itemName) : null;
            var children = new List<XElement>();

            foreach (var item in values)
            {
                if (item.IsNull())
                    continue;

                var itemType = item.GetType();
                if (itemType.IsSimple())
                {
                    if (TryFormat(item, itemType, out var text).IsFalse())
                    {
                        failure = Validation(MessageCodes.V_MAP_002, itemType.Name, wrapperName.LocalName);

                        return false;
                    }

                    children.Add(new XElement(
                        configuredName ?? itemNamespace.GetName(GetSimpleItemName(itemType)), text));

                    continue;
                }

                var contractType = declaredItemType ?? itemType;
                if (TryResolveMap(contractType, itemNamespace, depth + 1, out var map, out failure).IsFalse())
                    return false;

                if (TryEmitMapped(item, map, configuredName ?? map.ElementName, depth + 1, out var child, out failure).IsFalse())
                    return false;

                children.Add(child);
            }

            if (children.Count == 0)
                return true;

            element = new XElement(wrapperName, children);

            return true;
        }

        /// <summary>
        ///     Resolves the map of a type through the shared cache and converts a read failure into an
        ///     emit failure.
        /// </summary>
        /// <param name="clrType">The type to resolve.</param>
        /// <param name="inheritedNamespace">
        ///     The namespace inherited by a type that declares none.
        /// </param>
        /// <param name="depth">The current depth of the type graph walk.</param>
        /// <param name="map">[out] The resolved map.</param>
        /// <param name="failure">[out] The failure to return when the result is false.</param>
        /// <returns>
        ///     True when the map was resolved.
        /// </returns>
        private static bool TryResolveMap(Type clrType, XNamespace inheritedNamespace, int depth,
            out SoapTypeMap map, out EmitResult failure)
        {
            map = null;

            var resolved = SoapTypeMapCache.GetMap(clrType, inheritedNamespace.NamespaceName, depth);
            if (resolved.IsSuccess.IsFalse())
            {
                failure = resolved.Propagate<IEnumerable<XElement>>();

                return false;
            }

            map = resolved.Response;
            failure = null;

            return true;
        }

        /// <summary>
        ///     Gets the element name emitted per item of a collection of simple values.
        /// </summary>
        /// <param name="type">The item type.</param>
        /// <returns>
        ///     The per item local name.
        /// </returns>
        private static string GetSimpleItemName(Type type)
            => SimpleItemNames.TryGetValue(type, out var name) ? name : type.Name;

        /// <summary>
        ///     Formats a simple value as element text with culture-invariant conversions.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="type">The runtime type of the value.</param>
        /// <param name="text">[out] The formatted text.</param>
        /// <returns>
        ///     True when the value could be formatted.
        /// </returns>
        private static bool TryFormat(object value, Type type, out string text)
        {
            if (type.IsEnum)
                return TryFormatEnum(value, type, out text);

            text = null;
            switch (value)
            {
                case string asString:
                    text = asString;
                    break;
                case byte[] asBytes:
                    text = asBytes.ToBase64String();
                    break;
                case bool asBool:
                    text = XmlConvert.ToString(asBool);
                    break;
                case char asChar:
                    text = XmlConvert.ToString(asChar);
                    break;
                case sbyte asSByte:
                    text = XmlConvert.ToString(asSByte);
                    break;
                case byte asByte:
                    text = XmlConvert.ToString(asByte);
                    break;
                case short asInt16:
                    text = XmlConvert.ToString(asInt16);
                    break;
                case ushort asUInt16:
                    text = XmlConvert.ToString(asUInt16);
                    break;
                case int asInt32:
                    text = XmlConvert.ToString(asInt32);
                    break;
                case uint asUInt32:
                    text = XmlConvert.ToString(asUInt32);
                    break;
                case long asInt64:
                    text = XmlConvert.ToString(asInt64);
                    break;
                case ulong asUInt64:
                    text = XmlConvert.ToString(asUInt64);
                    break;
                case float asSingle:
                    text = XmlConvert.ToString(asSingle);
                    break;
                case double asDouble:
                    text = XmlConvert.ToString(asDouble);
                    break;
                case decimal asDecimal:
                    text = XmlConvert.ToString(asDecimal);
                    break;
                case Guid asGuid:
                    text = XmlConvert.ToString(asGuid);
                    break;
                case TimeSpan asTimeSpan:
                    text = XmlConvert.ToString(asTimeSpan);
                    break;
                case DateTime asDateTime:
                    text = XmlConvert.ToString(asDateTime, XmlDateTimeSerializationMode.RoundtripKind);
                    break;
                case DateTimeOffset asDateTimeOffset:
                    text = XmlConvert.ToString(asDateTimeOffset);
                    break;
            }

            return text.IsNotNull();
        }

        /// <summary>
        ///     Formats an enum by member name, writing a flags value as a space separated list of names.
        ///     A value that resolves to no name is refused.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="type">The enum type.</param>
        /// <param name="text">[out] The formatted text.</param>
        /// <returns>
        ///     True when the value could be formatted.
        /// </returns>
        private static bool TryFormatEnum(object value, Type type, out string text)
        {
            text = Enum.GetName(type, value);
            if (text.IsNotNull())
                return true;

            if (type.IsDefined(typeof(FlagsAttribute), false).IsFalse())
                return false;

            var composed = value.ToString();
            if (composed.IndexOf(',') < 0)
                return false;

            text = composed.Replace(", ", " ").Replace(",", " ");

            return true;
        }

        /// <summary>
        ///     Builds an emit failure naming the member and its declaring type, never the member's value.
        /// </summary>
        /// <param name="memberName">Name of the member being emitted.</param>
        /// <param name="typeName">Name of the type declaring the member.</param>
        /// <param name="exception">The captured exception, or null when there is none.</param>
        /// <returns>
        ///     The failure for a member that could not be emitted.
        /// </returns>
        private static EmitResult EmitError(string memberName, string typeName, Exception exception)
            => Result<IEnumerable<XElement>>.Failure(
                    MessageCodes.ER_MAP_EMT.GetDescription(),
                    Messages.GetErrorMessage(MessageCodes.ER_MAP_EMT).TryFormatWith(memberName, typeName))
                .WithOptionalError(exception, $"emitting '{typeName}.{memberName}'");

        /// <summary>
        ///     Builds a validation failure for the supplied code and message arguments.
        /// </summary>
        /// <param name="code">The message code.</param>
        /// <param name="args">The message arguments.</param>
        /// <returns>
        ///     A failed IResult&lt;IEnumerable&lt;XElement&gt;&gt;.
        /// </returns>
        private static EmitResult Validation(MessageCodes code, params object[] args)
            => Failures.Validation<IEnumerable<XElement>>(code, args);
    }
}