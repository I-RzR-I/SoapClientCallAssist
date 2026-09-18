// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2026-09-11 22:15
//
//  Last Modified By : RzR
//  Last Modified On : 2026-09-11 22:15
//  ***********************************************************************
//  <copyright file="WsSecurityHeaderBuilder.Saml.cs" company="RzR SOFT & TECH">
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
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Helpers;
using System;
using System.Security.Cryptography;
using System.Xml;
using Messages = SoapClientCallAssist.Helpers.DefaultResultMessageHelper;
using MessageCodes = SoapClientCallAssist.Enums.MessageCodesType;

#endregion

namespace SoapClientCallAssist.Security.WsSecurity
{
    /// <summary>
    ///     The SAML concern of the header builder. It carries an assertion into the Security header
    ///     and, for holder-of-key, points the primary signature's <c>KeyInfo</c> at it without
    ///     validating the issuer's signature.
    /// </summary>
    internal sealed partial class WsSecurityHeaderBuilder
    {
        /// <summary>
        ///     The unqualified attribute names <c>SignedXml</c> resolves a reference by before the
        ///     WS-Security utility id.
        /// </summary>
        private static readonly string[] UnqualifiedIdAttributeNames =
        {
            "Id", "id", "ID", SamlNames.Saml11IdAttributeName
        };

        /// <summary>
        ///     Imports the caller's <c>saml:Assertion</c> node for node, whitespace included. It refuses
        ///     a non-assertion, a lapsed assertion, and a missing or ambiguous identifier.
        /// </summary>
        partial void EmitSamlAssertion()
        {
            var token = Options.SamlToken;
            var assertion = token.Assertion;

            if (SamlAssertionLookup.IsAssertion(assertion).IsFalse())
            {
                _hookOutcome = SamlRefusal(MessageCodes.V_SEC_084);

                return;
            }

            var saml20 = SamlNames.IsSaml20(assertion.NamespaceURI);
            var id = SamlAssertionLookup.IdOf(assertion);

            if (id.IsMissing())
            {
                _hookOutcome = SamlRefusal(MessageCodes.V_SEC_085);

                return;
            }

            if (IsLapsed(assertion, saml20, token.ClockSkew))
            {
                _hookOutcome = SamlRefusal(MessageCodes.V_SEC_087);

                return;
            }

            var imported = (XmlElement)_document.ImportNode(assertion, true);
            _security.AppendChild(imported);

            if (IsUniquelyAddressable(imported, id).IsFalse())
            {
                _hookOutcome = SamlRefusal(MessageCodes.V_SEC_086);

                return;
            }

            if (token.Confirmation == SoapSamlConfirmationType.HolderOfKey)
            {
                if (token.SignAssertion)
                    _referenceIds.Add(id);

                _primaryKeyInfoClause = SecurityTokenReferenceClause.ToSamlAssertion(id, saml20);
            }

            _hookOutcome = Result.Success();
        }

