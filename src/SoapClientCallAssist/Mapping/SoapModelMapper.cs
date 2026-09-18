// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-08-31 17:08
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-04 22:44
//  ***********************************************************************
//  <copyright file="SoapModelMapper.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Map;
using SoapClientCallAssist.Helpers.Map;
using SoapClientCallAssist.Readers;
using System.Collections.Generic;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Mapping
{
    /// <summary>
    ///     The default <see cref="ISoapModelMapper" />. It holds no state and a single instance can
    ///     be shared by every caller.
    /// </summary>
    public sealed class SoapModelMapper : ISoapModelMapper
    {
        /// <inheritdoc/>
        public IResult<IEnumerable<XElement>> ToBodies(SoapOperationRequest request)
            => SoapModelEmitter.Emit(request);

        /// <inheritdoc/>
        public IResult<T> FromResponse<T>(string soapResponse, XNamespace protocolNamespace, string soapXmlBodyTag = null)
            => SoapResponseReader.Read<T>(soapResponse, protocolNamespace, soapXmlBodyTag);
    }
}