// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-04 22:44
//  ***********************************************************************
//  <copyright file="ISoapMessageVerifier.cs" company="RzR SOFT & TECH">
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
using System.Security.Cryptography.X509Certificates;

#endregion

namespace SoapClientCallAssist.Abstractions
{
    /// <summary>
    ///     Verifies a WS-Security signature on a raw SOAP response, against a certificate the caller
    ///     trusts.
    /// </summary>
    public interface ISoapMessageVerifier
    {
        /// <summary>
        ///     Verifies the WS-Security signature on a raw SOAP response under the strict default
        ///     policy: the signature must cover the Body and a signed, currently valid <c>wsu:Timestamp</c>
        ///     .
        /// </summary>
        /// <param name="soapResponse">The raw, unparsed SOAP response.</param>
        /// <param name="expectedCertificate">
        ///     The trusted certificate; the response's own is ignored.
        /// </param>
        /// <returns>
        ///     Success, describing what the signature covered, only when it is valid and covers the Body
        ///     and a current timestamp.
        /// </returns>
        IResult<SoapSignatureVerificationResult> Verify(string soapResponse, X509Certificate2 expectedCertificate);

        /// <summary>
        ///     Verifies the WS-Security signature on a raw SOAP response under a caller-supplied policy.
        /// </summary>
        /// <param name="soapResponse">The raw, unparsed SOAP response.</param>
        /// <param name="expectedCertificate">
        ///     The trusted certificate; the response's own is ignored.
        /// </param>
        /// <param name="policy">The conditions to meet, or null for the strict defaults.</param>
        /// <returns>
        ///     Success, describing what the signature covered, only when it is valid and meets every
        ///     condition the policy names.
        /// </returns>
        IResult<SoapSignatureVerificationResult> Verify(string soapResponse, 
            X509Certificate2 expectedCertificate, SoapVerificationPolicyDto policy);
    }
}