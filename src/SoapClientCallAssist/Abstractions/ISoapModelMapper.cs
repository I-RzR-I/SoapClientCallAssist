// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 13:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="ISoapModelMapper.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Map;
using System.Collections.Generic;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Abstractions
{
    /// <summary>
    ///     Interface for SOAP model mapper.
    /// </summary>
    public interface ISoapModelMapper
    {
        /// <summary>
        ///     Builds the SOAP body elements of the supplied operation call.
        /// </summary>
        /// <param name="request">The operation call to emit.</param>
        /// <returns>
        ///     An IResult&lt;IEnumerable&lt;XElement&gt;&gt; carrying the single operation element.
        /// </returns>
        IResult<IEnumerable<XElement>> ToBodies(SoapOperationRequest request);

        /// <summary>
        ///     Binds a SOAP response envelope onto a new instance of <typeparamref name="T" />. This
        ///     method never throws; every failure, including a SOAP fault carried by the response, is
        ///     returned as a failed result.
        /// </summary>
        /// <typeparam name="T">
        ///     The decorated model to bind. It must expose a public parameterless constructor.
        /// </typeparam>
        /// <param name="soapResponse">The raw response envelope.</param>
        /// <param name="protocolNamespace">
        ///     The expected envelope namespace. Both SOAP 1.1 and SOAP 1.2 are recognised regardless of
        ///     what is passed, and <see langword="null" /> is accepted.
        /// </param>
        /// <param name="soapXmlBodyTag">
        ///     (Optional)
        ///     The body element name, with or without a prefix, or <see langword="null" /> to locate the
        ///     body by SOAP namespace. This is the same tag the client endpoint accepts when it extracts
        ///     a response body.
        /// </param>
        /// <returns>
        ///     An IResult&lt;T&gt; carrying the bound instance.
        /// </returns>
        IResult<T> FromResponse<T>(string soapResponse, XNamespace protocolNamespace, string soapXmlBodyTag = null);
    }
}