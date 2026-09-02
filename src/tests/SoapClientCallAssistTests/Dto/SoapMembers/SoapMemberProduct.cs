// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssistTests
//  Author            : RzR
//  Created On        : 2026-09-02 21:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-02 21:30
//  ***********************************************************************
//  <copyright file="SoapMemberProduct.cs" company="RzR SOFT & TECH">
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
    [SoapContract(Name = "product", Namespace = "http://SoapClientCallAssist.local/")]
    public class SoapMemberProduct
    {
        [SoapMember]
        public int? Id { get; set; }

        [SoapMember]
        public string Code { get; set; }

        [SoapMember] 
        public string Name { get; set; }

        [SoapMember] 
        public bool IsActive { get; set; }

        [SoapMember] 
        public SoapMemberProductDetail Detail { get; set; }
    }
}