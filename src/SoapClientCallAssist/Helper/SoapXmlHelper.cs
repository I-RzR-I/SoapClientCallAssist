// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssist
//  Author           : RzR
//  Created On       : 2024-09-13 19:28
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-15 19:23
// ***********************************************************************
//  <copyright file="SoapXmlHelper.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using AggregatedGenericResultMessage;
using AggregatedGenericResultMessage.Abstractions;
using DomainCommonExtensions.ArraysExtensions;
using DomainCommonExtensions.CommonExtensions;
using DomainCommonExtensions.DataTypeExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;
// ReSharper disable PossibleMultipleEnumeration

#endregion

namespace SoapClientCallAssist.Helper
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A SOAP XML helper.
    /// </summary>
    /// =================================================================================================
    internal static class SoapXmlHelper
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Verify and build get segment.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="soapUri">URI of the SOAP.</param>
        /// <param name="bodies">The bodies.</param>
        /// <param name="buildGetRequestAsSlashUrl">Build current SOAP GET request as URL with slash ex: 'http:/site.local/GetDocuments/1'</param>
        /// <returns>
        ///     An IResult&lt;HttpRequestMessage&gt;
        /// </returns>
        /// =================================================================================================
        internal static IResult<HttpRequestMessage> VerifyAndBuildGetSegment(HttpMethod method, Uri soapUri,
            IEnumerable<XElement> bodies, bool buildGetRequestAsSlashUrl = false)
        {
            if (method == HttpMethod.Get)
            {
                var body = bodies.First();
                var paramsUri = soapUri + $"/{body.Name}";
                foreach (var node in body.Nodes())
                {
                    var param = ((XElement)node).Name;
                    var val = ((XElement)node).Value;

                    paramsUri = buildGetRequestAsSlashUrl.IsTrue()
                        ? $"{paramsUri}/{val}"
                        : paramsUri.AddQueryString($"{param.LocalName}={val}");
                }

                return Result<HttpRequestMessage>.Success(new HttpRequestMessage(method, paramsUri));
            }
            else
                return Result<HttpRequestMessage>.Success(new HttpRequestMessage(method, soapUri));
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Enumerates check and validate SOAP bodies in this collection.
        /// </summary>
        /// <param name="bodies">The bodies.</param>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process check and validate SOAP bodies in
        ///     this collection.
        /// </returns>
        /// =================================================================================================
        internal static IEnumerable<XElement> CheckAndValidateSoapBodies(IEnumerable<XElement> bodies)
        {
            var soapBodies = new List<XElement>();
            foreach (var body in bodies)
            {
                var bodyNs = XElement.Parse(body.ToString()).Attribute("xmlns");
                if (bodyNs.IsNotNull())
                {
                    var anyParamWithNoNs = body.Descendants().Any(
                        x => XElement.Parse(x.ToString()).Attribute("xmlns").IsNull()
                    );
                    if (anyParamWithNoNs.IsTrue())
                    {
                        var newBody = BuildNewBody(body);
                        soapBodies.Add(newBody);
                    }
                    else
                    {
                        soapBodies.Add(body);
                    }
                }
                else
                {
                    soapBodies.Add(body);
                }
            }

            return soapBodies;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Parse get content body.
        /// </summary>
        /// <param name="xmlBodyTag">The XML body tag.</param>
        /// <param name="xmlDocument">The XML document.</param>
        /// <param name="soapNamespace">(Optional) The SOAP namespace.</param>
        /// <returns>
        ///     An XmlNodeList.
        /// </returns>
        /// =================================================================================================
        internal static XmlNodeList ParseGetContentBody(string xmlBodyTag, XmlDocument xmlDocument, string soapNamespace = null)
        {
            if (xmlBodyTag.IsNullOrEmpty())
            {
                var asmx = soapNamespace.IsNullOrEmpty()
                    ? xmlDocument.GetElementsByTagName("soap:Body")
                    : xmlDocument.GetElementsByTagName("soap:Body", soapNamespace!);

                var wcf = soapNamespace.IsNullOrEmpty()
                    ? xmlDocument.GetElementsByTagName("s:Body")
                    : xmlDocument.GetElementsByTagName("s:Body", soapNamespace!);

                return asmx.IsNull() ? wcf : asmx;
            }
            else
            {
                return soapNamespace.IsNullOrEmpty()
                    ? xmlDocument.GetElementsByTagName(xmlBodyTag)
                    : xmlDocument.GetElementsByTagName(xmlBodyTag, soapNamespace!);
            }
        }

        internal static void BuildSoapHeader(ref XElement soapEnvelope, IEnumerable<XElement> headers, XNamespace soapNamespace, string action)
        {
            if (headers.IsNullOrEmptyEnumerable().IsFalse())
            {
                var headerList = headers.ToList();
                if (action.IsNullOrEmpty().IsFalse() && headerList.Any(x => x.Name.LocalName.Equals("Action")).IsFalse())
                    headerList.Add(new XElement("Action", action));
                //new XAttribute(soapNamespace + "mustUnderstand", "1"),
                soapEnvelope.Add(new XElement(soapNamespace + "Header", headerList));
            }
            else
            {
                //new XAttribute(soapNamespace + "mustUnderstand", "1"),
                if (action.IsNullOrEmpty().IsFalse())
                    soapEnvelope.Add(new XElement(soapNamespace + "Header",
                        new XElement("Action", action)));
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Builds new body.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <returns>
        ///     An XElement.
        /// </returns>
        /// =================================================================================================
        private static XElement BuildNewBody(XElement body)
        {
            var currentNs = body.Name.Namespace;
            var ns = XNamespace.Get(currentNs.ToString());
            var elements = new List<XElement>();

            foreach (var element in body.Elements())
            {
                var nsAttribute = XElement.Parse(element.ToString()).Attribute("xmlns");
                if (nsAttribute.IsNull() || nsAttribute!.Value.IsNullOrEmpty())
                    elements.Add(new XElement(ns.GetName(element.Name.ToString()), element.Value));
                else
                    elements.Add(element);
            }

            return new XElement(ns.GetName(body.Name.LocalName), elements);
        }
    }
}