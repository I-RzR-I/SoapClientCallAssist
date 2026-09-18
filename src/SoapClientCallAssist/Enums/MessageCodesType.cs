// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssist
//  Author            : RzR
//  Created On        : 2024-09-15 17:05
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-12 09:30
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

        [Description("ER-BEC-FLT")] ER_BEC_FLT,

        [Description("ER-BEC-VRS")] ER_BEC_VRS,

        [Description("ER-BEC-HTTP-3XX")] ER_BEC_HTTP_3XX,

        [Description("ER-BEC-HTTP-401")] ER_BEC_HTTP_401,

        [Description("ER-BEC-HTTP-403")] ER_BEC_HTTP_403,

        [Description("ER-BEC-HTTP-4XX")] ER_BEC_HTTP_4XX,

        [Description("ER-BEC-HTTP-FLT")] ER_BEC_HTTP_FLT,

        [Description("ER-BEC-HTTP-5XX")] ER_BEC_HTTP_5XX,

        [Description("ER-BEC-HTTP-STS")] ER_BEC_HTTP_STS,

        [Description("ER-MAP-EMT")] ER_MAP_EMT,

        [Description("ER-MAP-BND")] ER_MAP_BND,

        [Description("ER-MAP-MTD")] ER_MAP_MTD,

        [Description("ER-MAP-RSP")] ER_MAP_RSP,

        [Description("ER-MAP-FLT")] ER_MAP_FLT,

        [Description("ER-SEC-SGN")] ER_SEC_SGN,

        [Description("ER-SEC-C14N")] ER_SEC_C14N,

        [Description("ER-SEC-KEY")] ER_SEC_KEY,

        [Description("ER-SEC-DOM")] ER_SEC_DOM,

        [Description("ER-SEC-VER")] ER_SEC_VER,

        [Description("ER-XML-DEPTH")] ER_XML_DEPTH,

        #region ERROR CODES - WS-SECURITY DECRYPTION

        [Description("ER-SEC-DEC")] ER_SEC_DEC,

        #endregion

        #endregion

        #region VALIDATEION CODES

        [Description("V-BEC-VR-001")] V_BEC_VR_001,

        [Description("V_BEC_VR_002")] V_BEC_VR_002,

        [Description("V_BEC_VR_003")] V_BEC_VR_003,

        [Description("V_BEC_VR_004")] V_BEC_VR_004,

        [Description("V_BEC_VR_005")] V_BEC_VR_005,

        [Description("V_BEC_VR_006")] V_BEC_VR_006,

        [Description("V_BEC_VR_007")] V_BEC_VR_007,

        [Description("V-BEC-HDR-001")] V_BEC_HDR_001,

        [Description("V-BEC-TMO-001")] V_BEC_TMO_001,

        [Description("V-MAP-001")] V_MAP_001,

        [Description("V-MAP-002")] V_MAP_002,

        [Description("V-MAP-003")] V_MAP_003,

        [Description("V-MAP-004")] V_MAP_004,

        [Description("V-MAP-005")] V_MAP_005,

        [Description("V-MAP-006")] V_MAP_006,

        [Description("V-MAP-007")] V_MAP_007,

        [Description("V-MAP-008")] V_MAP_008,

        [Description("V-MAP-009")] V_MAP_009,

        [Description("V-MAP-010")] V_MAP_010,

        [Description("V-MAP-011")] V_MAP_011,

        [Description("V-MAP-012")] V_MAP_012,

        [Description("V-SEC-001")] V_SEC_001,

        [Description("V-SEC-002")] V_SEC_002,

        [Description("V-SEC-003")] V_SEC_003,

        [Description("V-SEC-004")] V_SEC_004,

        [Description("V-SEC-005")] V_SEC_005,

        [Description("V-SEC-006")] V_SEC_006,

        [Description("V-SEC-007")] V_SEC_007,

        [Description("V-SEC-008")] V_SEC_008,

        [Description("V-SEC-009")] V_SEC_009,

        [Description("V-SEC-010")] V_SEC_010,

        [Description("V-SEC-011")] V_SEC_011,

        [Description("V-SEC-012")] V_SEC_012,

        [Description("V-SEC-013")] V_SEC_013,

        [Description("V-SEC-014")] V_SEC_014,

        [Description("V-SEC-015")] V_SEC_015,

        [Description("V-SEC-016")] V_SEC_016,

        [Description("V-SEC-017")] V_SEC_017,

        #region VALIDATION CODES - WS-ADDRESSING (V-SEC-020..029)

        [Description("V-SEC-020")] V_SEC_020,

        [Description("V-SEC-021")] V_SEC_021,

        [Description("V-SEC-022")] V_SEC_022,

        [Description("V-SEC-023")] V_SEC_023,

        [Description("V-SEC-024")] V_SEC_024,

        #endregion

        #region VALIDATION CODES - SYMMETRIC BINDING (V-SEC-030..049)

        [Description("V-SEC-030")] V_SEC_030,

        [Description("V-SEC-031")] V_SEC_031,

        [Description("V-SEC-032")] V_SEC_032,

        [Description("V-SEC-033")] V_SEC_033,

        [Description("V-SEC-034")] V_SEC_034,

        [Description("V-SEC-035")] V_SEC_035,

        [Description("V-SEC-036")] V_SEC_036,

        [Description("V-SEC-037")] V_SEC_037,

        [Description("V-SEC-038")] V_SEC_038,

        [Description("V-SEC-039")] V_SEC_039,

        [Description("V-SEC-040")] V_SEC_040,

        [Description("V-SEC-041")] V_SEC_041,

        [Description("V-SEC-042")] V_SEC_042,

        [Description("V-SEC-043")] V_SEC_043,

        [Description("V-SEC-044")] V_SEC_044,

        [Description("V-SEC-045")] V_SEC_045,

        [Description("V-SEC-046")] V_SEC_046,

        [Description("V-SEC-047")] V_SEC_047,

        #endregion

        #region VALIDATION CODES - ENCRYPTION AND DECRYPTION (V-SEC-050..059)

        [Description("V-SEC-050")] V_SEC_050,

        [Description("V-SEC-051")] V_SEC_051,

        [Description("V-SEC-052")] V_SEC_052,

        [Description("V-SEC-053")] V_SEC_053,

        [Description("V-SEC-054")] V_SEC_054,

        [Description("V-SEC-055")] V_SEC_055,

        [Description("V-SEC-056")] V_SEC_056,

        [Description("V-SEC-057")] V_SEC_057,

        [Description("V-SEC-058")] V_SEC_058,

        [Description("V-SEC-059")] V_SEC_059,

        #endregion

        #region VALIDATION CODES - SECURE CONVERSATION (V-SEC-060..079)

        [Description("V-SEC-060")] V_SEC_060,

        [Description("V-SEC-061")] V_SEC_061,

        [Description("V-SEC-062")] V_SEC_062,

        [Description("V-SEC-063")] V_SEC_063,

        [Description("V-SEC-064")] V_SEC_064,

        [Description("V-SEC-065")] V_SEC_065,

        [Description("V-SEC-066")] V_SEC_066,

        [Description("V-SEC-067")] V_SEC_067,

        [Description("V-SEC-068")] V_SEC_068,

        [Description("V-SEC-069")] V_SEC_069,

        [Description("V-SEC-070")] V_SEC_070,

        [Description("V-SEC-071")] V_SEC_071,

        [Description("V-SEC-072")] V_SEC_072,

        [Description("V-SEC-073")] V_SEC_073,

        [Description("V-SEC-074")] V_SEC_074,

        [Description("V-SEC-075")] V_SEC_075,

        #endregion

        #region VALIDATION CODES - SAML (V-SEC-080..089)

        [Description("V-SEC-080")] V_SEC_080,

        [Description("V-SEC-081")] V_SEC_081,

        [Description("V-SEC-082")] V_SEC_082,

        [Description("V-SEC-083")] V_SEC_083,

        [Description("V-SEC-084")] V_SEC_084,

        [Description("V-SEC-085")] V_SEC_085,

        [Description("V-SEC-086")] V_SEC_086,

        [Description("V-SEC-087")] V_SEC_087,

        [Description("V-SEC-088")] V_SEC_088,

        #endregion

        #region VALIDATION CODES - SECURITY PLANNER (V-SEC-090..099)

        [Description("V-SEC-090")] V_SEC_090,

        [Description("V-SEC-091")] V_SEC_091,

        [Description("V-SEC-092")] V_SEC_092,

        [Description("V-SEC-093")] V_SEC_093,

        [Description("V-SEC-094")] V_SEC_094,

        #endregion

        #endregion
    }
}