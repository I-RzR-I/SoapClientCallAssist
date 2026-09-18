// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-10 22:10
//
//  Last Modified By : RzR
//  Last Modified On : 2026-09-10 22:10
//  ***********************************************************************
//  <copyright file="XmlDepthExceededException.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Extensions;
using System;
using System.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;
using SoapClientCallAssist.Helpers;


#endregion

namespace SoapClientCallAssist.Exceptions
{
    /// <summary>
    ///     The <see cref="XmlException" /> a <see cref="DepthBoundedXmlReader" /> raises when a document
    ///     nests deeper than its bound. Its message is the library's depth-exceeded text.
    /// </summary>
    [Serializable]
    internal sealed class XmlDepthExceededException : XmlException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="XmlDepthExceededException" /> class.
        /// </summary>
        /// <param name="maxDepth">The deepest element nesting that was accepted.</param>
        internal XmlDepthExceededException(int maxDepth)
            : base(Messages.GetErrorMessage(MessageCodes.ER_XML_DEPTH).TryFormatWith(maxDepth))
        {
        }
    }
}
