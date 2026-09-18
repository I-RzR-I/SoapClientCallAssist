// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SignatureEmitter.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Collections;
using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Text;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     Computes one <c>ds:Signature</c> from a <see cref="SignatureSpec" /> and attaches it to a
    ///     Security header. The spec's key family must match its signature method, and a truncated
    ///     HMAC is never emitted.
    /// </summary>
    internal static class SignatureEmitter
    {
        /// <summary>
        ///     The signature methods the RSA family may name.
        /// </summary>
        private static readonly HashSet<string> RsaSignatureMethods = new(StringComparer.Ordinal)
        {
            SignedXml.XmlDsigRSASHA1Url,
            SignedXml.XmlDsigRSASHA256Url,
            SignedXml.XmlDsigRSASHA384Url,
            SignedXml.XmlDsigRSASHA512Url
        };

        /// <summary>
        ///     The HMAC-SHA256 signature method.
        /// </summary>
        private const string HmacSha256Method = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256";

        /// <summary>
        ///     The HMAC-SHA1 signature method.
        /// </summary>
        private const string HmacSha1Method = SignedXml.XmlDsigHMACSHA1Url;

        /// <summary>
        ///     Computes the signature the spec describes over the document and appends it to the
        ///     Security header.
        /// </summary>
        /// <param name="document">
        ///     The envelope document, already carrying every element the references name.
        /// </param>
        /// <param name="security">The attached Security header the signature is appended to.</param>
        /// <param name="spec">The signature spec.</param>
        /// <returns>
        ///     The attached signature and a copy of its value, or a failed result for an inconsistent
        ///     spec or when computing the signature throws.
        /// </returns>
        internal static IResult<EmittedSignature> Emit(XmlDocument document, XmlElement security, SignatureSpec spec)
        {
            var refused = Validate(spec);
            if (refused.IsNotNull())
                return refused;

            try
            {
                var signedXml = new WsuSignedXml(document);
                signedXml.SignedInfo!.CanonicalizationMethod = spec.Canonicalization.GetDescription();
                signedXml.SignedInfo.SignatureMethod = spec.SignatureMethod;

                foreach (var id in spec.ReferenceIds)
                {
                    var reference = new Reference("#" + id) { DigestMethod = spec.DigestMethod };
                    reference.AddTransform(NewCanonicalizationTransform(spec.Canonicalization));
                    signedXml.AddReference(reference);
                }

                if (spec.KeyInfoClause.IsNotNull())
                {
                    var keyInfo = new KeyInfo();
                    keyInfo.AddClause(spec.KeyInfoClause);
                    signedXml.KeyInfo = keyInfo;
                }

                if (spec.Id.IsPresent())
                    signedXml.Signature.Id = spec.Id;

                if (spec.KeyFamily == SignatureKeyFamily.Rsa)
                {
                    signedXml.SigningKey = spec.RsaKey;
                    signedXml.ComputeSignature();
                }
                else
                {
                    using (var mac = NewHmac(spec.SignatureMethod, spec.HmacKey))
                        signedXml.ComputeSignature(mac);
                }

                var element = (XmlElement)document.ImportNode(signedXml.GetXml(), true);
                security.AppendChild(element);

                return Result<EmittedSignature>.Success(
                    new EmittedSignature(element, (byte[])signedXml.SignatureValue.Clone(), spec.Id));
            }
            catch (Exception ex)
            {
                return Result<EmittedSignature>
                    .Failure(MessageCodes.ER_SEC_C14N.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_C14N))
                    .WithOptionalError(ex, "computing the WS-Security signature");
            }
        }

        /// <summary>
        ///     Refuses a spec that pairs a key family with the wrong method or key, asks for a truncated
        ///     HMAC, or covers nothing.
        /// </summary>
        /// <param name="spec">The spec.</param>
        /// <returns>
        ///     A failed result, or null when the spec is consistent.
        /// </returns>
        private static IResult<EmittedSignature> Validate(SignatureSpec spec)
        {
            if (spec.IsNull() || spec!.ReferenceIds.IsNullOrEmptyEnumerable() || spec.SignatureMethod.IsMissing() || spec.DigestMethod.IsMissing())
                return SpecFailure();

            if (spec.HmacOutputLength.HasValue)
                return SpecFailure();

            switch (spec.KeyFamily)
            {
                case SignatureKeyFamily.Rsa:
                    return spec.RsaKey.IsNotNull() && spec.HmacKey.IsNull() && RsaSignatureMethods.Contains(spec.SignatureMethod)
                        ? null
                        : SpecFailure();

                case SignatureKeyFamily.Hmac:
                    return spec.HmacKey.IsNotNull() && spec.HmacKey!.Length > 0 && spec.RsaKey.IsNull() && IsHmacMethod(spec.SignatureMethod)
                        ? null
                        : SpecFailure();

                default:
                    return SpecFailure();
            }
        }

        /// <summary>
        ///     Determines whether a signature method is one of the HMAC methods this emitter computes.
        /// </summary>
        /// <param name="signatureMethod">The signature method URI.</param>
        /// <returns>
        ///     True when it is an HMAC method.
        /// </returns>
        private static bool IsHmacMethod(string signatureMethod)
            => string.Equals(signatureMethod, HmacSha256Method, StringComparison.Ordinal)
               || string.Equals(signatureMethod, HmacSha1Method, StringComparison.Ordinal);

        /// <summary>
        ///     Builds the keyed hash of an HMAC signature method.
        /// </summary>
        /// <param name="signatureMethod">
        ///     The signature method URI, already known to be an HMAC method.
        /// </param>
        /// <param name="key">The HMAC key.</param>
        /// <returns>
        ///     The keyed hash.
        /// </returns>
        private static HMAC NewHmac(string signatureMethod, byte[] key)
            => string.Equals(signatureMethod, HmacSha1Method, StringComparison.Ordinal)
                ? new HMACSHA1(key)
                : new HMACSHA256(key);

        /// <summary>
        ///     Builds the reference transform matching the configured canonicalization method.
        /// </summary>
        /// <param name="canonicalization">The configured canonicalization method.</param>
        /// <returns>
        ///     The transform.
        /// </returns>
        private static Transform NewCanonicalizationTransform(SoapCanonicalizationType canonicalization)
            => canonicalization == SoapCanonicalizationType.InclusiveC14N
                ? new XmlDsigC14NTransform()
                : new XmlDsigExcC14NTransform();

        /// <summary>
        ///     Builds the failure returned for an inconsistent spec.
        /// </summary>
        /// <returns>
        ///     A failed IResult&lt;EmittedSignature&gt;.
        /// </returns>
        private static IResult<EmittedSignature> SpecFailure()
            => Result<EmittedSignature>.Failure(MessageCodes.ER_SEC_SGN.GetDescription(), Messages.GetErrorMessage(MessageCodes.ER_SEC_SGN));
    }
}
