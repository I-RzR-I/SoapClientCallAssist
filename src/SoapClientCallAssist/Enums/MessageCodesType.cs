// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-15 17:05
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-08-31 20:42
//  ***********************************************************************
//  <copyright file="MessageCodesType.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

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

        [Description("ER_DI_RSCE_001")] ER_DI_RSCE_001,

        [Description("ER-S11-BSR")] ER_S11_BSR,

        [Description("ER-S11-BSRA")] ER_S11_BSRA,

        [Description("ER-S12-BSR")] ER_S12_BSR,

        [Description("ER-S12-BSRA")] ER_S12_BSRA,

        [Description("ER-S11-SR")] ER_S11_SR,

        [Description("ER-S11-SRA")] ER_S11_SRA,

        [Description("ER-S12-SR")] ER_S12_SR,

        [Description("ER-S12-SRA")] ER_S12_SRA,

        [Description("ER-BEC-BSRM")] ER_BEC_BSRM,

        [Description("ER-BEC-BSRM-SR")] ER_BEC_BSRM_SR,

        [Description("ER-BEC-BSRM-SRA")] ER_BEC_BSRM_SRA,

        [Description("ER-BEC-VR")] ER_BEC_VR,

        [Description("ER-BEC-GRB-01")] ER_BEC_GRB_01,

        [Description("ER-BEC-GRB-02")] ER_BEC_GRB_02,

        [Description("ER-BEC-GRB-03")] ER_BEC_GRB_03,

        [Description("ER-BEC-CBFFC")] ER_BEC_CBFFC,

        [Description("ER-MAP-EMT")] ER_MAP_EMT,

        [Description("ER-MAP-BND")] ER_MAP_BND,

        [Description("ER-MAP-MTD")] ER_MAP_MTD,

        [Description("ER-MAP-RSP")] ER_MAP_RSP,

        [Description("ER-MAP-FLT")] ER_MAP_FLT,

        #endregion

        #region VALIDATEION CODES

        [Description("V-BEC-VR-001")] V_BEC_VR_001,

        [Description("V_BEC_VR_002")] V_BEC_VR_002,

        [Description("V_BEC_VR_003")] V_BEC_VR_003,

        [Description("V_BEC_VR_004")] V_BEC_VR_004,

        [Description("V_BEC_VR_005")] V_BEC_VR_005,

        [Description("V_BEC_VR_006")] V_BEC_VR_006,

        [Description("V_BEC_VR_007")] V_BEC_VR_007,

        [Description("V-MAP-001")] V_MAP_001,

        [Description("V-MAP-002")] V_MAP_002,

        [Description("V-MAP-003")] V_MAP_003,

        [Description("V-MAP-004")] V_MAP_004,

        [Description("V-MAP-005")] V_MAP_005,

        [Description("V-MAP-006")] V_MAP_006,

        [Description("V-MAP-007")] V_MAP_007,

        [Description("V-MAP-008")] V_MAP_008,

        [Description("V-MAP-009")] V_MAP_009,

        [Description("V-MAP-010")] V_MAP_010

        #endregion
    }
}