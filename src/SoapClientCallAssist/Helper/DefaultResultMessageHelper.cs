// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-15 16:59
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="DefaultResultMessageHelper.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Enums;
using System.Collections.Generic;

#endregion

namespace SoapClientCallAssist.Helper
{
    /// <summary>
    ///     A default result message helper.
    /// </summary>
    internal static class DefaultResultMessageHelper
    {
        /// <summary>
        ///     (Immutable) the error messages.
        /// </summary>
        internal static readonly Dictionary<MessageCodesType, string> ErrorMessages
            = new()
            {
                { MessageCodesType.ER_DI_RSCE_001, "No implementation for endpoint type: {0}" },
                { MessageCodesType.ER_S11_BSR, "An error occurred while trying to build a SOAP 1.1 request." },
                { MessageCodesType.ER_S11_BSRA, "An error occurred while trying to build a SOAP 1.1 request async." },
                { MessageCodesType.ER_S12_BSR, "An error occurred while trying to build a SOAP 1.2 request." },
                { MessageCodesType.ER_S12_BSRA, "An error occurred while trying to build a SOAP 1.2 request async." },
                { MessageCodesType.ER_S11_SR, "An error occurred while trying to execute/send a SOAP 1.1 request." },
                { MessageCodesType.ER_S11_SRA, "An error occurred while trying to execute/send a SOAP 1.1 request async." },
                { MessageCodesType.ER_S12_SR, "An error occurred while trying to execute/send a SOAP 1.2 request." },
                { MessageCodesType.ER_S12_SRA, "An error occurred while trying to execute/send a SOAP 1.2 request async." },
                { MessageCodesType.ER_BEC_BSRM, "An error occurred while trying to validate and build SOAP request message." },
                { MessageCodesType.ER_BEC_BSRM_SR, "An error occurred while trying to send SOAP request message." },
                { MessageCodesType.ER_BEC_BSRM_SRA, "An error occurred while trying to send SOAP request message async." },
                { MessageCodesType.ER_BEC_VR, "An error occurred while trying to validate input parameters for SOAP request." },
                { MessageCodesType.ER_BEC_GRB_01, "No or more than one SOAP Body in response." },
                { MessageCodesType.ER_BEC_GRB_02, "No child in SOAP Body." },
                { MessageCodesType.ER_BEC_GRB_03, "Invalid SOAP Message in response." },
                { MessageCodesType.ER_BEC_CBFFC, "An error occurred while trying to check response for fault code." },
                { MessageCodesType.ER_MAP_EMT, "An error occurred while emitting XML for member '{0}' on type '{1}'." },
                { MessageCodesType.ER_MAP_BND, "An error occurred while binding response element '{0}' to '{1}.{2}'." },
                { MessageCodesType.ER_MAP_MTD, "An error occurred while reading SOAP mapping metadata for type '{0}'." },
                { MessageCodesType.ER_MAP_RSP, "The SOAP response could not be read into '{0}' ({1})." },
                { MessageCodesType.ER_MAP_FLT, "The SOAP response carries a SOAP fault; nothing was bound to '{0}'." }
            };

        /// <summary>
        ///     (Immutable) the validation messages.
        /// </summary>
        internal static readonly Dictionary<MessageCodesType, string> ValidationMessages
            = new()
            {
                { MessageCodesType.V_BEC_VR_001, "SOAP request object can not be null." },
                { MessageCodesType.V_BEC_VR_002, "The supplied HTTP method ({0}) is not allowed." },
                { MessageCodesType.V_BEC_VR_003, "SOAP Uri is mandatory." },
                { MessageCodesType.V_BEC_VR_004, "SOAP protocol version is mandatory." },
                { MessageCodesType.V_BEC_VR_005, "SOAP namespace is mandatory." },
                { MessageCodesType.V_BEC_VR_006, "SOAP media type is mandatory." },
                { MessageCodesType.V_BEC_VR_007, "SOAP body encoding is mandatory." },
                { MessageCodesType.V_MAP_001, "Type '{0}' declares no mapped members." },
                { MessageCodesType.V_MAP_002, "Unsupported member type '{0}' for member '{1}'." },
                { MessageCodesType.V_MAP_003, "Duplicate wire name '{0}' declared more than once on type '{1}'." },
                { MessageCodesType.V_MAP_004, "Member '{0}' resolves to an element with no namespace; every mapped element must be namespace-qualified." },
                { MessageCodesType.V_MAP_005, "Element '{0}' is nil but member '{1}' is a non-nullable value type." },
                { MessageCodesType.V_MAP_006, "Type '{0}' mixes [SoapMember] and [DataMember]; use one convention per type." },
                { MessageCodesType.V_MAP_007, "Type graph for '{0}' exceeds the maximum supported nesting depth of {1}." },
                { MessageCodesType.V_MAP_008, "Type '{0}' cannot be instantiated; a public parameterless constructor is required." },
                { MessageCodesType.V_MAP_009, "No SOAP Body element was found in the response, or more than one was present." },
                { MessageCodesType.V_MAP_010, "The SOAP Body of the response holds no element to bind '{0}' from." }
            };

        /// <summary>
        ///     Gets the error message of the supplied code without throwing on an unmapped code. Callers
        ///     that are contracted never to throw must resolve messages through this method rather than
        ///     indexing <see cref="ErrorMessages" /> directly.
        /// </summary>
        /// <param name="code">The message code.</param>
        /// <returns>
        ///     The message text, or a placeholder naming the unmapped code.
        /// </returns>
        internal static string GetErrorMessage(MessageCodesType code)
            => ErrorMessages.TryGetValue(code, out var message) ? message : UnmappedMessage(code);

        /// <summary>
        ///     Gets the validation message of the supplied code without throwing on an unmapped code.
        /// </summary>
        /// <param name="code">The message code.</param>
        /// <returns>
        ///     The message text, or a placeholder naming the unmapped code.
        /// </returns>
        internal static string GetValidationMessage(MessageCodesType code)
            => ValidationMessages.TryGetValue(code, out var message) ? message : UnmappedMessage(code);

        /// <summary>
        ///     Builds the placeholder returned for a code that has no message text.
        /// </summary>
        /// <param name="code">The unmapped message code.</param>
        /// <returns>
        ///     The placeholder message.
        /// </returns>
        private static string UnmappedMessage(MessageCodesType code)
            => $"No message is defined for code '{code}'.";
    }
}