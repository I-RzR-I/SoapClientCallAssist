// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-12 09:30
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
//  ***********************************************************************
//  <copyright file="SecureConversationIssueReader.cs" company="RzR SOFT & TECH">
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
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;
using SoapClientCallAssist.Security.WsSecurity;

#endregion

namespace SoapClientCallAssist.Security
{
    /// <summary>
    ///     Reads a decrypted, verified WS-Trust issue response into a session, computing the session
    ///     secret as <c>P_SHA1(clientEntropy, serverEntropy)</c>. The local copy of the secret is
    ///     zeroed before returning.
    /// </summary>
    internal static class SecureConversationIssueReader
    {
        /// <summary>
        ///     Reads a session from an issue response envelope.
        /// </summary>
        /// <param name="responseEnvelope">The decrypted, verified response envelope.</param>
        /// <param name="version">The WS-SecureConversation version.</param>
        /// <param name="clientEntropy">The client entropy.</param>
        /// <param name="secretLength">The length of the computed session secret, in bytes.</param>
        /// <param name="requestedKeySizeBits">The requested key size, in bits.</param>
        /// <returns>
        ///     The session, or a failed result when the response is not a usable issue response.
        /// </returns>
        internal static IResult<SoapSecureConversationSession> Read(string responseEnvelope, SoapSecureConversationVersionType version, 
            byte[] clientEntropy, int secretLength, int requestedKeySizeBits)
        {
            if (responseEnvelope.IsMissing())
                return Refuse(MessageCodes.V_SEC_072);

            XDocument document;

            try
            {
                document = XDocument.Parse(responseEnvelope, LoadOptions.None);
            }
            catch (Exception)
            {
                return Refuse(MessageCodes.V_SEC_072);
            }

            XNamespace trust = WsTrustNames.Namespace(version);

            var responses = document.Descendants(trust + WsTrustNames.RequestSecurityTokenResponseLocalName).ToList();
            if (responses.Count != 1)
                return Refuse(MessageCodes.V_SEC_072);

            var response = responses[0];

            var proof = RequireComputedKeyProof(response, trust, version);
            if (proof.IsNotNull())
                return proof;

            var serverEntropy = ReadServerEntropy(response, trust);
            if (serverEntropy.IsNull())
                return Refuse(MessageCodes.V_SEC_067);

            var keySize = RequireKeySize(response, trust, requestedKeySizeBits);
            if (keySize.IsNotNull())
                return keySize;

            var identifier = ReadContextIdentifier(response, trust, version);
            if (identifier.IsMissing())
                return Refuse(MessageCodes.V_SEC_069);

            var expires = ReadExpiry(response, trust);
            if (expires.HasValue.IsFalse())
                return Refuse(MessageCodes.V_SEC_070);

            var secret = WsSecurityKeyDerivation.ComputeKey(clientEntropy, serverEntropy, secretLength);

            if (secret.IsNull())
            {
                Array.Clear(serverEntropy, 0, serverEntropy.Length);

                return Refuse(MessageCodes.V_SEC_067);
            }

            try
            {
                return Result<SoapSecureConversationSession>.Success(
                    new SoapSecureConversationSession(identifier, secret, expires.Value, version));
            }
            finally
            {
                Array.Clear(secret, 0, secret.Length);
                Array.Clear(serverEntropy, 0, serverEntropy.Length);
            }
        }

        /// <summary>
        ///     Requires the response's proof token to name a P_SHA1 computed key, refusing a missing
        ///     token, a <c>BinarySecret</c> or any other computed key algorithm.
        /// </summary>
        /// <param name="response">The response element.</param>
        /// <param name="trust">The WS-Trust namespace.</param>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     A failed result, or null when the proof token names the P_SHA1 computed key.
        /// </returns>
        private static IResult<SoapSecureConversationSession> RequireComputedKeyProof(
            XElement response, XNamespace trust, SoapSecureConversationVersionType version)
        {
            var proofToken = response.Element(trust + WsTrustNames.RequestedProofTokenLocalName);
            if (proofToken.IsNull())
                return Refuse(MessageCodes.V_SEC_066);

            if (proofToken.Element(trust + WsTrustNames.BinarySecretLocalName).IsNotNull())
                return Refuse(MessageCodes.V_SEC_066);

            var computedKey = proofToken.Element(trust + WsTrustNames.ComputedKeyAlgorithmLocalName);
            if (computedKey.IsNull())
                return Refuse(MessageCodes.V_SEC_066);

            return string.Equals(computedKey.Value.Trim(), WsTrustNames.Psha1ComputedKey(version), StringComparison.Ordinal)
                ? null
                : Refuse(MessageCodes.V_SEC_066);
        }

