// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-08 23:10
//  ***********************************************************************
//  <copyright file="WsuSignedXml.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     A <see cref="SignedXml" /> that also resolves the WS-Security <c>wsu:Id</c> attribute,
    ///     which
    ///     .NET's own <see cref="SignedXml.GetIdElement" /> does not know about.
    /// </summary>
    /// <seealso cref="T:System.Security.Cryptography.Xml.SignedXml"/>
    internal sealed class WsuSignedXml : SignedXml
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="WsuSignedXml" /> class.
        /// </summary>
        /// <param name="document">The document the signature is read from or written into.</param>
        internal WsuSignedXml(XmlDocument document)
            : base(document) { }

        /// <summary>
        ///     Resolves the element a signature reference names, falling back to the WS-Security
        ///     <c>wsu:Id</c> attribute and to the <c>AssertionID</c> of a SAML 1.1 assertion when the
        ///     base implementation finds nothing.
        /// </summary>
        /// <exception cref="CryptographicException">
        ///     Thrown when more than one element carries the id.
        /// </exception>
        /// <param name="document">The document the signature is read from or written into.</param>
        /// <param name="idValue">The id a reference names.</param>
        /// <returns>
        ///     The single element carrying the id, or null when no element does.
        /// </returns>
        public override XmlElement GetIdElement(XmlDocument document, string idValue)
        {
            var byDefault = base.GetIdElement(document, idValue);
            if (byDefault.IsNotNull())
                return byDefault;

            var matches = WsuIdLookup.FindById(document, idValue);
            matches.AddRange(SamlAssertionLookup.FindById(document, idValue));

            if (matches.Count == 0)
                return null;

            if (matches.Count > 1)
                throw new CryptographicException($"Ambiguous wsu:Id '{idValue}'.");

            return matches[0];
        }
    }
}