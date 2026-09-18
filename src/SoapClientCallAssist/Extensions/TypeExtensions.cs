// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 16:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-04 22:44
//  ***********************************************************************
//  <copyright file="TypeExtensions.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

#endregion

namespace SoapClientCallAssist.Extensions
{
    /// <summary>
    ///     Type helpers that classify CLR types for mapping and convert enum text.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        ///     The non-primitive CLR types written as element text; never mutated after initialization.
        /// </summary>
        private static readonly HashSet<Type> NonPrimitiveSimpleTypes = new()
        {
            typeof(string),
            typeof(decimal),
            typeof(Guid),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(byte[])
        };

        /// <summary>
        ///     Determines whether a type can never be mapped, regardless of its members.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>
        ///     True when the type is unmappable.
        /// </returns>
        internal static bool IsUnsupportedShape(this Type type)
            => type == typeof(object)
               || type == typeof(IntPtr)
               || type == typeof(UIntPtr)
               || type.IsPointer
               || type.IsByRef
               || type.ContainsGenericParameters
               || typeof(Delegate).IsAssignableFrom(type);

        /// <summary>
        ///     Creates an instance through the public parameterless constructor.
        /// </summary>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>
        ///     The new instance, or null when the type offers no public parameterless constructor.
        /// </returns>
        internal static object CreateInstance(this Type type)
        {
            if (type.IsAbstract || type.IsInterface)
                return null;

            var constructor = type.GetConstructor(Type.EmptyTypes);

            return constructor.IsNull() ? null : constructor!.Invoke(null);
        }

        /// <summary>
        ///     Converts element text to an enum by member name.
        /// </summary>
        /// <exception cref="FormatException">
        ///     Thrown when the text names no declared member or sets a flag bit the enum does not declare.
        /// </exception>
        /// <param name="enumType">The enum type.</param>
        /// <param name="value">The trimmed element text.</param>
        /// <returns>
        ///     The converted value.
        /// </returns>
        internal static object ConvertEnum(this Type enumType, string value)
        {
            var isFlags = enumType.GetCustomAttribute<FlagsAttribute>(false).IsNotNull();

            var parsed = Enum.Parse(enumType, isFlags ? value.NormalizeFlagList() : value);

            if (isFlags)
            {
                if (IsWithinDeclaredFlags(enumType, parsed).IsFalse())
                    throw new FormatException("The element text sets a bit the enum does not declare.");
            }
            else if (Enum.IsDefined(enumType, parsed).IsFalse()) 
                throw new FormatException("The element text does not name a declared member of the enum.");

            return parsed;
        }

        /// <summary>
        ///     Determines whether a parsed [Flags] value sets only bits the enum declares.
        /// </summary>
        /// <param name="enumType">The enum type.</param>
        /// <param name="parsed">The parsed value.</param>
        /// <returns>
        ///     True when every set bit is declared.
        /// </returns>
        private static bool IsWithinDeclaredFlags(Type enumType, object parsed)
        {
            var declared = 0UL;
            foreach (var value in Enum.GetValues(enumType))
            {
                declared |= enumType.ToBits(value);
            }

            return (enumType.ToBits(parsed) & ~declared) == 0UL;
        }

        /// <summary>
        ///     Reads the bit pattern of an enum value, widening a signed underlying type through
        ///     <see cref="long" /> first.
        /// </summary>
        /// <param name="enumType">The enum type.</param>
        /// <param name="value">The enum value.</param>
        /// <returns>
        ///     The bit pattern.
        /// </returns>
        internal static ulong ToBits(this Type enumType, object value)
        {
            var array = new HashSet<Type>
            {
                typeof(sbyte), 
                typeof(short), 
                typeof(int), 
                typeof(long)
            };

            return array.Contains(Enum.GetUnderlyingType(enumType))
                ? unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture))
                : Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Determines whether a type can hold a null, the value an element marked nil binds to.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>
        ///     True when the type accepts null.
        /// </returns>
        internal static bool AcceptsNull(this Type type)
            => type.IsValueType.IsFalse() || Nullable.GetUnderlyingType(type).IsNotNull();

        /// <summary>
        ///     Determines whether a type is represented as element text, classifying a nullable type by
        ///     the value it wraps.
        /// </summary>
        /// <param name="type">The type to test, which may be null.</param>
        /// <returns>
        ///     True when the type is a simple value.
        /// </returns>
        internal static bool IsSimple(this Type type)
        {
            if (type.IsNull())
                return false;

            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying.IsEnum)
                return true;

            if (underlying == typeof(IntPtr) || underlying == typeof(UIntPtr))
                return false;

            return underlying.IsPrimitive || NonPrimitiveSimpleTypes.Contains(underlying);
        }
    }
}