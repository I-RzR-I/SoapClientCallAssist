// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-12 18:48
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 20:10
//  ***********************************************************************
//  <copyright file="ISoapClientEndpoint.cs" company="RzR SOFT & TECH">
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
    /// <summary>
    ///     A SOAP client for one protocol version that builds requests, sends them and reads the
    ///     responses.
    /// </summary>
    public interface ISoapClientEndpoint
    {
        /// <summary>
        ///     Builds an HTTP request carrying a SOAP envelope assembled from the supplied bodies and
        ///     headers.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="endpoint">The endpoint URI.</param>
        /// <param name="bodies">The elements placed in the SOAP Body.</param>
        /// <param name="headers">(Optional) The elements placed in the SOAP Header, or null.</param>
        /// <param name="bodyEncoding">(Optional) The body encoding, or null for UTF-8.</param>
        /// <param name="action">(Optional) The SOAP action, or null.</param>
        /// <param name="ownSoapEnvelopeAttributes">
        ///     (Optional) Extra Envelope element attributes, or null.
        /// </param>
        /// <param name="httpClientHeaders">
        ///     (Optional) The HTTP headers added to the request, or null.
        /// </param>
        /// <param name="buildGetRequestAsSlashUrl">
        ///     (Optional) True to put GET values in the path.
        /// </param>
        /// <returns>
        ///     An IResult&lt;HttpRequestMessage&gt; carrying the request ready to send.
        /// </returns>
        IResult<HttpRequestMessage> BuildRequest(HttpMethod method, Uri endpoint,
            IEnumerable<XElement> bodies, IEnumerable<XElement> headers = null,
            Encoding bodyEncoding = null, string action = null,
            IEnumerable<XAttribute> ownSoapEnvelopeAttributes = null,
            Dictionary<string, IEnumerable<string>> httpClientHeaders = null,
            bool buildGetRequestAsSlashUrl = false);

        /// <summary>
        ///     Builds an HTTP request from a request description, applying WS-Security when the
        ///     description enables it. A request with message security enabled must use POST.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="soapRequest">The client, envelope and security settings of the request.</param>
        /// <returns>
        ///     An IResult&lt;HttpRequestMessage&gt; carrying the request ready to send.
        /// </returns>
        IResult<HttpRequestMessage> BuildRequest(HttpMethod method,
            BuildSoapRequestDto soapRequest);

        /// <summary>
        ///     Sends a built request and succeeds only on a 2xx status. Any other status fails under a
        ///     code naming its class, with the <see cref="HttpResponseMessage" /> still carried in
        ///     <see cref="IResult{T}.Response" />.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>
        ///     The response on a 2xx status, or a failed result for any other outcome.
        /// </returns>
        IResult<HttpResponseMessage> SendRequest(HttpRequestMessage request);

        /// <summary>
        ///     Sends a built request asynchronously under the same status contract as
        ///     <see cref="SendRequest" />.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">(Optional) A token that cancels the send.</param>
        /// <returns>
        ///     The response on a 2xx status, or the failure described on <see cref="SendRequest" />.
        /// </returns>
        Task<IResult<HttpResponseMessage>> SendRequestAsync(HttpRequestMessage request,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Sets the timeout applied to every later send on this client.
        ///     <see cref="Timeout.InfiniteTimeSpan" /> disables the deadline.
        /// </summary>
        /// <param name="clientTimeout">The client timeout.</param>
        /// <returns>
        ///     Success once stored, or a failed result when <see cref="HttpClient" /> would refuse the
        ///     value.
        /// </returns>
        IResult SetClientTimeout(TimeSpan clientTimeout);

        /// <summary>
        ///     Checks the Body directly under the envelope for a SOAP fault in this client's protocol
        ///     namespace.
        /// </summary>
        /// <param name="soapResponse">The raw SOAP response.</param>
        /// <returns>
        ///     Success when the Body carries no fault, or a failed result.
        /// </returns>
        IResult CheckBodyForFaultCode(string soapResponse);

        /// <summary>
        ///     Returns the first element child of the SOAP Body as an <see cref="XmlNode" />, skipping
        ///     comments, processing instructions and whitespace.
        /// </summary>
        /// <param name="soapResponse">The raw SOAP response.</param>
        /// <param name="soapNamespace">
        ///     (Optional) The envelope namespace, or null to accept either protocol.
        /// </param>
        /// <param name="soapXmlBodyTag">
        ///     (Optional) The Body tag, with or without a prefix, or null.
        /// </param>
        /// <returns>
        ///     The payload element, or a failed result when the Body is missing, empty or unreadable.
        /// </returns>
        IResult<XmlNode> GetXmlNodeResponseBody(string soapResponse, string soapNamespace = null, string soapXmlBodyTag = null);

        /// <summary>
        ///     Returns the first element child of the SOAP Body as an <see cref="XNode" />, under the
        ///     same rules as <see cref="GetXmlNodeResponseBody" />.
        /// </summary>
        /// <param name="soapResponse">The raw SOAP response.</param>
        /// <param name="soapNamespace">
        ///     (Optional) The envelope namespace, or null to accept either protocol.
        /// </param>
        /// <param name="soapXmlBodyTag">
        ///     (Optional) The Body tag, with or without a prefix, or null.
        /// </param>
        /// <returns>
        ///     The payload element, or the failure described on <see cref="GetXmlNodeResponseBody" />.
        /// </returns>
        IResult<XNode> GetXNodeResponseBody(string soapResponse, string soapNamespace = null, string soapXmlBodyTag = null);
    }
}