// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-01 19:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-01 20:04
//  ***********************************************************************
//  <copyright file="SoapContracts.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.Xml;

#endregion

namespace SoapClientCallAssist.Helper
{
    internal static class SoapContracts
    {
        /// <summary>
        ///     (Immutable)
        ///     The largest response accepted, in characters. A larger payload is refused before any tree
        ///     is built rather than after it has been allocated.
        /// </summary>
        internal const int MaxDocumentCharacters = 8 * 1024 * 1024;

        /// <summary>
        ///     (Immutable)
        ///     The deepest element nesting accepted. <see cref="XmlReaderSettings" /> exposes no depth
        ///     cap, so the parse walk counts depth itself and refuses a deeply nested payload.
        /// </summary>
        internal const int MaxXmlDepth = 64;

        /// <summary>
        ///     (Immutable) the namespace of a xmlns declaration, which is markup rather than data.
        /// </summary>
        internal const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

        /// <summary>
        ///     (Immutable) the local name of the SOAP fault element.
        /// </summary>
        internal const string FaultLocalName = "Fault";

        /// <summary>
        ///     (Immutable)
        ///     The deepest type graph a caller may walk. A recursive graph such as
        ///     <c>Product -&gt; Detail -&gt; Product</c> is bounded by this cap instead of overflowing
        ///     the stack.
        /// </summary>
        internal const int MaxGraphDepth = 32;

        /// <summary>
        ///     (Immutable) the local name of the SOAP body element.
        /// </summary>
        internal const string BodyLocalName = "Body";

        /// <summary>
        ///     (Immutable) the local name of the nil marker attribute.
        /// </summary>
        internal const string NilLocalName = "nil";

        /// <summary>
        ///     (Immutable) the XML schema instance namespace that carries the nil marker.
        /// </summary>
        internal const string SchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
    }
}