// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssistTests
//  Author            : RzR
//  Created On        : 2026-09-02 21:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-02 21:30
//  ***********************************************************************
//  <copyright file="SoapMemberProductDetail.cs" company="RzR SOFT & TECH">
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
    [SoapContract]
    public class SoapMemberProductDetail
    {
        [SoapMember] 
        public int PartnerId { get; set; }

        [SoapMember] 
        public int ManufacturerId { get; set; }

        [SoapMember]
        public int SupplierId { get; set; }
    }
}