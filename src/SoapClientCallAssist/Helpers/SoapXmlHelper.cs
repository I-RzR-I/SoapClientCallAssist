// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-13 19:28
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-10 20:40
//  ***********************************************************************
//  <copyright file="SoapXmlHelper.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Collections;
using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Text;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;

// ReSharper disable PossibleMultipleEnumeration

#endregion

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     Builds the request message and Header of a SOAP call and locates the envelope, Body and
    ///     fault of a response.
    /// </summary>
    internal static class SoapXmlHelper
    {
        /// <summary>
        ///     Builds the HTTP request message of a call. For a GET, the first body's name and child
        ///     values are encoded into the URL as query parameters or path segments.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="soapUri">The endpoint URI.</param>
        /// <param name="bodies">The body elements; only the first is encoded into a GET URL.</param>
        /// <param name="buildGetRequestAsSlashUrl">
        ///     (Optional) True to append GET values as path segments.
        /// </param>
        /// <returns>
        ///     A successful IResult&lt;HttpRequestMessage&gt; carrying the request.
        /// </returns>
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

            return Result<HttpRequestMessage>.Success(new HttpRequestMessage(method, soapUri));
        }

        /// <summary>
        ///     Returns the bodies, rebuilding any body that declares a namespace but has a descendant
        ///     without one.
        /// </summary>
        /// <param name="bodies">The body elements to check.</param>
        /// <returns>
        ///     The bodies, with every element of a rebuilt body qualified in that body's namespace.
        /// </returns>
        internal static IEnumerable<XElement> CheckAndValidateSoapBodies(IEnumerable<XElement> bodies)
        {
            var soapBodies = new List<XElement>();
            foreach (var body in bodies)
            {
                var bodyNs = XElement.Parse(body.ToString()).Attribute("xmlns");
                if (bodyNs.IsNotNull())
                {
                    var anyParamWithNoNs = body.Descendants()
                        .Any(x => XElement.Parse(x.ToString()).Attribute("xmlns").IsNull());
                    if (anyParamWithNoNs.IsTrue())
                    {
                        var newBody = BuildNewBody(body);
                        soapBodies.Add(newBody);
                    }
                    else
                        soapBodies.Add(body);
                }
                else
                    soapBodies.Add(body);
            }

            return soapBodies;
        }

        /// <summary>
        ///     Locates the SOAP Body of a response, the single direct child of the envelope named Body
        ///     in the envelope's own namespace.
        /// </summary>
        /// <param name="xmlDocument">The parsed response document.</param>
        /// <param name="soapNamespace">
        ///     (Optional) The envelope namespace required, or null for either.
        /// </param>
        /// <param name="xmlBodyTag">
        ///     (Optional) A Body tag such as <c>s:Body</c>, or null; other names refused.
        /// </param>
        /// <returns>
        ///     The Body element, or null when the document is not a SOAP envelope, the tag names an
        ///     element other than the Body, or the envelope carries no such child or more than one.
        /// 
        /// </returns>
        internal static XmlElement LocateSoapBody(XmlDocument xmlDocument, string soapNamespace = null,
            string xmlBodyTag = null)
        {
            if (AcceptsBodyTag(xmlBodyTag).IsFalse())
                return null;

            var envelope = LocateSoapEnvelope(xmlDocument, soapNamespace);

            return SingleChildElement(envelope, SoapContracts.BodyLocalName, envelope?.NamespaceURI);
        }

        /// <summary>
        ///     Determines whether a caller-supplied Body tag is one the readers honour: absent, or
        ///     carrying <c>Body</c> as its local part behind any prefix.
        /// </summary>
        /// <param name="xmlBodyTag">The caller-supplied Body tag, or null.</param>
        /// <returns>
        ///     True when the tag is absent or names the Body.
        /// </returns>
        internal static bool AcceptsBodyTag(string xmlBodyTag)
            => xmlBodyTag.IsNullOrEmpty()
               || string.Equals(xmlBodyTag.LocalPartOf(), SoapContracts.BodyLocalName, StringComparison.Ordinal);

        /// <summary>
        ///     Returns the first element child of a parent, skipping the whitespace text an indented
        ///     response carries before its payload.
        /// </summary>
        /// <param name="parent">The parent element, or null.</param>
        /// <returns>
        ///     The first element child, or null when the parent is null or carries none.
        /// </returns>
        internal static XmlElement FirstChildElement(XmlElement parent)
        {
            if (parent.IsNull())
                return null;

            foreach (var child in parent!.ChildNodes)
            {
                if (child is XmlElement element)
                    return element;
            }

            return null;
        }

        /// <summary>
        ///     Locates the SOAP envelope of a document, refusing a document that is not one.
        /// </summary>
        /// <param name="xmlDocument">The parsed document.</param>
        /// <param name="soapNamespace">
        ///     (Optional) The envelope namespace required, or null for either.
        /// </param>
        /// <returns>
        ///     The envelope element, or null when the document is not a SOAP envelope the caller accepts.
        /// </returns>
        internal static XmlElement LocateSoapEnvelope(XmlDocument xmlDocument, string soapNamespace = null)
        {
            var envelope = xmlDocument?.DocumentElement;
            if (envelope.IsNull())
                return null;

            if (string.Equals(envelope!.LocalName, SoapContracts.EnvelopeLocalName, StringComparison.Ordinal).IsFalse())
                return null;

            return IsAcceptedEnvelopeNamespace(envelope.NamespaceURI, soapNamespace) ? envelope : null;
        }

        /// <summary>
        ///     Locates the SOAP fault a response carries: a direct child of the Body, in the same
        ///     protocol namespace.
        /// </summary>
        /// <param name="body">The Body element, or null when none was located.</param>
        /// <param name="soapNamespace">The SOAP protocol namespace the fault must carry.</param>
        /// <returns>
        ///     The fault element, or null when the Body carries none.
        /// </returns>
        internal static XmlElement LocateSoapFault(XmlElement body, string soapNamespace)
        {
            if (body.IsNull())
                return null;

            foreach (var child in body.ChildNodes)
            {
                if (!(child is XmlElement element))
                    continue;

                if (string.Equals(element.LocalName, SoapContracts.FaultLocalName, StringComparison.Ordinal).IsFalse())
                    continue;

                if (string.Equals(element.NamespaceURI, soapNamespace, StringComparison.Ordinal))
                    return element;
            }

            return null;
        }

        /// <summary>
        ///     Determines whether the namespace of the document element is one the caller accepts.
        /// </summary>
        /// <param name="actualNamespace">The namespace of the document element.</param>
        /// <param name="expectedNamespace">The namespace the caller demanded, or null.</param>
        /// <returns>
        ///     True when the namespace is accepted.
        /// </returns>
        private static bool IsAcceptedEnvelopeNamespace(string actualNamespace, string expectedNamespace)
            => expectedNamespace.IsNullOrEmpty()
                ? actualNamespace.IsProtocolNamespace()
                : string.Equals(actualNamespace, expectedNamespace, StringComparison.Ordinal);

        /// <summary>
        ///     Returns the only direct child element matching a qualified name.
        /// </summary>
        /// <param name="parent">The parent element, or null.</param>
        /// <param name="localName">The local name to match.</param>
        /// <param name="namespaceUri">The namespace to match, or null to match nothing.</param>
        /// <returns>
        ///     The single matching child, or null.
        /// </returns>
        internal static XmlElement SingleChildElement(XmlElement parent, string localName, string namespaceUri)
        {
            if (parent.IsNull() || namespaceUri.IsNull())
                return null;

            XmlElement match = null;

            foreach (var child in parent!.ChildNodes)
            {
                if (!(child is XmlElement element))
                    continue;

                if (string.Equals(element.LocalName, localName, StringComparison.Ordinal).IsFalse())
                    continue;

                if (string.Equals(element.NamespaceURI, namespaceUri, StringComparison.Ordinal).IsFalse())
                    continue;

                if (match.IsNotNull())
                    return null;

                match = element;
            }

            return match;
        }

        /// <summary>
        ///     Adds a Header element with the supplied headers to the envelope, plus an Action element
        ///     when an action is given and no header already carries one.
        /// </summary>
        /// <param name="soapEnvelope">[in,out] The SOAP envelope the Header is added to.</param>
        /// <param name="headers">The header elements, or null.</param>
        /// <param name="soapNamespace">The SOAP envelope namespace.</param>
        /// <param name="action">The SOAP action, or null.</param>
        internal static void BuildSoapHeader(ref XElement soapEnvelope, IEnumerable<XElement> headers,
            XNamespace soapNamespace, string action)
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
                {
                    soapEnvelope.Add(new XElement(soapNamespace + "Header",
                        new XElement("Action", action)));
                }
            }
        }

        /// <summary>
        ///     Rebuilds a body in its own namespace, replacing each unqualified child with a same-named
        ///     element carrying only its text.
        /// </summary>
        /// <param name="body">The body to rebuild.</param>
        /// <returns>
        ///     The rebuilt body element.
        /// </returns>
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