        /// <summary>
        ///     Determines whether the assertion's conditions, or on SAML 2.0 any subject confirmation,
        ///     carry a <c>NotOnOrAfter</c> at or before now less the tolerated skew. An unreadable
        ///     instant counts as lapsed.
        /// </summary>
        /// <param name="assertion">The assertion.</param>
        /// <param name="saml20">True for SAML 2.0, false for SAML 1.1.</param>
        /// <param name="skew">The tolerated clock skew; a negative value counts as none.</param>
        /// <returns>
        ///     True when the assertion is lapsed or its validity cannot be read.
        /// </returns>
        private static bool IsLapsed(XmlElement assertion, bool saml20, TimeSpan skew)
        {
            var limit = DateTime.UtcNow - (skew > TimeSpan.Zero ? skew : TimeSpan.Zero);
            var samlNamespace = assertion.NamespaceURI;

            foreach (XmlNode child in assertion.ChildNodes)
            {
                if (child is not XmlElement element || string.Equals(element.NamespaceURI, samlNamespace, StringComparison.Ordinal).IsFalse())
                    continue;

                if (string.Equals(element.LocalName, SamlNames.ConditionsLocalName, StringComparison.Ordinal) && IsPast(element, limit))
                    return true;

                if (saml20 && string.Equals(element.LocalName, SamlNames.SubjectLocalName, StringComparison.Ordinal) && HasLapsedConfirmation(element, limit))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Determines whether any subject confirmation data of a SAML 2.0 subject has lapsed.
        /// </summary>
        /// <param name="subject">The subject element.</param>
        /// <param name="limit">The instant a <c>NotOnOrAfter</c> must lie after.</param>
        /// <returns>
        ///     True when a confirmation carries a validity instant at or before the limit.
        /// </returns>
        private static bool HasLapsedConfirmation(XmlElement subject, DateTime limit)
        {
            foreach (XmlNode confirmation in subject.ChildNodes)
            {
                if (confirmation is not XmlElement confirmationElement
                    || string.Equals(confirmationElement.LocalName, SamlNames.SubjectConfirmationLocalName, StringComparison.Ordinal).IsFalse())
                    continue;

                foreach (XmlNode data in confirmationElement.ChildNodes)
                {
                    if (data is XmlElement dataElement
                        && string.Equals(dataElement.LocalName, SamlNames.SubjectConfirmationDataLocalName, StringComparison.Ordinal)
                        && IsPast(dataElement, limit))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Determines whether an element's <c>NotOnOrAfter</c> lies at or before a limit. An
        ///     element without the attribute is not past; one whose attribute cannot be read is.
        /// </summary>
        /// <param name="element">The element carrying the attribute.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>
        ///     True when the instant is past or unreadable.
        /// </returns>
        private static bool IsPast(XmlElement element, DateTime limit)
        {
            if (element.HasAttribute(SamlNames.NotOnOrAfterAttributeName).IsFalse())
                return false;

            try
            {
                return XmlConvert.ToDateTime(element.GetAttribute(SamlNames.NotOnOrAfterAttributeName), XmlDateTimeSerializationMode.Utc) <= limit;
            }
            catch (FormatException)
            {
                return true;
            }
        }

        /// <summary>
        ///     Determines whether the imported assertion is the only element of the envelope its
        ///     identifier resolves to, by utility id or unqualified id attribute, and that
        ///     <c>SignedXml</c> resolves it too.
        /// </summary>
        /// <param name="imported">The assertion as imported into the envelope.</param>
        /// <param name="id">The assertion's identifier.</param>
        /// <returns>
        ///     True when the identifier resolves to the assertion alone.
        /// </returns>
        private bool IsUniquelyAddressable(XmlElement imported, string id)
        {
            if (WsuIdLookup.FindById(_document, id).Count + CountUnqualifiedIdBearers(id) != 1)
                return false;

            try
            {
                return ReferenceEquals(new WsuSignedXml(_document).GetIdElement(_document, id), imported);
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        /// <summary>
        ///     Counts the elements of the envelope carrying the supplied value in one of the
        ///     unqualified attributes a reference resolves by.
        /// </summary>
        /// <param name="id">The id to match, ordinally.</param>
        /// <returns>
        ///     The number of elements answering to the id.
        /// </returns>
        private int CountUnqualifiedIdBearers(string id)
        {
            var bearers = 0;

            foreach (var candidate in _document.GetElementsByTagName("*"))
            {
                if (candidate is not XmlElement element)
                    continue;

                foreach (var attributeName in UnqualifiedIdAttributeNames)
                {
                    if (element.HasAttribute(attributeName) && string.Equals(element.GetAttribute(attributeName), id, StringComparison.Ordinal))
                    {
                        bearers++;

                        break;
                    }
                }
            }

            return bearers;
        }

        /// <summary>
        ///     Builds the refusal of the assertion under a validation code, quoting nothing of the
        ///     assertion.
        /// </summary>
        /// <param name="code">The validation message code.</param>
        /// <returns>
        ///     A failed IResult.
        /// </returns>
        private static IResult SamlRefusal(MessageCodes code)
            => Result.Failure(code.GetDescription(), Messages.GetValidationMessage(code));
    }
}
