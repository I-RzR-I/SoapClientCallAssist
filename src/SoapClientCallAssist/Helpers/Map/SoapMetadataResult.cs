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
using Failures = SoapClientCallAssist.Helpers.Map.SoapMappingFailure;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;

#endregion

namespace SoapClientCallAssist.Helpers.Map
{
    /// <summary>
    ///     Builds the <see cref="IResult{T}" /> values the mapping metadata pipeline returns. No member
    ///     throws.
    /// </summary>
    internal static class SoapMetadataResult
    {
        /// <summary>
        ///     Builds the success result carrying a resolved type map.
        /// </summary>
        /// <param name="map">The resolved map.</param>
        /// <returns>
        ///     A successful IResult&lt;SoapTypeMap&gt;.
        /// </returns>
        internal static IResult<SoapTypeMap> Success(SoapTypeMap map)
            => Result<SoapTypeMap>.Success(map);

        /// <summary>
        ///     Builds the failure for a type that declares neither a [SoapMember] nor a [DataMember]
        ///     member.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns>
        ///     The failure for a type with no mapped members.
        /// </returns>
        internal static IResult<SoapTypeMap> NoMappedMembers(string typeName)
            => Validation(MessageCodes.V_MAP_001, typeName);

        /// <summary>
        ///     Builds the failure for a member whose declared CLR type cannot be represented in XML.
        /// </summary>
        /// <param name="memberTypeName">Name of the unsupported member type.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <returns>
        ///     The failure for an unsupported member type.
        /// </returns>
        internal static IResult<SoapTypeMap> UnsupportedMemberType(string memberTypeName, string memberName)
            => Validation(MessageCodes.V_MAP_002, memberTypeName, memberName);

        /// <summary>
        ///     Builds the failure for a mapped member that exposes no public setter for the response
        ///     reader to bind into.
        /// </summary>
        /// <param name="memberTypeName">Name of the member type.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <returns>
        ///     The failure for a member that cannot be set.
        /// </returns>
        internal static IResult<SoapTypeMap> NotSettableMember(string memberTypeName, string memberName)
            => Validation(MessageCodes.V_MAP_002, memberTypeName, memberName);

        /// <summary>
        ///     Builds the failure for two members of one type that resolve to the same namespace, local
        ///     name and binding path.
        /// </summary>
        /// <param name="wireName">The duplicated wire name.</param>
        /// <param name="typeName">Name of the declaring type.</param>
        /// <returns>
        ///     The failure for a duplicated wire name.
        /// </returns>
        internal static IResult<SoapTypeMap> DuplicateWireName(string wireName, string typeName)
            => Validation(MessageCodes.V_MAP_003, wireName, typeName);

        /// <summary>
        ///     Builds the failure for a type that declares no namespace of its own and inherits none from
        ///     the call site.
        /// </summary>
        /// <param name="memberName">Name of the type or member that resolved to no namespace.</param>
        /// <returns>
        ///     The failure for a missing namespace.
        /// </returns>
        internal static IResult<SoapTypeMap> MissingNamespace(string memberName)
            => Validation(MessageCodes.V_MAP_004, memberName);

        /// <summary>
        ///     Builds the failure for a type that decorates some members with [SoapMember] and others with
        ///     [DataMember].
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns>
        ///     The failure for mixed member conventions.
        /// </returns>
        internal static IResult<SoapTypeMap> MixedConventions(string typeName)
            => Validation(MessageCodes.V_MAP_006, typeName);

        /// <summary>
        ///     Builds the failure for a type graph walk that passed the supported nesting depth.
        /// </summary>
        /// <param name="typeName">Name of the type being resolved when the cap was reached.</param>
        /// <param name="maxDepth">The maximum supported depth.</param>
        /// <returns>
        ///     The failure for a type graph nested too deep.
        /// </returns>
        internal static IResult<SoapTypeMap> DepthExceeded(string typeName, int maxDepth)
            => Validation(MessageCodes.V_MAP_007, typeName, maxDepth);

        /// <summary>
        ///     Builds the failure for an unexpected exception, typically from reflection, while reading
        ///     mapping metadata.
        /// </summary>
        /// <param name="typeName">Name of the type being read.</param>
        /// <param name="exception">The captured exception.</param>
        /// <returns>
        ///     The failure for a metadata read error, carrying the exception.
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