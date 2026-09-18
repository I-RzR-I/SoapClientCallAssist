// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="WsSecurityResponseInspection.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Text;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Extensions;
using SoapClientCallAssist.Helpers;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;
using System.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     The response-side checks every verifier shares: locating the single signature structurally,
    ///     refusing any signature shape outside the strict profile before cryptography runs, reporting
    ///     coverage, and applying the timestamp window.
    /// </summary>
    internal static class WsSecurityResponseInspection
    {
        /// <summary>
        ///     The canonicalization algorithms accepted for a <c>CanonicalizationMethod</c> or a
        ///     reference <c>Transform</c>. Every <c>*-WithComments</c> variant and every non-C14N
        ///     transform is refused.
        /// </summary>
        private static readonly HashSet<string> AllowedCanonicalizationAlgorithms = new(StringComparer.Ordinal)
        {
            SignedXml.XmlDsigExcC14NTransformUrl,
            SignedXml.XmlDsigC14NTransformUrl
        };

        /// <summary>
        ///     The signature algorithms accepted for the RSA family without any opt-in. No HMAC
        ///     algorithm is in this set.
        /// </summary>
        private static readonly HashSet<string> RsaSignatureMethods = new(StringComparer.Ordinal)
        {
            SignedXml.XmlDsigRSASHA256Url,
            SignedXml.XmlDsigRSASHA512Url
        };

        /// <summary>
        ///     The signature algorithms accepted for the HMAC family without any opt-in.
        /// </summary>
        private static readonly HashSet<string> HmacSignatureMethods = new(StringComparer.Ordinal)
        {
            "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256"
        };

        /// <summary>
        ///     The digest algorithms accepted for a reference's <c>DigestMethod</c> without any opt-in.
        /// </summary>
        private static readonly HashSet<string> AllowedDigestMethods = new(StringComparer.Ordinal)
        {
            SignedXml.XmlDsigSHA256Url,
            SignedXml.XmlDsigSHA512Url
        };

        /// <summary>
        ///     The SHA-1 signature algorithm of the RSA family, accepted only when the caller opts in
        ///     through <see cref="SoapVerificationPolicyDto.AllowSha1Algorithms" />.
        /// </summary>
        private const string RsaSha1SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

        /// <summary>
        ///     The SHA-1 signature algorithm of the HMAC family, accepted only under the same opt-in.
        /// </summary>
        private const string HmacSha1SignatureMethod = SignedXml.XmlDsigHMACSHA1Url;

        /// <summary>
        ///     The SHA-1 digest algorithm, accepted only when the caller opts in through
        ///     <see cref="SoapVerificationPolicyDto.AllowSha1Algorithms" />.
        /// </summary>
        private const string Sha1DigestMethod = SignedXml.XmlDsigSHA1Url;

        /// <summary>
        ///     Locates the single <c>wsse:Security</c> header of the response: the only such child of
        ///     the single Header that sits directly under the envelope.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <returns>
        ///     The Security header element, or null when the document is not a SOAP envelope, or the
        ///     Header or the Security header is absent or ambiguous.
        /// </returns>
        internal static XmlElement LocateSecurityHeader(XmlDocument document)
            => SoapXmlHelper.SingleChildElement(
                LocateHeader(document), WsSecurityNames.SecurityLocalName, WsSecurityNames.WsseNamespace);

        /// <summary>
        ///     Locates the single SOAP Header of the response.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <returns>
        ///     The Header element, or null when absent or ambiguous.
        /// </returns>
        internal static XmlElement LocateHeader(XmlDocument document)
        {
            var envelope = SoapXmlHelper.LocateSoapEnvelope(document);

            return SoapXmlHelper.SingleChildElement(envelope, WsSecurityNames.HeaderLocalName, envelope?.NamespaceURI);
        }

        /// <summary>
        ///     Locates the one <c>ds:Signature</c> in the response's single <c>wsse:Security</c> header
        ///     structurally, never by document-wide search. A header carrying two is refused.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <returns>
        ///     The signature element, or null when the Security header is absent or ambiguous, or
        ///     carries no signature or more than one.
        /// </returns>
        internal static XmlElement LocateSignature(XmlDocument document)
            => SoapXmlHelper.SingleChildElement(
                LocateSecurityHeader(document), WsSecurityNames.SignatureLocalName, SignedXml.XmlDsigNamespaceUrl);

        /// <summary>
        ///     Locates the single <c>wsu:Timestamp</c> directly under the single Security header,
        ///     by qualified name.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <returns>
        ///     The timestamp element, or null when it is absent or ambiguous.
        /// </returns>
        internal static XmlElement LocateTimestamp(XmlDocument document)
            => SoapXmlHelper.SingleChildElement(
                LocateSecurityHeader(document), WsSecurityNames.TimestampLocalName, WsSecurityNames.WsuNamespace);

        /// <summary>
        ///     Refuses, before the cryptographic check, any signature shape outside the strict profile:
        ///     a non-fragment reference, a non-C14N transform, a signature or digest algorithm off the
        ///     allow-list, an <c>HMACOutputLength</c>, or a <c>RetrievalMethod</c> in <c>KeyInfo</c>.
        /// 
        /// </summary>
        /// <param name="signatureElement">The single <c>ds:Signature</c> element found.</param>
        /// <param name="keyFamily">The family of key the signature must have been computed with.</param>
        /// <param name="allowSha1Algorithms">True when the caller opted in to SHA-1.</param>
        /// <returns>
        ///     Success when the shape is accepted, otherwise a failed result.
        /// </returns>
        internal static IResult ValidateSignatureShape(XmlElement signatureElement, 
            SignatureKeyFamily keyFamily, bool allowSha1Algorithms)
        {
            var namespaceManager = DsigNamespaces(signatureElement.OwnerDocument);

            var canonicalizationAlgorithm = AttributeValue(
                signatureElement, "ds:SignedInfo/ds:CanonicalizationMethod", "Algorithm", namespaceManager);
            if (canonicalizationAlgorithm.IsMissing() || AllowedCanonicalizationAlgorithms.Contains(canonicalizationAlgorithm).IsFalse())
                return ValidationFailure(MessageCodes.V_SEC_008);

            var signatureMethod = AttributeValue(
                signatureElement, "ds:SignedInfo/ds:SignatureMethod", "Algorithm", namespaceManager);
            if (IsAllowedSignatureMethod(signatureMethod, keyFamily, allowSha1Algorithms).IsFalse())
                return ValidationFailure(MessageCodes.V_SEC_008);

            if (signatureElement.SelectSingleNode("ds:SignedInfo/ds:SignatureMethod/ds:HMACOutputLength", namespaceManager).IsNotNull())
                return ValidationFailure(MessageCodes.V_SEC_008);

            if (signatureElement.SelectSingleNode("ds:KeyInfo//ds:RetrievalMethod", namespaceManager).IsNotNull())
                return ValidationFailure(MessageCodes.V_SEC_008);

            var references = signatureElement.SelectNodes("ds:SignedInfo/ds:Reference", namespaceManager);
            if (references.IsNull() || references!.Count == 0)
                return ValidationFailure(MessageCodes.V_SEC_008);

            foreach (XmlElement reference in references)
            {
                if (IsPlainSameDocumentFragment(reference.GetAttribute("URI")).IsFalse())
                    return ValidationFailure(MessageCodes.V_SEC_007);

                var digestMethod = AttributeValue(reference, "ds:DigestMethod", "Algorithm", namespaceManager);
                if (IsAllowedAlgorithm(digestMethod, AllowedDigestMethods, Sha1DigestMethod, allowSha1Algorithms).IsFalse())
                    return ValidationFailure(MessageCodes.V_SEC_008);

                var transformAlgorithms = reference.SelectNodes("ds:Transforms/ds:Transform", namespaceManager);
                if (transformAlgorithms.IsNull() || transformAlgorithms!.Count == 0)
                    return ValidationFailure(MessageCodes.V_SEC_008);

                foreach (XmlElement transform in transformAlgorithms)
                {
                    var algorithm = transform.GetAttribute("Algorithm");
                    if (algorithm.IsMissing() || AllowedCanonicalizationAlgorithms.Contains(algorithm).IsFalse())
                        return ValidationFailure(MessageCodes.V_SEC_008);
                }
            }

            return Result.Success();
        }

        /// <summary>
        ///     Determines whether a signature method belongs to the named key family and is accepted in
        ///     it.
        /// </summary>
        /// <param name="signatureMethod">The signature method read off the signature, or null.</param>
        /// <param name="keyFamily">The family the signature must belong to.</param>
        /// <param name="allowSha1">True when the caller opted in to SHA-1.</param>
        /// <returns>
        ///     True when the method is accepted for the family.
        /// </returns>
        private static bool IsAllowedSignatureMethod(string signatureMethod, SignatureKeyFamily keyFamily, bool allowSha1)
            => keyFamily == SignatureKeyFamily.Hmac
                ? IsAllowedAlgorithm(signatureMethod, HmacSignatureMethods, HmacSha1SignatureMethod, allowSha1)
                : IsAllowedAlgorithm(signatureMethod, RsaSignatureMethods, RsaSha1SignatureMethod, allowSha1);

        /// <summary>
        ///     Determines whether a reference URI is a plain same-document fragment, which is the only
        ///     shape this library dereferences.
        /// </summary>
        /// <param name="uri">The reference URI read off the signature.</param>
        /// <returns>
        ///     True when the URI is a plain '#id' fragment.
        /// </returns>
        internal static bool IsPlainSameDocumentFragment(string uri)
        {
            if (uri.IsMissing() || uri.StartsWith("#", StringComparison.Ordinal).IsFalse() || uri.Length < 2)
                return false;

            if (uri.IndexOf('#', 1) >= 0)
                return false;

            return uri.StartsWith("#xpointer(", StringComparison.Ordinal).IsFalse();
        }

        /// <summary>
        ///     Determines whether an algorithm identifier is one this library accepts in the position it
        ///     was read from.
        /// </summary>
        /// <param name="algorithm">The algorithm identifier read off the signature, or null.</param>
        /// <param name="allowed">The identifiers accepted for this position without any opt-in.</param>
        /// <param name="sha1Algorithm">The SHA-1 identifier valid for this position.</param>
        /// <param name="allowSha1">True when the caller opted in to SHA-1.</param>
        /// <returns>
        ///     True when the identifier is accepted.
        /// </returns>
        private static bool IsAllowedAlgorithm(string algorithm, 
            HashSet<string> allowed, string sha1Algorithm, bool allowSha1)
        {
            if (algorithm.IsMissing())
                return false;

            return allowed.Contains(algorithm)
                   || allowSha1 && string.Equals(algorithm, sha1Algorithm, StringComparison.Ordinal);
        }

        /// <summary>
        ///     Reads a single attribute's value off a single descendant located by XPath.
        /// </summary>
        /// <param name="context">The element the path is evaluated from.</param>
        /// <param name="path">The XPath to the descendant.</param>
        /// <param name="attributeName">The attribute local name.</param>
        /// <param name="namespaceManager">The namespace manager.</param>
        /// <returns>
        ///     The attribute value, or null when the descendant or the attribute is absent.
        /// </returns>
        private static string AttributeValue(XmlElement context, 
            string path, string attributeName, XmlNamespaceManager namespaceManager)
            => (context.SelectSingleNode(path, namespaceManager) as XmlElement)?.GetAttribute(attributeName);

        /// <summary>
        ///     Builds a namespace manager carrying the <c>ds</c> prefix.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>
        ///     The namespace manager.
        /// </returns>
        private static XmlNamespaceManager DsigNamespaces(XmlDocument document)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

            return namespaceManager;
        }

        /// <summary>
        ///     Builds the coverage report once the signature is known to be cryptographically valid.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <param name="signedXml">The loaded, already-verified signature.</param>
        /// <returns>
        ///     The coverage report.
        /// </returns>
        internal static SoapSignatureVerificationResult ResolveCoverage(XmlDocument document, WsuSignedXml signedXml)
        {
            var signedIds = SignedIds(signedXml);
            var signedLocalNames = new List<string>();

            foreach (var id in signedIds)
            {
                var resolved = signedXml.GetIdElement(document, id);
                if (resolved.IsNotNull())
                    signedLocalNames.Add(resolved!.LocalName);
            }

            var body = SoapXmlHelper.LocateSoapBody(document);
            var timestamp = LocateTimestamp(document);

            var coverage = new SoapSignatureVerificationResult
            {
                BodySigned = IsCoveredByReference(document, signedXml, body, signedIds),
                TimestampSigned = IsCoveredByReference(document, signedXml, timestamp, signedIds),
                SignedElementIds = signedIds,
                SignedElementLocalNames = signedLocalNames
            };

            if (coverage.TimestampSigned)
            {
                coverage.Created = ReadInstant(timestamp, WsSecurityNames.CreatedLocalName);
                coverage.Expires = ReadInstant(timestamp, WsSecurityNames.ExpiresLocalName);
            }

            return coverage;
        }

        /// <summary>
        ///     Lists the ids a loaded signature's references address, derived exactly the way the
        ///     dereferencer derives them.
        /// </summary>
        /// <param name="signedXml">The loaded signature.</param>
        /// <returns>
        ///     The ids, in reference order.
        /// </returns>
        internal static List<string> SignedIds(SignedXml signedXml)
        {
            var signedIds = new List<string>();

            if (signedXml.SignedInfo.IsNull())
                return signedIds;

            foreach (Reference reference in signedXml.SignedInfo!.References)
            {
                var id = ReferenceId(reference.Uri);
                if (id.IsPresent())
                    signedIds.Add(id);
            }

            return signedIds;
        }

        /// <summary>
        ///     Derives the element id a reference addresses exactly the way the dereferencer does, by
        ///     removing the single leading <c>#</c>.
        /// </summary>
        /// <param name="uri">The reference URI.</param>
        /// <returns>
        ///     The addressed id, or null when the URI is not a fragment.
        /// </returns>
        private static string ReferenceId(string uri)
            => IsPlainSameDocumentFragment(uri) ? uri.Substring(1) : null;

        /// <summary>
        ///     Determines whether a structurally located candidate element is the very element a
        ///     signature reference resolves to, by both an id membership check and a reference-equality
        ///     check on the resolved node.
        /// </summary>
        /// <param name="document">The parsed response document.</param>
        /// <param name="signedXml">The loaded signature.</param>
        /// <param name="candidate">
        ///     The structurally located candidate, or null when ambiguous or absent.
        /// </param>
        /// <param name="signedIds">The ids covered by a signature reference.</param>
        /// <returns>
        ///     True when the candidate is the element a reference actually covers.
        /// </returns>
        internal static bool IsCoveredByReference(XmlDocument document, 
            WsuSignedXml signedXml, XmlElement candidate, List<string> signedIds)
        {
            if (candidate.IsNull())
                return false;

            var id = candidate.GetAttribute(WsSecurityNames.IdLocalName, WsSecurityNames.WsuNamespace);
            if (id.IsMissing() || signedIds.Contains(id).IsFalse())
                return false;

            try
            {
                return ReferenceEquals(signedXml.GetIdElement(document, id), candidate);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     Reads one child instant of the timestamp the signature covers.
        /// </summary>
        /// <param name="timestamp">The covered <c>wsu:Timestamp</c> element.</param>
        /// <param name="localName">The child local name, <c>Created</c> or <c>Expires</c>.</param>
        /// <returns>
        ///     The instant in UTC, or null when the child is absent, ambiguous or unreadable.
        /// </returns>
        private static DateTimeOffset? ReadInstant(XmlElement timestamp, string localName)
        {
            var element = SoapXmlHelper.SingleChildElement(timestamp, localName, WsSecurityNames.WsuNamespace);
            if (element.IsNull())
                return null;

            return TryReadUtcInstant(element!.InnerText, out var instant) ? instant : null;
        }

        /// <summary>
        ///     Reads an ISO-8601 instant that states its own zone, and normalizes it to UTC.
        /// </summary>
        /// <param name="value">The raw element text.</param>
        /// <param name="instant">The instant in UTC.</param>
        /// <returns>
        ///     True when the value was read.
        /// </returns>
        private static bool TryReadUtcInstant(string value, out DateTimeOffset instant)
        {
            instant = default;

            if (value.IsMissing())
                return false;

            try
            {
                var parsed = XmlConvert.ToDateTime(value.Trim(), XmlDateTimeSerializationMode.RoundtripKind);
                if (parsed.Kind == DateTimeKind.Unspecified)
                    return false;

                instant = new DateTimeOffset(parsed).ToUniversalTime();

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        ///     Applies the policy to a coverage report, failing a valid signature that covers the wrong
        ///     content or a stale timestamp.
        /// </summary>
        /// <param name="coverage">The coverage report of a cryptographically valid signature.</param>
        /// <param name="policy">The conditions the signature must meet.</param>
        /// <returns>
        ///     Null when every named condition holds, otherwise the failure of the first that does not.
        /// </returns>
        internal static IResult ResolvePolicyFailure(SoapSignatureVerificationResult coverage, SoapVerificationPolicyDto policy)
        {
            if (policy.RequireBodySigned && coverage.BodySigned.IsFalse())
                return ValidationFailure(MessageCodes.V_SEC_009);

            if (policy.RequireValidTimestamp.IsFalse())
                return null;

            if (coverage.TimestampSigned.IsFalse())
                return ValidationFailure(MessageCodes.V_SEC_010);

            if (coverage.Created.HasValue.IsFalse() || coverage.Expires.HasValue.IsFalse())
                return ValidationFailure(MessageCodes.V_SEC_011);

            var skew = policy.ClockSkew > TimeSpan.Zero ? policy.ClockSkew : TimeSpan.Zero;
            var now = DateTimeOffset.UtcNow;

            var expired = coverage.Expires!.Value <= now - skew;
            var notYetValid = coverage.Created!.Value > now + skew;
            var invertedWindow = coverage.Expires.Value < coverage.Created.Value;

            return expired || notYetValid || invertedWindow
                ? ValidationFailure(MessageCodes.V_SEC_012)
                : null;
        }

        /// <summary>
        ///     Builds a validation failure from a message code, resolving and formatting its text
        ///     through the throw-safe helpers.
        /// </summary>
        /// <param name="code">The validation message code.</param>
        /// <param name="args">The message arguments, if the message text carries placeholders.</param>
        /// <returns>
        ///     A failure carrying the code and its formatted message.
        /// </returns>
        internal static IResult ValidationFailure(MessageCodes code, params object[] args)
            => Result.Failure(code.GetDescription(), Messages.GetValidationMessage(code).TryFormatWith(args));
    }
}
