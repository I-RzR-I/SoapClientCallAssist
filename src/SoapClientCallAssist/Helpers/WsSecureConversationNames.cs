// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="WsSecureConversationNames.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Enums;

#endregion

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     The WS-SecureConversation XML names, including the derived key token vocabulary, for both
    ///     the February 2005 draft and the OASIS December 2005 standard.
    /// </summary>
    internal static class WsSecureConversationNames
    {
        /// <summary>
        ///     The February 2005 WS-SecureConversation namespace.
        /// </summary>
        internal const string NamespaceFebruary2005 = "http://schemas.xmlsoap.org/ws/2005/02/sc";

        /// <summary>
        ///     The December 2005 WS-SecureConversation namespace.
        /// </summary>
        internal const string NamespaceDecember2005 = "http://docs.oasis-open.org/ws-sx/ws-secureconversation/200512";

        /// <summary>
        ///     The prefix the secure conversation elements are written with.
        /// </summary>
        internal const string Prefix = "sc";

        /// <summary>
        ///     The local name of the <c>DerivedKeyToken</c> element.
        /// </summary>
        internal const string DerivedKeyTokenLocalName = "DerivedKeyToken";

        /// <summary>
        ///     The local name of the <c>SecurityContextToken</c> element.
        /// </summary>
        internal const string SecurityContextTokenLocalName = "SecurityContextToken";

        /// <summary>
        ///     The local name of the <c>Identifier</c> child of a security context token.
        /// </summary>
        internal const string IdentifierLocalName = "Identifier";

        /// <summary>
        ///     The local name of the <c>Instance</c> child of a security context token.
        /// </summary>
        internal const string InstanceLocalName = "Instance";

        /// <summary>
        ///     The local name of the <c>Nonce</c> child of a derived key token.
        /// </summary>
        internal const string NonceLocalName = "Nonce";

        /// <summary>
        ///     The local name of the <c>Length</c> child of a derived key token.
        /// </summary>
        internal const string LengthLocalName = "Length";

        /// <summary>
        ///     The local name of the <c>Offset</c> child of a derived key token.
        /// </summary>
        internal const string OffsetLocalName = "Offset";

        /// <summary>
        ///     The local name of the <c>Generation</c> child of a derived key token.
        /// </summary>
        internal const string GenerationLocalName = "Generation";

        /// <summary>
        ///     The local name of the <c>Label</c> child of a derived key token.
        /// </summary>
        internal const string LabelLocalName = "Label";

        /// <summary>
        ///     The local name of the <c>Properties</c> child of a derived key token, refused when present.
        /// </summary>
        internal const string PropertiesLocalName = "Properties";

        /// <summary>
        ///     The local name of the <c>Algorithm</c> attribute of a derived key token.
        /// </summary>
        internal const string AlgorithmAttributeName = "Algorithm";

        /// <summary>
        ///     The default derivation label, the specification's own label concatenated with itself.
        /// </summary>
        internal const string DefaultLabel = "WS-SecureConversationWS-SecureConversation";

        /// <summary>
        ///     The suffix appended to a namespace to name the derived key token value type.
        /// </summary>
        internal const string DerivedKeyTokenValueTypeSuffix = "/dk";

        /// <summary>
        ///     The suffix appended to a namespace to name the security context token value type.
        /// </summary>
        internal const string SecurityContextTokenValueTypeSuffix = "/sct";

        /// <summary>
        ///     The suffix appended to a namespace to name the P_SHA1 derivation algorithm.
        /// </summary>
        internal const string Psha1AlgorithmSuffix = "/dk/p_sha1";

        /// <summary>
        ///     Resolves the namespace of a WS-SecureConversation version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The namespace URI.
        /// </returns>
        internal static string Namespace(SoapSecureConversationVersionType version)
            => version == SoapSecureConversationVersionType.December2005 ? NamespaceDecember2005 : NamespaceFebruary2005;

        /// <summary>
        ///     Resolves the derived key token value type of a version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The value type URI.
        /// </returns>
        internal static string DerivedKeyTokenValueType(SoapSecureConversationVersionType version)
            => Namespace(version) + DerivedKeyTokenValueTypeSuffix;

        /// <summary>
        ///     Resolves the security context token value type of a version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The value type URI.
        /// </returns>
        internal static string SecurityContextTokenValueType(SoapSecureConversationVersionType version)
            => Namespace(version) + SecurityContextTokenValueTypeSuffix;

        /// <summary>
        ///     Resolves the P_SHA1 derivation algorithm identifier of a version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The algorithm URI.
        /// </returns>
        internal static string Psha1Algorithm(SoapSecureConversationVersionType version)
            => Namespace(version) + Psha1AlgorithmSuffix;
    }
}
