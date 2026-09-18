// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-10 20:40
//
//  Last Modified By : RzR
//  Last Modified On : 2026-09-10 22:10
//  ***********************************************************************
//  <copyright file="SoapXmlDocumentLoader.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Text;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Extensions;
using System.IO;
using System.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;
using SoapClientCallAssist.Exceptions;

#endregion

namespace SoapClientCallAssist.Helpers
{
    /// <summary>
    ///     The one hardened SOAP parse every consumer shares, with no DTD, no resolver, bounded entity
    ///     expansion, bounded element nesting, and comments and processing instructions dropped.
    /// </summary>
    internal static class SoapXmlDocumentLoader
    {
        /// <summary>
        ///     Loads a document of any size through the hardened reader, or fails without throwing.
        /// </summary>
        /// <param name="xml">The raw XML text. Null is read as an empty document.</param>
        /// <param name="preserveWhitespace">True to keep whitespace-only text nodes.</param>
        /// <param name="malformedCode">The code a malformed document fails under.</param>
        /// <returns>
        ///     An IResult&lt;XmlDocument&gt; that fails under <paramref name="malformedCode" /> when the
        ///     text is not well-formed XML or breaks a reader limit, and with a depth failure when the
        ///     document nests deeper than <see cref="SoapContracts.MaxXmlDepth" />.
        /// </returns>
        internal static IResult<XmlDocument> Load(string xml, bool preserveWhitespace, MessageCodes malformedCode)
        {
            var document = new XmlDocument
            {
                PreserveWhitespace = preserveWhitespace,
                XmlResolver = null
            };

            try
            {
                using (var reader = CreateReader(xml, false))
                    document.Load(reader);

                return Result<XmlDocument>.Success(document);
            }
            catch (XmlDepthExceededException ex)
            {
                return Result<XmlDocument>
                    .Failure(MessageCodes.ER_XML_DEPTH.GetDescription(), ex.Message)
                    .WithOptionalError(ex, "parsing the SOAP document");
            }
            catch (XmlException ex)
            {
                return Result<XmlDocument>
                    .Failure(malformedCode.GetDescription(), Messages.GetErrorMessage(malformedCode))
                    .WithOptionalError(ex, "parsing the SOAP document");
            }
        }

        /// <summary>
        ///     Creates the hardened reader every parse in this library goes through. A document nesting
        ///     deeper than <see cref="SoapContracts.MaxXmlDepth" /> stops the reader with an
        ///     <see cref="XmlDepthExceededException" />.
        /// </summary>
        /// <param name="xml">The raw XML text. Null is read as an empty document.</param>
        /// <param name="capDocumentSize">
        ///     True to refuse a document longer than the character cap.
        /// </param>
        /// <returns>
        ///     The hardened reader, owning the text it reads from.
        /// </returns>
        internal static XmlReader CreateReader(string xml, bool capDocumentSize)
            => new DepthBoundedXmlReader(
                XmlReader.Create(new StringReader(xml.IfNullThenEmpty()), HardenedSettings(capDocumentSize)),
                SoapContracts.MaxXmlDepth);

        /// <summary>
        ///     Builds the reader settings every parse uses.
        /// </summary>
        /// <param name="capDocumentSize">True to bound the document at the character cap.</param>
        /// <returns>
        ///     The hardened settings.
        /// </returns>
        private static XmlReaderSettings HardenedSettings(bool capDocumentSize)
            => new()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = SoapContracts.MaxDocumentCharacters,
                MaxCharactersInDocument = capDocumentSize ? SoapContracts.MaxDocumentCharacters : 0,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                CloseInput = true,
                ConformanceLevel = ConformanceLevel.Document
            };
    }
}
