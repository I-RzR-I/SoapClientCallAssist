// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-12 00:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 00:10
//  ***********************************************************************
//  <copyright file="EncryptedPart.cs" company="RzR SOFT & TECH">
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
    ///     One encrypted part of a response as the decryptor located it, before any key is derived.
    /// </summary>
    internal sealed class EncryptedPart
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EncryptedPart" /> class.
        /// </summary>
        /// <param name="element">The <c>EncryptedData</c> element.</param>
        /// <param name="token">The derived key token the part is keyed by.</param>
        /// <param name="cipherText">The cipher text with its initialisation vector in front.</param>
        /// <param name="content">True when the part stands for the content of its parent, false for a whole element.</param>
        internal EncryptedPart(XmlElement element, XmlElement token, byte[] cipherText, bool content)
        {
            Element = element;
            Token = token;
            CipherText = cipherText;
            Content = content;
        }

        /// <summary>
        ///     The <c>EncryptedData</c> element.
        /// </summary>
        internal XmlElement Element { get; }

        /// <summary>
        ///     The derived key token the part is keyed by.
        /// </summary>
        internal XmlElement Token { get; }

        /// <summary>
        ///     The cipher text with its initialisation vector in front.
        /// </summary>
        internal byte[] CipherText { get; }

        /// <summary>
        ///     True when the part stands for the Body's content, false for a whole element in the
        ///     Security header.
        /// </summary>
        internal bool Content { get; }
    }
}
