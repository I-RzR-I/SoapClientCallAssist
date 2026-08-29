// ***********************************************************************
//  Assembly         : TestSoapServiceN45.TestSoapServiceN45
//  Author           : RzR
//  Created On       : 2024-09-16 14:39
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-16 14:39
// ***********************************************************************
//  <copyright file="Product.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using System.Runtime.Serialization;

namespace TestSoapServiceN45.Dto
{
    [DataContract]
    public class Product
    {
        [DataMember]
        public int? Id { get; set; }

        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool? IsActive { get; set; }

        [DataMember]
        public ProductDetail Detail { get; set; }
    }
}