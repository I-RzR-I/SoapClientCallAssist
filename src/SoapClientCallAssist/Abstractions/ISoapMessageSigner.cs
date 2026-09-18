// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-03 00:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-04 22:44
//  ***********************************************************************
//  <copyright file="ISoapMessageSigner.cs" company="RzR SOFT & TECH">
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
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Abstractions
{
    /// <summary>
    ///     Applies WS-Security message signing to a built SOAP envelope.
    /// </summary>
    public interface ISoapMessageSigner
    {
        /// <summary>
        ///     Signs a SOAP envelope and returns the exact wire string the signature was computed over;
        ///     the caller sends that string as is, never a re-serialization of it.
        /// </summary>
        /// <param name="soapEnvelope">The built SOAP envelope, not yet serialized.</param>
        /// <param name="options">The signing options, including the signing certificate.</param>
        /// <returns>
        ///     An IResult&lt;string&gt; carrying the envelope's final wire representation: signed when
        ///     the options enable signing, and the envelope as built when they do not.
        /// </returns>
        IResult<string> Sign(XElement soapEnvelope, SoapSecurityDto options);
    }
}