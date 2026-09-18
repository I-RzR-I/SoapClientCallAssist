// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="EmittedSignature.cs" company="RzR SOFT & TECH">
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

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     A <c>ds:Signature</c> the emitter has computed and attached, together with the facts about
    ///     it the request later needs to check its reply against.
    /// </summary>
    internal sealed class EmittedSignature
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EmittedSignature" /> class.
        /// </summary>
        /// <param name="element">The attached signature element.</param>
        /// <param name="signatureValue">A copy of the raw signature value.</param>
        /// <param name="id">The plain <c>Id</c> stamped on the element, or null.</param>
        internal EmittedSignature(XmlElement element, byte[] signatureValue, string id)
        {
            Element = element;
            SignatureValue = signatureValue;
            Id = id;
        }

        /// <summary>
        ///     The attached signature element.
        /// </summary>
        internal XmlElement Element { get; }

        /// <summary>
        ///     The raw signature value bytes.
        /// </summary>
        internal byte[] SignatureValue { get; }

        /// <summary>
        ///     The plain <c>Id</c> stamped on the element, or null when none was stamped.
        /// </summary>
        internal string Id { get; }
    }
}
