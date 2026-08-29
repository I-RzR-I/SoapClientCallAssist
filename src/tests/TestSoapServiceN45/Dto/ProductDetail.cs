// ***********************************************************************
//  Assembly         : TestSoapServiceN45.TestSoapServiceN45
//  Author           : RzR
//  Created On       : 2024-09-16 14:39
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-16 14:39
// ***********************************************************************
//  <copyright file="ProductDetail.cs" company="">
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
    public class ProductDetail
    {
        [DataMember]
        public int PartnerId { get; set; }

        [DataMember]
        public int ManufacturerId { get; set; }

        [DataMember]
        public int SupplierId { get; set; }
    }
}