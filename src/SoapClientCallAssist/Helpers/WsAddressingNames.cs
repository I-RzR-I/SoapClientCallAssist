// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 21:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 21:10
//  ***********************************************************************
//  <copyright file="WsAddressingNames.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Enums;

#endregion

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     The WS-Addressing XML names, for both the 1.0 recommendation and the August 2004 member
    ///     submission.
    /// </summary>
    internal static class WsAddressingNames
    {
        /// <summary>
        ///     The WS-Addressing 1.0 namespace.
        /// </summary>
        internal const string Namespace10 = "http://www.w3.org/2005/08/addressing";

        /// <summary>
        ///     The WS-Addressing August 2004 namespace.
        /// </summary>
        internal const string NamespaceAugust2004 = "http://schemas.xmlsoap.org/ws/2004/08/addressing";

        /// <summary>
        ///     The anonymous reply address of WS-Addressing 1.0.
        /// </summary>
        internal const string Anonymous10 = "http://www.w3.org/2005/08/addressing/anonymous";

        /// <summary>
        ///     The anonymous reply address of WS-Addressing August 2004.
        /// </summary>
        internal const string AnonymousAugust2004 = "http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous";

        /// <summary>
        ///     The prefix the addressing headers are written with.
        /// </summary>
        internal const string Prefix = "wsa";

        /// <summary>
        ///     The local name of the <c>wsa:Action</c> header.
        /// </summary>
        internal const string ActionLocalName = "Action";

        /// <summary>
        ///     The local name of the <c>wsa:To</c> header.
        /// </summary>
        internal const string ToLocalName = "To";

        /// <summary>
        ///     The local name of the <c>wsa:MessageID</c> header.
        /// </summary>
        internal const string MessageIdLocalName = "MessageID";

        /// <summary>
        ///     The local name of the <c>wsa:ReplyTo</c> header.
        /// </summary>
        internal const string ReplyToLocalName = "ReplyTo";

        /// <summary>
        ///     The local name of the <c>wsa:RelatesTo</c> header a reply carries.
        /// </summary>
        internal const string RelatesToLocalName = "RelatesTo";

        /// <summary>
        ///     The local name of the <c>wsa:Address</c> child of an endpoint reference.
        /// </summary>
        internal const string AddressLocalName = "Address";

        /// <summary>
        ///     The local name of the <c>wsa:From</c> header.
        /// </summary>
        internal const string FromLocalName = "From";

        /// <summary>
        ///     The local name of the <c>wsa:FaultTo</c> header.
        /// </summary>
        internal const string FaultToLocalName = "FaultTo";

        /// <summary>
        ///     The URN prefix a generated <c>wsa:MessageID</c> is written with.
        /// </summary>
        internal const string MessageIdUrnPrefix = "urn:uuid:";

        /// <summary>
        ///     Resolves the namespace of a WS-Addressing version.
        /// </summary>
        /// <param name="version">The addressing version.</param>
        /// <returns>
        ///     The namespace URI.
        /// </returns>
        internal static string Namespace(SoapAddressingVersionType version)
            => version == SoapAddressingVersionType.WsAddressingAugust2004 ? NamespaceAugust2004 : Namespace10;

        /// <summary>
        ///     Resolves the anonymous reply address of a WS-Addressing version.
        /// </summary>
        /// <param name="version">The addressing version.</param>
        /// <returns>
        ///     The anonymous address URI.
        /// </returns>
        internal static string Anonymous(SoapAddressingVersionType version)
            => version == SoapAddressingVersionType.WsAddressingAugust2004 ? AnonymousAugust2004 : Anonymous10;
    }
}
