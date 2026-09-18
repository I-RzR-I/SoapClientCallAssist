// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-01 19:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-10 22:10
//  ***********************************************************************
//  <copyright file="SoapContracts.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     The fixed limits and well-known SOAP names the mapping and reading stages are bounded by.
    /// </summary>
    internal static class SoapContracts
    {
        /// <summary>
        ///     The largest response accepted, in characters. A larger payload is refused before any tree
        ///     is built.
        /// </summary>
        internal const int MaxDocumentCharacters = 8 * 1024 * 1024;

        /// <summary>
        ///     The deepest element nesting accepted when a document is parsed.
        /// </summary>
        internal const int MaxXmlDepth = 64;

        /// <summary>
        ///     The local name of the SOAP fault element.
        /// </summary>
        internal const string FaultLocalName = "Fault";

        /// <summary>
        ///     The deepest type graph walk accepted; a recursive graph stops at this cap.
        /// </summary>
        internal const int MaxGraphDepth = 32;

        /// <summary>
        ///     The local name of the SOAP body element.
        /// </summary>
        internal const string BodyLocalName = "Body";

        /// <summary>
        ///     The local name of the SOAP envelope element.
        /// </summary>
        internal const string EnvelopeLocalName = "Envelope";

        /// <summary>
        ///     The local name of the nil marker attribute.
        /// </summary>
        internal const string NilLocalName = "nil";

        /// <summary>
        ///     The XML schema instance namespace that carries the nil marker.
        /// </summary>
        internal const string SchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
    }
}