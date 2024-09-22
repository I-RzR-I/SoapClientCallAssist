// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssist
//  Author           : RzR
//  Created On       : 2024-09-12 18:48
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-13 16:35
// ***********************************************************************
//  <copyright file="ISoapClientEndpoint.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using AggregatedGenericResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

#endregion

namespace SoapClientCallAssist.Abstractions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     SOAP client endpoint.
    /// </summary>
    /// =================================================================================================
    public interface ISoapClientEndpoint
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sends a request.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="endpoint">The endpoint URI.</param>
        /// <param name="bodies">The bodies. SOAP request bodies.</param>
        /// <param name="headers">(Optional) The headers. SOAP request headers.</param>
        /// <param name="bodyEncoding">(Optional) The body encoding. Default encoding is UTF8.</param>
        /// <param name="action">(Optional) The action. SOAP Action.</param>
        /// <param name="ownSoapEnvelopeAttributes">
        ///     (Optional) The own defined SOAP Envelope attributes.
        /// </param>
        /// <param name="httpClientHeaders">(Optional) The HTTP client header variables.</param>
        /// <param name="buildGetRequestAsSlashUrl">
        ///     (Optional) Build current SOAP GET request as URL with separated param by slash ex:
        ///     'http:/site.local/GetDocuments/1'.
        /// </param>
        /// <returns>
        ///     An IResult&lt;HttpRequestMessage&gt;
        /// </returns>
        /// =================================================================================================
        IResult<HttpRequestMessage> BuildRequest(
            HttpMethod method,
            Uri endpoint,
            IEnumerable<XElement> bodies,
            IEnumerable<XElement> headers = null,
            Encoding bodyEncoding = null,
            string action = null,
            IEnumerable<XAttribute> ownSoapEnvelopeAttributes = null,
            Dictionary<string, IEnumerable<string>> httpClientHeaders = null,
            bool buildGetRequestAsSlashUrl = false);

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sends a request.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="soapRequest">.</param>
        /// <returns>
        ///     An IResult&lt;HttpRequestMessage&gt;
        /// </returns>
        /// =================================================================================================
        IResult<HttpRequestMessage> BuildRequest(
            HttpMethod method,
            BuildSoapRequestDto soapRequest);

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sends a request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>
        ///     An IResult&lt;HttpResponseMessage&gt;
        /// </returns>
        /// =================================================================================================
        IResult<HttpResponseMessage> SendRequest(HttpRequestMessage request);

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sends a request asynchronous.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">
        ///     (Optional) A token that allows processing to be cancelled.
        /// </param>
        /// <returns>
        ///     The send request.
        /// </returns>
        /// =================================================================================================
        Task<IResult<HttpResponseMessage>> SendRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets client timeout.
        /// </summary>
        /// <param name="clientTimeout">The client timeout.</param>
        /// <returns>
        ///     An IResult.
        /// </returns>
        /// =================================================================================================
        IResult SetClientTimeout(TimeSpan clientTimeout);

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Check body for fault code.
        /// </summary>
        /// <param name="soapResponse">The SOAP response.</param>
        /// <returns>
        ///     An IResult.
        /// </returns>
        /// =================================================================================================
        IResult CheckBodyForFaultCode(string soapResponse);

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets XmlNode response body.
        /// </summary>
        /// <param name="soapResponse">The SOAP response.</param>
        /// <param name="soapNamespace">(Optional) The SOAP namespace.</param>
        /// <param name="soapXmlBodyTag">(Optional) The SOAP XML body tag.</param>
        /// <returns>
        ///     The response body.
        /// </returns>
        /// =================================================================================================
        IResult<XmlNode> GetXmlNodeResponseBody(string soapResponse, string soapNamespace = null, string soapXmlBodyTag = null);

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets XNode response body.
        /// </summary>
        /// <param name="soapResponse">The SOAP response.</param>
        /// <param name="soapNamespace">(Optional) The SOAP namespace.</param>
        /// <param name="soapXmlBodyTag">(Optional) The SOAP XML body tag.</param>
        /// <returns>
        ///     The response body.
        /// </returns>
        /// =================================================================================================
        IResult<XNode> GetXNodeResponseBody(string soapResponse, string soapNamespace = null, string soapXmlBodyTag = null);
    }
}