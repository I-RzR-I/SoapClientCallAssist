// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 00:10
//  ***********************************************************************
//  <copyright file="ISoapResponseSecurity.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

#endregion

namespace SoapClientCallAssist.Abstractions
{
    /// <summary>
    ///     Checks a SOAP response against the request it answers, or against a certificate.
    ///     Verification is never automatic; a response is checked only when one of these methods is
    ///     called.
    /// </summary>
    public interface ISoapResponseSecurity
    {
        /// <summary>
        ///     Verifies a response against the key material snapshotted on the sent request, by
        ///     certificate or by derived keys as the request's mode dictates. The snapshot is single-use,
        ///     and a second call is refused.
        /// </summary>
        /// <param name="sentRequest">The request that was built and sent.</param>
        /// <param name="soapResponse">The raw SOAP response.</param>
        /// <returns>
        ///     Success only when the signature is valid, meets the request's policy, and is bound to
        ///     that request; a failed result when the request carries no key material or was already
        ///     checked.
        /// </returns>
        IResult<SoapSignatureVerificationResult> Verify(HttpRequestMessage sentRequest, string soapResponse);

        /// <summary>
        ///     Verifies the WS-Security signature on a raw SOAP response against a certificate, under a
        ///     policy. A response to a symmetric-bound request cannot be checked this way.
        /// </summary>
        /// <param name="soapResponse">The raw, unparsed SOAP response.</param>
        /// <param name="expectedCertificate">
        ///     The trusted certificate; the response's own is ignored.
        /// </param>
        /// <param name="policy">The conditions to meet, or null for the strict defaults.</param>
        /// <returns>
        ///     Success only when the signature is valid and meets every condition the policy names.
        /// </returns>
        IResult<SoapSignatureVerificationResult> Verify(string soapResponse, 
            X509Certificate2 expectedCertificate, SoapVerificationPolicyDto policy);

        /// <summary>
        ///     Verifies the WS-Security signature on a raw SOAP response with the certificate, policy
        ///     and verifier read from the request's <see cref="SoapSecurityDto" />. Symmetric-binding
        ///     and secure-conversation options are refused.
        /// </summary>
        /// <param name="soapResponse">The raw, unparsed SOAP response.</param>
        /// <param name="security">The message security options the request was built with.</param>
        /// <returns>
        ///     Success only when the signature is valid and meets the configured policy, or a failed
        ///     result.
        /// </returns>
        IResult<SoapSignatureVerificationResult> Verify(string soapResponse, SoapSecurityDto security);

        /// <summary>
        ///     Decrypts a response and verifies the signature over its plaintext Body, returning the
        ///     plaintext only when both succeed. Requires a request built with
        ///     <see cref="SoapResponseSecurityDto.AllowDecryption" />; its single-use snapshot is
        ///     consumed
        ///     whatever the outcome.
        /// </summary>
        /// <param name="sentRequest">The request message that was built and sent.</param>
        /// <param name="soapResponse">The raw, unparsed SOAP response.</param>
        /// <returns>
        ///     The decrypted, verified envelope, or a failed result carrying no plaintext and no cause.
        /// </returns>
        IResult<string> DecryptAndVerify(HttpRequestMessage sentRequest, string soapResponse);

        /// <summary>
        ///     Decrypts and verifies exactly as <see cref="DecryptAndVerify" /> does; there is no
        ///     decryption without verification.
        /// </summary>
        /// <param name="sentRequest">The request message that was built and sent.</param>
        /// <param name="soapResponse">The raw, unparsed SOAP response.</param>
        /// <returns>
        ///     An IResult&lt;string&gt; carrying the decrypted, verified envelope.
        /// </returns>
        IResult<string> Decrypt(HttpRequestMessage sentRequest, string soapResponse);
    }
}
