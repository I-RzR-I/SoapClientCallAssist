// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssist
//  Author           : RzR
//  Created On       : 2024-09-15 16:59
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-15 18:43
// ***********************************************************************
//  <copyright file="DefaultResultMessageHelper.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using SoapClientCallAssist.Enums;
using System.Collections.Generic;

#endregion

namespace SoapClientCallAssist.Helper
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A default result message helper.
    /// </summary>
    /// =================================================================================================
    internal static class DefaultResultMessageHelper
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the error messages.
        /// </summary>
        /// =================================================================================================
        internal static readonly Dictionary<MessageCodesType, string> ErrorMessages
            = new Dictionary<MessageCodesType, string>
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
                { MessageCodesType.ER_BEC_CBFFC, "An error occurred while trying to check response for fault code." }
            };

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the validation messages.
        /// </summary>
        /// =================================================================================================
        internal static readonly Dictionary<MessageCodesType, string> ValidationMessages
            = new Dictionary<MessageCodesType, string>
            {
                { MessageCodesType.V_BEC_VR_001, "SOAP request object can not be null." },
                { MessageCodesType.V_BEC_VR_002, "The supplied HTTP method ({0}) is not allowed." },
                { MessageCodesType.V_BEC_VR_003, "SOAP Uri is mandatory." },
                { MessageCodesType.V_BEC_VR_004, "SOAP protocol version is mandatory." },
                { MessageCodesType.V_BEC_VR_005, "SOAP namespace is mandatory." },
                { MessageCodesType.V_BEC_VR_006, "SOAP media type is mandatory." },
                { MessageCodesType.V_BEC_VR_007, "SOAP body encoding is mandatory." }
            };
    }
}