// ***********************************************************************
//  Assembly         : RzR.Shared.Services.SoapClientCallAssistTests
//  Author           : RzR
//  Created On       : 2024-09-17 21:27
// 
//  Last Modified By : RzR
//  Last Modified On : 2024-09-17 21:27
// ***********************************************************************
//  <copyright file="ProductDetail.cs" company="">
//   Copyright (c) RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using System.Runtime.Serialization;

namespace SoapClientCallAssistTests.Dto
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