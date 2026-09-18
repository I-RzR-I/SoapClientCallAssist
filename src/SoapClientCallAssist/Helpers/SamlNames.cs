// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 22:15
//
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 22:15
//  ***********************************************************************
//  <copyright file="SamlNames.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;

#endregion

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     The SAML assertion XML names the header builder reads when it carries an issued assertion.
    ///     The token profile value types that reference an assertion live in
    ///     <see cref="WsSecurity11Names" />.
    /// </summary>
    internal static class SamlNames
    {
        /// <summary>
        ///     The SAML 1.1 assertion namespace.
        /// </summary>
        internal const string Saml11Namespace = "urn:oasis:names:tc:SAML:1.0:assertion";

        /// <summary>
        ///     The SAML 2.0 assertion namespace.
        /// </summary>
        internal const string Saml20Namespace = "urn:oasis:names:tc:SAML:2.0:assertion";

        /// <summary>
        ///     The local name of the assertion element in both versions.
        /// </summary>
        internal const string AssertionLocalName = "Assertion";

        /// <summary>
        ///     The unqualified identifier attribute of a SAML 2.0 assertion.
        /// </summary>
        internal const string Saml20IdAttributeName = "ID";

        /// <summary>
        ///     The unqualified identifier attribute of a SAML 1.1 assertion.
        /// </summary>
        internal const string Saml11IdAttributeName = "AssertionID";

        /// <summary>
        ///     The local name of the conditions element in both versions.
        /// </summary>
        internal const string ConditionsLocalName = "Conditions";

        /// <summary>
        ///     The local name of the SAML 2.0 subject element.
        /// </summary>
        internal const string SubjectLocalName = "Subject";

        /// <summary>
        ///     The local name of the SAML 2.0 subject confirmation element.
        /// </summary>
        internal const string SubjectConfirmationLocalName = "SubjectConfirmation";

        /// <summary>
        ///     The local name of the SAML 2.0 subject confirmation data element, which carries its own
        ///     validity instant.
        /// </summary>
        internal const string SubjectConfirmationDataLocalName = "SubjectConfirmationData";

        /// <summary>
        ///     The attribute naming the instant an assertion, or a subject confirmation, stops being
        ///     valid at.
        /// </summary>
        internal const string NotOnOrAfterAttributeName = "NotOnOrAfter";

        /// <summary>
        ///     Determines whether a namespace is one of the two assertion namespaces this library
        ///     carries.
        /// </summary>
        /// <param name="namespaceUri">The namespace, or null.</param>
        /// <returns>
        ///     True for the SAML 1.1 or the SAML 2.0 assertion namespace.
        /// </returns>
        internal static bool IsAssertionNamespace(string namespaceUri)
            => IsSaml20(namespaceUri) || string.Equals(namespaceUri, Saml11Namespace, StringComparison.Ordinal);

        /// <summary>
        ///     Determines whether a namespace is the SAML 2.0 assertion namespace.
        /// </summary>
        /// <param name="namespaceUri">The namespace, or null.</param>
        /// <returns>
        ///     True for SAML 2.0, false for anything else including SAML 1.1.
        /// </returns>
        internal static bool IsSaml20(string namespaceUri)
            => string.Equals(namespaceUri, Saml20Namespace, StringComparison.Ordinal);

        /// <summary>
        ///     Resolves the name of the identifier attribute of an assertion version.
        /// </summary>
        /// <param name="saml20">True for SAML 2.0, false for SAML 1.1.</param>
        /// <returns>
        ///     <c>ID</c> for SAML 2.0, <c>AssertionID</c> for SAML 1.1.
        /// </returns>
        internal static string IdAttributeName(bool saml20)
            => saml20 ? Saml20IdAttributeName : Saml11IdAttributeName;

        /// <summary>
        ///     Resolves the token profile value type a signature's key identifier names an assertion
        ///     of a version with.
        /// </summary>
        /// <param name="saml20">True for SAML 2.0, false for SAML 1.1.</param>
        /// <returns>
        ///     The key identifier value type.
        /// </returns>
        internal static string KeyIdentifierValueType(bool saml20)
            => saml20 ? WsSecurity11Names.SamlIdValueType : WsSecurity11Names.SamlAssertionIdValueType;

        /// <summary>
        ///     Resolves the WS-Security 1.1 token type of an assertion of a version.
        /// </summary>
        /// <param name="saml20">True for SAML 2.0, false for SAML 1.1.</param>
        /// <returns>
        ///     The token type.
        /// </returns>
        internal static string TokenType(bool saml20)
            => saml20 ? WsSecurity11Names.SamlV20TokenType : WsSecurity11Names.SamlV11TokenType;
    }
}
