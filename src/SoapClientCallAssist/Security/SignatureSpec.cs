// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SignatureSpec.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Enums;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     Everything one <c>ds:Signature</c> is computed from. The key family has no default and the
    ///     emitter refuses a signature method from the other family.
    /// </summary>
    internal sealed class SignatureSpec
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SignatureSpec" /> class.
        /// </summary>
        /// <param name="keyFamily">The family of the signing key.</param>
        /// <param name="signatureMethod">The signature algorithm URI.</param>
        /// <param name="digestMethod">The digest algorithm URI applied to every reference.</param>
        /// <param name="canonicalization">
        ///     The canonicalization applied to SignedInfo and to every reference.
        /// </param>
        /// <param name="referenceIds">
        ///     The ids of the elements the signature covers, in reference order.
        /// </param>
        internal SignatureSpec(SignatureKeyFamily keyFamily, string signatureMethod, string digestMethod,
            SoapCanonicalizationType canonicalization, IReadOnlyList<string> referenceIds)
        {
            KeyFamily = keyFamily;
            SignatureMethod = signatureMethod;
            DigestMethod = digestMethod;
            Canonicalization = canonicalization;
            ReferenceIds = referenceIds;
        }

        /// <summary>
        ///     The family of the signing key.
        /// </summary>
        /// <value>
        ///     The key family.
        /// </value>
        internal SignatureKeyFamily KeyFamily { get; }

        /// <summary>
        ///     The signature algorithm URI.
        /// </summary>
        /// <value>
        ///     The signature method.
        /// </value>
        internal string SignatureMethod { get; }

        /// <summary>
        ///     The digest algorithm URI applied to every reference.
        /// </summary>
        /// <value>
        ///     The digest method.
        /// </value>
        internal string DigestMethod { get; }

        /// <summary>
        ///     The canonicalization applied to SignedInfo and to every reference.
        /// </summary>
        /// <value>
        ///     The canonicalization.
        /// </value>
        internal SoapCanonicalizationType Canonicalization { get; }

        /// <summary>
        ///     The ids of the elements the signature covers, in reference order.
        /// </summary>
        /// <value>
        ///     A list of identifiers of the references.
        /// </value>
        internal IReadOnlyList<string> ReferenceIds { get; }

        /// <summary>
        ///     The RSA private key, or null. Required for the <see cref="SignatureKeyFamily.Rsa" />
        ///     family and refused for any other.
        /// </summary>
        /// <value>
        ///     The rsa key.
        /// </value>
        internal RSA RsaKey { get; set; }

        /// <summary>
        ///     The HMAC key bytes, or null. Required for the <see cref="SignatureKeyFamily.Hmac" />
        ///     family and refused for any other; the emitter reads it and never keeps it.
        /// </summary>
        /// <value>
        ///     The hmac key.
        /// </value>
        internal byte[] HmacKey { get; set; }

        /// <summary>
        ///     The <c>KeyInfo</c> clause naming the signing token, or null to emit no <c>KeyInfo</c>.
        /// </summary>
        /// <value>
        ///     The key information clause.
        /// </value>
        internal KeyInfoClause KeyInfoClause { get; set; }

        /// <summary>
        ///     The plain <c>Id</c> attribute stamped on the <c>ds:Signature</c> for an endorsing
        ///     signature to reference, or null to stamp none.
        /// </summary>
        /// <value>
        ///     The identifier.
        /// </value>
        internal string Id { get; set; }

        /// <summary>
        ///     The <c>HMACOutputLength</c>, which must stay null. The emitter refuses any value.
        /// </summary>
        /// <value>
        ///     The length of the hmac output.
        /// </value>
        internal int? HmacOutputLength { get; set; }
    }
}
