// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="SoapMetadataResult.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Extensions;
using System;
using Failures = SoapClientCallAssist.Helper.Map.SoapMappingFailure;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;
using Messages = SoapClientCallAssist.Helper.DefaultResultMessageHelper;

#endregion

namespace SoapClientCallAssist.Helper.Map
{
    /// <summary>
    ///     Builds the <see cref="IResult{T}" /> values returned by the mapping metadata pipeline.
    ///     Every member is total and never throws, so a caller can rely on a result instead of an
    ///     exception.
    /// </summary>
    internal static class SoapMetadataResult
    {
        /// <summary>
        ///     A successfully resolved type map.
        /// </summary>
        /// <param name="map">The resolved map.</param>
        /// <returns>
        ///     A successful IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> Success(SoapTypeMap map)
            => Result<SoapTypeMap>.Success(map);

        /// <summary>
        ///     The type declares neither a [SoapMember] nor a [DataMember] member. Properties are never
        ///     mapped implicitly, so this is a failure rather than an empty map.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> NoMappedMembers(string typeName)
            => Validation(MessageCodes.V_MAP_001, typeName);

        /// <summary>
        ///     The declared CLR type of member cannot be represented in XML.
        /// </summary>
        /// <param name="memberTypeName">Name of the unsupported member type.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> UnsupportedMemberType(string memberTypeName, string memberName)
            => Validation(MessageCodes.V_MAP_002, memberTypeName, memberName);

        /// <summary>
        ///     A mapped member exposes no public setter, so the response reader has nowhere to put the
        ///     value it binds. The member is reported rather than dropped, because a silently dropped
        ///     member reads back as an all defaults value on a successful result.
        /// </summary>
        /// <param name="memberTypeName">Name of the member type.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> NotSettableMember(string memberTypeName, string memberName)
            => Validation(MessageCodes.V_MAP_002, memberTypeName, memberName);

        /// <summary>
        ///     Two members of the same type resolve to the same namespace, local name and binding path.
        /// </summary>
        /// <param name="wireName">The duplicated wire name.</param>
        /// <param name="typeName">Name of the declaring type.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> DuplicateWireName(string wireName, string typeName)
            => Validation(MessageCodes.V_MAP_003, wireName, typeName);

        /// <summary>
        ///     A type resolves to an element in no namespace: it declares no namespace of its own and
        ///     the call site offers none to inherit. An unqualified element triggers the destructive
        ///     body rebuild in the SOAP XML helper, so it is rejected here, at the one place a namespace
        ///     is resolved from. Every member name is then built on the namespace this check has already
        ///     accepted, which is why no second, per member check is needed.
        /// </summary>
        /// <param name="memberName">
        ///     Name of the type or request member that resolved to no namespace.
        /// </param>
        /// <returns>
        ///     A failed IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> MissingNamespace(string memberName)
            => Validation(MessageCodes.V_MAP_004, memberName);

        /// <summary>
        ///     The type decorates some members with [SoapMember] and others with [DataMember].
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> MixedConventions(string typeName)
            => Validation(MessageCodes.V_MAP_006, typeName);

        /// <summary>
        ///     A walk of the type graph passed the supported nesting depth, which is how a recursive
        ///     graph is stopped instead of overflowing the stack.
        /// </summary>
        /// <param name="typeName">Name of the type being resolved when the cap was reached.</param>
        /// <param name="maxDepth">The maximum supported depth.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> DepthExceeded(string typeName, int maxDepth)
            => Validation(MessageCodes.V_MAP_007, typeName, maxDepth);

        /// <summary>
        ///     An unexpected error, typically raised by reflection, while reading mapping metadata.
        /// </summary>
        /// <param name="typeName">Name of the type being read.</param>
        /// <param name="exception">The captured exception.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> ReadError(string typeName, Exception exception)
            => Result<SoapTypeMap>.Failure(
                    MessageCodes.ER_MAP_MTD.GetDescription(),
                    Messages.GetErrorMessage(MessageCodes.ER_MAP_MTD).TryFormatWith(typeName))
                .WithOptionalError(exception, $"reading the SOAP mapping metadata of '{typeName}'");

        /// <summary>
        ///     Builds a validation failure for the supplied code and message arguments.
        /// </summary>
        /// <param name="code">The message code.</param>
        /// <param name="args">The message arguments.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        private static IResult<SoapTypeMap> Validation(MessageCodes code, params object[] args)
            => Failures.Validation<SoapTypeMap>(code, args);
    }
}