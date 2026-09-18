// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="SoapSecurityDto.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

// ReSharper disable RedundantDefaultMemberInitializer

#endregion

namespace SoapClientCallAssist.Dto.Public
{
    /// <summary>
    ///     WS-Security message signing options for a single SOAP request.
    /// </summary>
    public class SoapSecurityDto
    {
        /// <summary>
        ///     Whether WS-Security signing is applied to the request. Defaults to true.
        /// </summary>
        /// <value>
        ///     True if enabled, false if not.
        /// </value>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     The signer that applies the signature. When null and <see cref="Enabled" /> is true, the
        ///     library's own <see cref="ISoapMessageSigner" /> implementation is used.
        /// </summary>
        /// <value>
        ///     The signer.
        /// </value>
        public ISoapMessageSigner Signer { get; set; }

        /// <summary>
        ///     Gets or sets the certificate the request is signed with.
        /// </summary>
        /// <value>
        ///     The signing certificate.
        /// </value>
        public X509Certificate2 SigningCertificate { get; set; }

        /// <summary>
        ///     Whether the SOAP Body is covered by the signature. Defaults to true.
        /// </summary>
        /// <value>
        ///     True if sign body, false if not.
        /// </value>
        public bool SignBody { get; set; } = true;

        /// <summary>
        ///     Whether a <c>wsu:Timestamp</c> is added to the Security header. Defaults to true.
        /// </summary>
        /// <value>
        ///     True if include timestamp, false if not.
        /// </value>
        public bool IncludeTimestamp { get; set; } = true;

        /// <summary>
        ///     Whether the timestamp is covered by the signature, true by default. Has no effect when
        ///     <see cref="IncludeTimestamp" /> is false.
        /// </summary>
        /// <value>
        ///     True if sign timestamp, false if not.
        /// </value>
        public bool SignTimestamp { get; set; } = true;

