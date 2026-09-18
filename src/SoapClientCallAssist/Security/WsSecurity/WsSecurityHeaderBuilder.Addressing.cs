// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="WsSecurityHeaderBuilder.Addressing.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Text;
using SoapClientCallAssist.Helpers;
using System;
using System.Xml;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <content>
    ///     The WS-Addressing concern of the header builder. The addressing headers are inserted into
    ///     the SOAP Header before the Security header and each is covered by the primary signature. 
    /// </content>
    internal sealed partial class WsSecurityHeaderBuilder
    {
        /// <summary>
        ///     Emits <c>wsa:Action</c>, <c>wsa:MessageID</c>, <c>wsa:ReplyTo</c> and <c>wsa:To</c> as
        ///     the plan and the options call for.
        /// </summary>
        private void EmitAddressing()
        {
            var addressing = Options.Addressing;
            var ns = WsAddressingNames.Namespace(addressing.Version);

            var action = NewAddressingHeader(ns, WsAddressingNames.ActionLocalName, _plan.Action, true);
            InsertAddressingHeader(action);

            if (addressing.IncludeMessageId)
            {
                _messageId = WsAddressingNames.MessageIdUrnPrefix + Guid.NewGuid().ToString("D");
                InsertAddressingHeader(NewAddressingHeader(ns, WsAddressingNames.MessageIdLocalName, _messageId, false));
            }

            if (addressing.IncludeReplyTo)
            {
                var replyTo = NewAddressingHeader(ns, WsAddressingNames.ReplyToLocalName, null, false);
                AppendTextChild(replyTo, WsAddressingNames.Prefix, WsAddressingNames.AddressLocalName, ns, WsAddressingNames.Anonymous(addressing.Version));
                InsertAddressingHeader(replyTo);
            }

            InsertAddressingHeader(NewAddressingHeader(ns, WsAddressingNames.ToLocalName, _plan.To.AbsoluteUri, true));
        }

        /// <summary>
        ///     Creates one addressing header, stamped with a fresh <c>wsu:Id</c> and recorded for the
        ///     primary signature when the mode signs.
        /// </summary>
        /// <param name="ns">The addressing namespace.</param>
        /// <param name="localName">The header's local name.</param>
        /// <param name="text">
        ///     The header's text, or null for a header that only carries children.
        /// </param>
        /// <param name="mustUnderstand">True to mark the header <c>mustUnderstand</c>.</param>
        /// <returns>
        ///     The unattached header.
        /// </returns>
        private XmlElement NewAddressingHeader(string ns, string localName, string text, bool mustUnderstand)
        {
            var header = _document.CreateElement(WsAddressingNames.Prefix, localName, ns);

            if (mustUnderstand)
                header.Attributes.Append(NewMustUnderstandAttribute(header));

            var id = _ids.Stamp(_document, header, "wsa");

            if (text != null)
                header.InnerText = text;

            if (_plan.EmitsSignature)
                _addressingIds.Add(id);

            return header;
        }

        /// <summary>
        ///     Creates the <c>mustUnderstand</c> attribute of a signed header with an explicit envelope
        ///     prefix, so its in-memory form canonicalises exactly as its re-parsed form does.
        /// </summary>
        /// <param name="header">The header the attribute is created for.</param>
        /// <returns>
        ///     The unattached attribute.
        /// </returns>
        private XmlAttribute NewMustUnderstandAttribute(XmlElement header)
        {
            var prefix = _document.DocumentElement!.GetPrefixOfNamespace(_envelopeNamespace);

            if (prefix.IsMissing())
            {
                prefix = WsSecurityNames.EnvelopePrefixFallback;
                header.SetAttribute("xmlns:" + prefix, _envelopeNamespace);
            }

            var attribute = _document.CreateAttribute(prefix, WsSecurityNames.MustUnderstandLocalName, _envelopeNamespace);
            attribute.Value = "1";

            return attribute;
        }

        /// <summary>
        ///     Inserts an addressing header into the SOAP Header right before the Security header.
        /// </summary>
        /// <param name="header">The addressing header.</param>
        private void InsertAddressingHeader(XmlElement header)
            => _header.InsertBefore(header, _security);
    }
}
