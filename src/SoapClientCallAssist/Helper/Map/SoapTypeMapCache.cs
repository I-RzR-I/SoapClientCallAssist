// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapTypeMapCache.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Readers;
using System;
using System.Collections.Concurrent;

#endregion

namespace SoapClientCallAssist.Helper.Map
{
    /// <summary>
    ///     Caches the resolved <see cref="SoapTypeMap" /> of each CLR type. Maps are keyed by
    ///     <see cref="Type" /> and published only once fully built, so readers never observe a
    ///     partial
    ///     map. A nested complex member is resolved through this cache on demand rather than while
    ///     its parent map is being built, which is what keeps a cyclic type graph from recursing
    ///     forever.
    /// </summary>
    internal static class SoapTypeMapCache
    {

        /// <summary>
        ///     (Immutable) the resolved maps, keyed by CLR type.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, SoapTypeMap> Maps = new();

        /// <summary>
        ///     Gets the map of the supplied type at a known depth in a type graph walk. A caller
        ///     descending into a nested complex member passes its own depth plus one.
        /// </summary>
        /// <param name="clrType">The type to resolve.</param>
        /// <param name="inheritedNamespace">
        ///     The call site namespace inherited by a type that declares none of its own.
        /// </param>
        /// <param name="depth">The current depth of the graph walk.</param>
        /// <returns>
        ///     An IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> GetMap(Type clrType, string inheritedNamespace, int depth)
        {
            if (clrType.IsNull())
                return SoapMetadataResult.ReadError("(null)", new ArgumentNullException(nameof(clrType)));

            try
            {
                if (depth < 0 || depth >= SoapContracts.MaxGraphDepth)
                    return SoapMetadataResult.DepthExceeded(clrType.Name, SoapContracts.MaxGraphDepth);

                if (Maps.TryGetValue(clrType, out var cached) && IsReusable(cached, inheritedNamespace))
                    return SoapMetadataResult.Success(cached);

                var built = SoapMetadataReader.Read(clrType, inheritedNamespace);
                if (built.IsSuccess.IsFalse())
                    return built;

                var map = built.Response;

                var published = Maps.GetOrAdd(clrType, map);

                return SoapMetadataResult.Success(
                    IsReusable(published, inheritedNamespace) ? published : map);
            }
            catch (Exception ex)
            {
                return SoapMetadataResult.ReadError(clrType.Name, ex);
            }
        }

        /// <summary>
        ///     Determines whether a cached map can serve a request that inherits the supplied namespace.
        /// </summary>
        /// <param name="map">The cached map.</param>
        /// <param name="inheritedNamespace">The call site namespace.</param>
        /// <returns>
        ///     True when the cached map is reusable.
        /// </returns>
        private static bool IsReusable(SoapTypeMap map, string inheritedNamespace)
            => map.DeclaresOwnNamespace
               || string.Equals(map.Namespace.NamespaceName, inheritedNamespace, StringComparison.Ordinal);
    }
}