        /// <summary>
        ///     The validity window of the timestamp, applied from the moment it is created. Defaults to
        ///     five minutes.
        /// </summary>
        /// <value>
        ///     The timestamp time to live.
        /// </value>
        public TimeSpan TimestampTimeToLive { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        ///     Whether the Security header carries a <c>mustUnderstand</c> attribute. Defaults to true.
        /// </summary>
        /// <value>
        ///     True if we must understand, false if not.
        /// </value>
        public bool MustUnderstand { get; set; } = true;

        /// <summary>
        ///     The SOAP actor (SOAP 1.1) or role (SOAP 1.2) the Security header targets, or
        ///     <see langword="null" /> to leave it unset.
        /// </summary>
        /// <value>
        ///     The security actor.
        /// </value>
        public string SecurityActor { get; set; }

        /// <summary>
        ///     The digest algorithm used for every signed reference. Defaults to
        ///     <see cref="SoapDigestAlgorithmType.Sha256" />.
        /// </summary>
        /// <value>
        ///     The digest algorithm.
        /// </value>
        public SoapDigestAlgorithmType DigestAlgorithm { get; set; } = SoapDigestAlgorithmType.Sha256;

        /// <summary>
        ///     The signature algorithm the signature is computed with. Defaults to
        ///     <see cref="SoapSignatureAlgorithmType.RsaSha256" />.
        /// </summary>
        /// <value>
        ///     The signature algorithm.
        /// </value>
        public SoapSignatureAlgorithmType SignatureAlgorithm { get; set; } = SoapSignatureAlgorithmType.RsaSha256;

        /// <summary>
        ///     The canonicalization method used for the signature and for every reference transform.
        ///     Defaults to <see cref="SoapCanonicalizationType.ExclusiveC14N" />.
        /// </summary>
        /// <value>
        ///     The canonicalization.
        /// </value>
        public SoapCanonicalizationType Canonicalization { get; set; } = SoapCanonicalizationType.ExclusiveC14N;

        /// <summary>
        ///     The certificate the response is expected to be signed with. It is checked only when
        ///     <c>VerifyResponseSignature</c> is called; no response is verified automatically.
        /// </summary>
        /// <value>
        ///     The expected response certificate.
        /// </value>
        public X509Certificate2 ExpectedResponseCertificate { get; set; }

        /// <summary>
        ///     The conditions a response signature must meet, or <see langword="null" /> to apply the
        ///     strict defaults.
        /// </summary>
        /// <value>
        ///     The response verification policy.
        /// </value>
        public SoapVerificationPolicyDto ResponseVerificationPolicy { get; set; }

        /// <summary>
        ///     The verifier that checks the response signature, or <see langword="null" /> to use the
        ///     default implementation.
        /// </summary>
        /// <value>
        ///     The response verifier.
        /// </value>
        public ISoapMessageVerifier ResponseVerifier { get; set; }

        /// <summary>
        ///     The <c>wsu:Id</c> values of extra request elements to sign alongside the Body and
        ///     timestamp, or <see langword="null" />. Each id must match exactly one element, or signing
        ///     fails.
        /// </summary>
        /// <value>
        ///     A list of identifiers of the additional signed elements.
        /// </value>
        public IEnumerable<string> AdditionalSignedElementIds { get; set; }

        /// <summary>
        ///     The <c>UsernameToken</c> to carry, or <see langword="null" /> for none. On its own, with
        ///     no <see cref="SigningCertificate" />, it produces a token-only header with no signature. 
        /// </summary>
        /// <value>
        ///     The username token.
        /// </value>
        public SoapUsernameTokenDto UsernameToken { get; set; }

        /// <summary>
        ///     The WS-Addressing headers to add and sign, or <see langword="null" /> for none. Required
        ///     by <see cref="SymmetricBinding" /> and <see cref="SecureConversation" />.
        /// </summary>
        /// <value>
        ///     The addressing.
        /// </value>
        public SoapAddressingDto Addressing { get; set; }

        /// <summary>
        ///     The symmetric binding options, or <see langword="null" /> for the asymmetric X.509
        ///     binding. Setting it selects the symmetric mode, where <see cref="SigningCertificate" />
        ///     only endorses, <see cref="ExpectedResponseCertificate" />, <see cref="Signer" /> and
        ///     <see cref="ResponseVerifier" /> are refused, and <see cref="SecureConversation" /> is
        ///     excluded.
        /// </summary>
        /// <value>
        ///     The symmetric binding.
        /// </value>
        public SoapSymmetricBindingDto SymmetricBinding { get; set; }

        /// <summary>
        ///     Which parts of the request are encrypted, or <see langword="null" /> to encrypt nothing
        ///     beyond what the mode itself requires. Requires <see cref="SymmetricBinding" /> or
        ///     <see cref="SecureConversation" />.
        /// </summary>
        /// <value>
        ///     The encryption.
        /// </value>
        public SoapEncryptionDto Encryption { get; set; }

        /// <summary>
        ///     The SAML assertion to carry, or <see langword="null" /> for none.
        /// </summary>
        /// <value>
        ///     The saml token.
        /// </value>
        public SoapSamlTokenDto SamlToken { get; set; }

        /// <summary>
        ///     The secure conversation options, or <see langword="null" />. Setting it selects the
        ///     secure conversation mode, under the same rules as <see cref="SymmetricBinding" />, which
        ///     it cannot be combined with.
        /// </summary>
        /// <value>
        ///     The secure conversation.
        /// </value>
        public SoapSecureConversationDto SecureConversation { get; set; }

        /// <summary>
        ///     What the response is allowed and required to carry when it is checked against the sent
        ///     request, or <see langword="null" /> for the per-mode defaults.
        /// </summary>
        /// <value>
        ///     The response security.
        /// </value>
        public SoapResponseSecurityDto ResponseSecurity { get; set; }
    }
}