        /// <summary>
        ///     Reads the service entropy, requiring a <c>BinarySecret</c> of at least 32 bytes that is
        ///     not all zero.
        /// </summary>
        /// <param name="response">The response element.</param>
        /// <param name="trust">The WS-Trust namespace.</param>
        /// <returns>
        ///     The entropy, or null when it is missing, short or all zero.
        /// </returns>
        private static byte[] ReadServerEntropy(XElement response, XNamespace trust)
        {
            var binarySecret = response.Element(trust + WsTrustNames.EntropyLocalName)?.Element(trust + WsTrustNames.BinarySecretLocalName);
            if (binarySecret.IsNull())
                return null;

            byte[] entropy;

            try
            {
                entropy = Convert.FromBase64String(binarySecret!.Value.Trim());
            }
            catch (FormatException)
            {
                return null;
            }

            if (entropy.Length < 32 || entropy.All(value => value == 0))
                return null;

            return entropy;
        }

        /// <summary>
        ///     Requires the response's echoed key size, when present, to equal the requested one.
        /// </summary>
        /// <param name="response">The response element.</param>
        /// <param name="trust">The WS-Trust namespace.</param>
        /// <param name="requestedKeySizeBits">The requested key size, in bits.</param>
        /// <returns>
        ///     A failed result, or null when the key size is absent or equal.
        /// </returns>
        private static IResult<SoapSecureConversationSession> RequireKeySize(XElement response, XNamespace trust, int requestedKeySizeBits)
        {
            var keySize = response.Element(trust + WsTrustNames.KeySizeLocalName);
            if (keySize.IsNull())
                return null;

            return int.TryParse(keySize.Value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value == requestedKeySizeBits
                ? null
                : Refuse(MessageCodes.V_SEC_068);
        }

        /// <summary>
        ///     Reads the security context token's identifier from the requested security token.
        /// </summary>
        /// <param name="response">The response element.</param>
        /// <param name="trust">The WS-Trust namespace.</param>
        /// <param name="version">The version.</param>
        /// <returns>
        ///     The identifier, or null when there is no single non-empty one.
        /// </returns>
        private static string ReadContextIdentifier(XElement response, XNamespace trust, SoapSecureConversationVersionType version)
        {
            XNamespace sc = WsSecureConversationNames.Namespace(version);

            var requested = response.Element(trust + WsTrustNames.RequestedSecurityTokenLocalName);
            var securityContextToken = requested?.Element(sc + WsSecureConversationNames.SecurityContextTokenLocalName);
            var identifiers = securityContextToken?.Elements(sc + WsSecureConversationNames.IdentifierLocalName).ToList();

            if (identifiers.IsNull() || identifiers!.Count != 1)
                return null;

            var identifier = identifiers[0].Value.Trim();

            return identifier.IsPresent() ? identifier : null;
        }

        /// <summary>
        ///     Reads and parses the lifetime's expiry, requiring it to be present, parseable and still
        ///     in the future.
        /// </summary>
        /// <param name="response">The response element.</param>
        /// <param name="trust">The WS-Trust namespace.</param>
        /// <returns>
        ///     The expiry, or null when it is missing, unparseable or already past.
        /// </returns>
        private static DateTimeOffset? ReadExpiry(XElement response, XNamespace trust)
        {
            XNamespace wsu = WsSecurityNames.WsuNamespace;

            var expires = response.Element(trust + WsTrustNames.LifetimeLocalName)?.Element(wsu + WsTrustNames.ExpiresLocalName);
            if (expires.IsNull())
                return null;

            if (DateTimeOffset.TryParse(
                    expires!.Value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var value).IsFalse())
                return null;

            return value <= DateTimeOffset.UtcNow ? (DateTimeOffset?)null : value;
        }

        /// <summary>
        ///     Builds a refusal under a validation code.
        /// </summary>
        /// <param name="code">The validation message code.</param>
        /// <returns>
        ///     A failed IResult&lt;SoapSecureConversationSession&gt;.
        /// </returns>
        private static IResult<SoapSecureConversationSession> Refuse(MessageCodes code)
            => Result<SoapSecureConversationSession>.Failure(code.GetDescription(), Messages.GetValidationMessage(code));
    }
}
