// ***********************************************************************
//  Assembly          : RzR.Shared.Services.SoapClientCallAssistTests
//  Author            : RzR
//  Created On        : 2026-09-02 21:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-09-02 21:45
//  ***********************************************************************
//  <copyright file="SoapMemberIsValid.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Dto.SoapMembers
{
    [SoapContract(Namespace = "http://SoapClientCallAssist.local/")]
    public class SoapMemberIsValid
    {
        [SoapMember]
        public string Id { get; set; }
    }
}