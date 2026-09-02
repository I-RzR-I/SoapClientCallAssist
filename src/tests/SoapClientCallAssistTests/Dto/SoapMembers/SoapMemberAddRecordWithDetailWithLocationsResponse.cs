// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssistTests
//  Author            : RzR
//  Created On        : 2026-09-02 23:07
//
//  Last Modified By : RzR
//  Last Modified On : 2026-09-02 23:07
//  ***********************************************************************
//  <copyright file="SoapMemberAddRecordWithDetailWithLocationsResponse.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using SoapClientCallAssist.Attributes;

#endregion

namespace SoapClientCallAssistTests.Dto.SoapMembers
{
    [SoapContract(Name = "AddRecordWithDetailWithLocationsResponse", Namespace = "http://SoapClientCallAssist.local/")]
    public class SoapMemberAddRecordWithDetailWithLocationsResponse
    {
        [SoapMember(Name = "AddRecordWithDetailWithLocationsResult")]
        public bool Result { get; set; }
    }
}
