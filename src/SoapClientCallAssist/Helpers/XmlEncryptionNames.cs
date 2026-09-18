// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="XmlEncryptionNames.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.Security.Cryptography.Xml;

#endregion

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     The XML Encryption 1.0 names that wrap a symmetric secret and encrypt message parts.
    /// </summary>
    internal static class XmlEncryptionNames
    {
        /// <summary>
        ///     The XML Encryption namespace.
        /// </summary>
        internal const string Namespace = EncryptedXml.XmlEncNamespaceUrl;

        /// <summary>
        ///     The prefix the encryption elements are written with.
        /// </summary>
        internal const string Prefix = "xenc";

        /// <summary>
        ///     The local name of the <c>xenc:EncryptedKey</c> element.
        /// </summary>
        internal const string EncryptedKeyLocalName = "EncryptedKey";

        /// <summary>
        ///     The local name of the <c>xenc:EncryptedData</c> element.
        /// </summary>
        internal const string EncryptedDataLocalName = "EncryptedData";

        /// <summary>
        ///     The local name of the <c>xenc:EncryptionMethod</c> element.
        /// </summary>
        internal const string EncryptionMethodLocalName = "EncryptionMethod";

        /// <summary>
        ///     The local name of the <c>xenc:CipherData</c> element.
        /// </summary>
        internal const string CipherDataLocalName = "CipherData";

        /// <summary>
        ///     The local name of the <c>xenc:CipherValue</c> element.
        /// </summary>
        internal const string CipherValueLocalName = "CipherValue";

        /// <summary>
        ///     The local name of the <c>xenc:ReferenceList</c> element.
        /// </summary>
        internal const string ReferenceListLocalName = "ReferenceList";

        /// <summary>
        ///     The local name of the <c>xenc:DataReference</c> element.
        /// </summary>
        internal const string DataReferenceLocalName = "DataReference";

        /// <summary>
        ///     The local name of the <c>xenc:KeyReference</c> element.
        /// </summary>
        internal const string KeyReferenceLocalName = "KeyReference";

        /// <summary>
        ///     The local name of the <c>Algorithm</c> attribute of an encryption method.
        /// </summary>
        internal const string AlgorithmAttributeName = "Algorithm";

        /// <summary>
        ///     The local name of the <c>Type</c> attribute of an <c>EncryptedData</c>.
        /// </summary>
        internal const string TypeAttributeName = "Type";

        /// <summary>
        ///     The <c>EncryptedData</c> type stating that an element's content was encrypted.
        /// </summary>
        internal const string ContentType = EncryptedXml.XmlEncNamespaceUrl + "Content";

        /// <summary>
        ///     The <c>EncryptedData</c> type stating that a whole element was encrypted.
        /// </summary>
        internal const string ElementType = EncryptedXml.XmlEncNamespaceUrl + "Element";

        /// <summary>
        ///     The AES-128-CBC algorithm identifier.
        /// </summary>
        internal const string Aes128Cbc = EncryptedXml.XmlEncAES128Url;

        /// <summary>
        ///     The AES-192-CBC algorithm identifier.
        /// </summary>
        internal const string Aes192Cbc = EncryptedXml.XmlEncAES192Url;

        /// <summary>
        ///     The AES-256-CBC algorithm identifier.
        /// </summary>
        internal const string Aes256Cbc = EncryptedXml.XmlEncAES256Url;

        /// <summary>
        ///     The RSA-OAEP (MGF1 with SHA-1) key transport algorithm identifier.
        /// </summary>
        internal const string RsaOaepMgf1p = EncryptedXml.XmlEncRSAOAEPUrl;

        /// <summary>
        ///     The RSA PKCS#1 v1.5 key transport algorithm identifier.
        /// </summary>
        internal const string Rsa15 = EncryptedXml.XmlEncRSA15Url;

        /// <summary>
        ///     The SHA-1 digest method identifier an OAEP encryption method names.
        /// </summary>
        internal const string Sha1DigestMethod = SignedXml.XmlDsigSHA1Url;
    }
}
