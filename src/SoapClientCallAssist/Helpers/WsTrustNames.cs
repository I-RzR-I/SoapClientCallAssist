// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="WsTrustNames.cs" company="RzR SOFT & TECH">
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
    ///     The WS-Trust XML names that issue and cancel a secure conversation session, for both the
    ///     February 2005 draft and the OASIS December 2005 standard (WS-Trust 1.3).
    /// </summary>
    internal static class WsTrustNames
    {
        /// <summary>
        ///     The February 2005 WS-Trust namespace.
        /// </summary>
        internal const string NamespaceFebruary2005 = "http://schemas.xmlsoap.org/ws/2005/02/trust";

        /// <summary>
        ///     The December 2005 WS-Trust 1.3 namespace.
        /// </summary>
        internal const string NamespaceDecember2005 = "http://docs.oasis-open.org/ws-sx/ws-trust/200512";

        /// <summary>
        ///     The prefix the trust elements are written with.
        /// </summary>
        internal const string Prefix = "wst";

        /// <summary>
        ///     The local name of the <c>RequestSecurityToken</c> element.
        /// </summary>
        internal const string RequestSecurityTokenLocalName = "RequestSecurityToken";

        /// <summary>
        ///     The local name of the <c>RequestSecurityTokenResponse</c> element.
        /// </summary>
        internal const string RequestSecurityTokenResponseLocalName = "RequestSecurityTokenResponse";

        /// <summary>
        ///     The local name of the <c>RequestSecurityTokenResponseCollection</c> element WS-Trust 1.3
        ///     wraps its responses in.
        /// </summary>
        internal const string RequestSecurityTokenResponseCollectionLocalName = "RequestSecurityTokenResponseCollection";

        /// <summary>
        ///     The local name of the <c>RequestedSecurityToken</c> element.
        /// </summary>
        internal const string RequestedSecurityTokenLocalName = "RequestedSecurityToken";

        /// <summary>
        ///     The local name of the <c>RequestedProofToken</c> element.
        /// </summary>
        internal const string RequestedProofTokenLocalName = "RequestedProofToken";

        /// <summary>
        ///     The local name of the <c>RequestedAttachedReference</c> element.
        /// </summary>
        internal const string RequestedAttachedReferenceLocalName = "RequestedAttachedReference";

        /// <summary>
        ///     The local name of the <c>RequestedUnattachedReference</c> element.
        /// </summary>
        internal const string RequestedUnattachedReferenceLocalName = "RequestedUnattachedReference";

        /// <summary>
        ///     The local name of the <c>Entropy</c> element.
        /// </summary>
        internal const string EntropyLocalName = "Entropy";

        /// <summary>
        ///     The local name of the <c>BinarySecret</c> element.
        /// </summary>
        internal const string BinarySecretLocalName = "BinarySecret";

        /// <summary>
        ///     The local name of the <c>ComputedKey</c> element.
        /// </summary>
        internal const string ComputedKeyLocalName = "ComputedKey";

        /// <summary>
        ///     The local name of the <c>KeySize</c> element.
        /// </summary>
        internal const string KeySizeLocalName = "KeySize";

        /// <summary>
        ///     The local name of the <c>Lifetime</c> element.
        /// </summary>
        internal const string LifetimeLocalName = "Lifetime";

        /// <summary>
        ///     The local name of the <c>TokenType</c> element.
        /// </summary>
        internal const string TokenTypeLocalName = "TokenType";

        /// <summary>
        ///     The local name of the <c>RequestType</c> element.
        /// </summary>
        internal const string RequestTypeLocalName = "RequestType";

        /// <summary>
        ///     The local name of the <c>KeyType</c> element.
        /// </summary>
        internal const string KeyTypeLocalName = "KeyType";

        /// <summary>
        ///     The local name of the <c>ComputedKey</c> algorithm element inside a proof token.
        /// </summary>
        internal const string ComputedKeyAlgorithmLocalName = "ComputedKey";

        /// <summary>
        ///     The local name of the <c>CancelTarget</c> element.
        /// </summary>
        internal const string CancelTargetLocalName = "CancelTarget";

        /// <summary>
        ///     The local name of the <c>Expires</c> child of a <c>Lifetime</c> element, in the
        ///     WS-Security utility namespace.
        /// </summary>
        internal const string ExpiresLocalName = "Expires";

        /// <summary>
        ///     The suffix appended to a namespace to name the symmetric key type.
        /// </summary>
        internal const string SymmetricKeyTypeSuffix = "/SymmetricKey";

        /// <summary>
        ///     The suffix appended to a namespace to name the issue request type.
        /// </summary>
        internal const string IssueRequestTypeSuffix = "/Issue";

        /// <summary>
        ///     The suffix appended to a namespace to name the cancel request type.
        /// </summary>
        internal const string CancelRequestTypeSuffix = "/Cancel";

        /// <summary>
        ///     The WS-Addressing action of a February 2005 cancel request.
        /// </summary>
        internal const string CancelActionFebruary2005 = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Cancel";

        /// <summary>
        ///     The WS-Addressing action of a WS-Trust 1.3 cancel request.
        /// </summary>
        internal const string CancelActionDecember2005 = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Cancel";

        /// <summary>
        ///     The WS-Addressing action a February 2005 secure conversation establishes a security
        ///     context token with.
        /// </summary>
        internal const string ScIssueActionFebruary2005 = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/SCT";

        /// <summary>
        ///     The WS-Addressing action a WS-Trust 1.3 secure conversation establishes a security
        ///     context token with.
        /// </summary>
        internal const string ScIssueActionDecember2005 = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/SCT";

        /// <summary>
        ///     The WS-Addressing action a February 2005 secure conversation cancels a security context
        ///     token with.
        /// </summary>
        internal const string ScCancelActionFebruary2005 = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/SCT/Cancel";

        /// <summary>
        ///     The WS-Addressing action a WS-Trust 1.3 secure conversation cancels a security context
        ///     token with.
        /// </summary>
        internal const string ScCancelActionDecember2005 = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/SCT/Cancel";

        /// <summary>
        ///     The suffix appended to a namespace to name the P_SHA1 computed key algorithm.
        /// </summary>
        internal const string Psha1ComputedKeySuffix = "/CK/PSHA1";

        /// <summary>
        ///     The suffix appended to a namespace to name the nonce binary secret type.
        /// </summary>
        internal const string NonceBinarySecretTypeSuffix = "/Nonce";

        /// <summary>
        ///     The suffix appended to a namespace to name the symmetric key binary secret type.
        /// </summary>
        internal const string SymmetricKeyBinarySecretTypeSuffix = "/SymmetricKey";

        /// <summary>
        ///     The WS-Addressing action of a February 2005 issue request.
        /// </summary>
        internal const string IssueActionFebruary2005 = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue";

        /// <summary>
        ///     The WS-Addressing action of a February 2005 issue response.
        /// </summary>
        internal const string IssueResponseActionFebruary2005 = "http://schemas.xmlsoap.org/ws/2005/02/trust/RSTR/Issue";

        /// <summary>
        ///     The WS-Addressing action of a WS-Trust 1.3 issue request.
        /// </summary>
        internal const string IssueActionDecember2005 = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue";

        /// <summary>
        ///     The WS-Addressing action of a WS-Trust 1.3 issue response.
        /// </summary>
        internal const string IssueResponseActionDecember2005 = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RSTRC/IssueFinal";

        /// <summary>
        ///     Resolves the WS-Trust namespace paired with a secure conversation version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The namespace URI.
        /// </returns>
        internal static string Namespace(SoapSecureConversationVersionType version)
            => version == SoapSecureConversationVersionType.December2005 ? NamespaceDecember2005 : NamespaceFebruary2005;

        /// <summary>
        ///     Resolves the issue request type of a version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The request type URI.
        /// </returns>
        internal static string IssueRequestType(SoapSecureConversationVersionType version)
            => Namespace(version) + IssueRequestTypeSuffix;

        /// <summary>
        ///     Resolves the P_SHA1 computed key algorithm of a version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The algorithm URI.
        /// </returns>
        internal static string Psha1ComputedKey(SoapSecureConversationVersionType version)
            => Namespace(version) + Psha1ComputedKeySuffix;

        /// <summary>
        ///     Resolves the WS-Addressing action of an issue request of a version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The action URI.
        /// </returns>
        internal static string IssueAction(SoapSecureConversationVersionType version)
            => version == SoapSecureConversationVersionType.December2005 ? IssueActionDecember2005 : IssueActionFebruary2005;

        /// <summary>
        ///     Resolves the WS-Addressing action of a cancel request of a version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The action URI.
        /// </returns>
        internal static string CancelAction(SoapSecureConversationVersionType version)
            => version == SoapSecureConversationVersionType.December2005 ? CancelActionDecember2005 : CancelActionFebruary2005;

        /// <summary>
        ///     Resolves the cancel request type of a version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The request type URI.
        /// </returns>
        internal static string CancelRequestType(SoapSecureConversationVersionType version)
            => Namespace(version) + CancelRequestTypeSuffix;

        /// <summary>
        ///     Resolves the WS-Addressing action a secure conversation establishes a security context
        ///     token with, in the given version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The action URI.
        /// </returns>
        internal static string SecureConversationIssueAction(SoapSecureConversationVersionType version)
            => version == SoapSecureConversationVersionType.December2005 ? ScIssueActionDecember2005 : ScIssueActionFebruary2005;

        /// <summary>
        ///     Resolves the WS-Addressing action a secure conversation cancels a security context token
        ///     with, in the given version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The action URI.
        /// </returns>
        internal static string SecureConversationCancelAction(SoapSecureConversationVersionType version)
            => version == SoapSecureConversationVersionType.December2005 ? ScCancelActionDecember2005 : ScCancelActionFebruary2005;

        /// <summary>
        ///     Resolves the symmetric key type of a version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The key type URI.
        /// </returns>
        internal static string SymmetricKeyType(SoapSecureConversationVersionType version)
            => Namespace(version) + SymmetricKeyTypeSuffix;
    }
}
