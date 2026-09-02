// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 16:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
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
    ///     A type extensions.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        ///     (Immutable) the non-primitive CLR types written as element text. The set is never mutated
        ///     after initialization, so concurrent lookups need no synchronization.
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
        ///     Creates an instance through the public parameterless constructor. No type is ever
        ///     resolved from the response, so this only ever runs on a type reached from the caller's
        ///     own graph.
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
        ///     Thrown when the format of an input is incorrect.
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
            else if (Enum.IsDefined(enumType, parsed).IsFalse()) throw new FormatException("The element text does not name a declared member of the enum.");

            return parsed;
        }

        /// <summary>
        ///     Determines whether a parsed [Flags] value sets only bits the enum declares. A numeric
        ///     literal off the wire is otherwise accepted whole, which would bind a value no combination
        ///     of declared members can produce.
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
                declared |= enumType.ToBits(value);

            return (enumType.ToBits(parsed) & ~declared) == 0UL;
        }

        /// <summary>
        ///     Reads the bit pattern of an enum value. A signed underlying type is widened through
        ///     <see cref="long" /> first, so a negative member is a bit pattern rather than an overflow.
        /// 
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
        ///     Determines whether a type can hold a null, which is what an element marked nil binds to.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>
        ///     True when the type accepts null.
        /// </returns>
        internal static bool AcceptsNull(this Type type)
            => type.IsValueType.IsFalse() || Nullable.GetUnderlyingType(type) != null;

        /// <summary>
        ///     Determines whether a type is represented as element text. A nullable type is classified
        ///     by the value it wraps, since a nullable member arrives boxed as its underlying value and
        ///     is read back into the same underlying type.
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

            // Both are primitives to the runtime but carry no XML representation, so they are refused
            // before the primitive test rather than after it.
            if (underlying == typeof(IntPtr) || underlying == typeof(UIntPtr))
                return false;

            return underlying.IsPrimitive || NonPrimitiveSimpleTypes.Contains(underlying);
        }
    }
}