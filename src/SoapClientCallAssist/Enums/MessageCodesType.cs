// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssist
//  Author           : RzR
//  Created On       : 2024-09-15 17:05
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-15 18:41
// ***********************************************************************
//  <copyright file="MessageCodesType.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System.ComponentModel;

#endregion

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace SoapClientCallAssist.Enums
{
    internal enum MessageCodesType
    {
        #region ERROR CODES

        [Description("ER_DI_RSCE_001")]
        ER_DI_RSCE_001,

        [Description("ER-S11-BSR")]
        ER_S11_BSR,

        [Description("ER-S11-BSRA")] 
        ER_S11_BSRA,

        [Description("ER-S12-BSR")] 
        ER_S12_BSR,

        [Description("ER-S12-BSRA")]
        ER_S12_BSRA,

        [Description("ER-S11-SR")]
        ER_S11_SR,

        [Description("ER-S11-SRA")] 
        ER_S11_SRA,

        [Description("ER-S12-SR")] 
        ER_S12_SR,

        [Description("ER-S12-SRA")]
        ER_S12_SRA,

        [Description("ER-BEC-BSRM")]
        ER_BEC_BSRM,

        [Description("ER-BEC-BSRM-SR")] 
        ER_BEC_BSRM_SR,

        [Description("ER-BEC-BSRM-SRA")] 
        ER_BEC_BSRM_SRA,

        [Description("ER-BEC-VR")]
        ER_BEC_VR,

        [Description("ER-BEC-GRB-01")]
        ER_BEC_GRB_01,

        [Description("ER-BEC-GRB-02")]
        ER_BEC_GRB_02,

        [Description("ER-BEC-GRB-03")]
        ER_BEC_GRB_03,

        [Description("ER-BEC-CBFFC")]
        ER_BEC_CBFFC,

        #endregion

        #region VALIDATEION CODES

        [Description("V-BEC-VR-001")] 
        V_BEC_VR_001,

        [Description("V_BEC_VR_002")] 
        V_BEC_VR_002,

        [Description("V_BEC_VR_003")] 
        V_BEC_VR_003,

        [Description("V_BEC_VR_004")] 
        V_BEC_VR_004,

        [Description("V_BEC_VR_005")] 
        V_BEC_VR_005,

        [Description("V_BEC_VR_006")]
        V_BEC_VR_006,

        [Description("V_BEC_VR_007")] 
        V_BEC_VR_007

        #endregion
    }
}