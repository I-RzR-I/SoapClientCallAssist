// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssistTests
//  Author           : RzR
//  Created On       : 2024-09-17 21:26
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-17 21:26
// ***********************************************************************
//  <copyright file="Product.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using System.Runtime.Serialization;

namespace SoapClientCallAssistTests.Dto
{
    [DataContract(Name = "product", Namespace = "http://SoapClientCallAssist.local/")]
    public class Product
    {
        [DataMember]
        public int? Id { get; set; }

        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool IsActive { get; set; }

        [DataMember]
        public ProductDetail Detail { get; set; }
    }
}