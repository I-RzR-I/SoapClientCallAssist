using System;
using System.Collections.Generic;
using System.Diagnostics;
using DomainCommonExtensions.CommonExtensions;
using DomainCommonExtensions.DataTypeExtensions;
using TestSoapServiceN45.Dto;

namespace TestSoapServiceN45
{

    public class ServiceSvc : IServiceSvc
    {
        public bool DoWork()
        {
            return true;
        }

        public string HelloWorld()
        {
            return "Hello World";
        }

        public int IsValid(string id)
        {
            if (string.IsNullOrEmpty(id))
                return -1;
            return 1;

        }

        public bool AddRecordWithDetail(Product product)
        {
            Debug.WriteLine($"product.IsNull()? {product.IsNull()}");
            Debug.WriteLine($"product> {product.SerializeToString()}");
            if (product.IsNull()) throw new ArgumentNullException(nameof(product));
            if (product.Id.IsNullOrZero()) throw new ArgumentNullException(nameof(product.Id));
            if (product.Code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(product.Code));
            if (product.Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(product.Name));
            if (product.IsActive.IsNull()) throw new ArgumentNullException(nameof(product.IsActive));

            if (product.Detail.IsNull()) throw new ArgumentNullException(nameof(product.Detail));
            if (product.Detail.ManufacturerId.IsNull()) throw new ArgumentNullException(nameof(product.Detail.ManufacturerId));
            if (product.Detail.SupplierId.IsNull()) throw new ArgumentNullException(nameof(product.Detail.SupplierId));
            if (product.Detail.PartnerId.IsNull()) throw new ArgumentNullException(nameof(product.Detail.PartnerId));

            return true;
        }

        public bool AddRecordWithDetailWithLocations(Product product, List<int> associatedLocationIds)
        {
            if (product.IsNull()) throw new ArgumentNullException(nameof(product));
            if (product.Id.IsNullOrZero()) throw new ArgumentNullException(nameof(product.Id));
            if (product.Code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(product.Code));
            if (product.Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(product.Name));
            if (product.IsActive.IsNull()) throw new ArgumentNullException(nameof(product.IsActive));

            if (product.Detail.IsNull()) throw new ArgumentNullException(nameof(product.Detail));
            if (product.Detail.ManufacturerId.IsNull()) throw new ArgumentNullException(nameof(product.Detail.ManufacturerId));
            if (product.Detail.SupplierId.IsNull()) throw new ArgumentNullException(nameof(product.Detail.SupplierId));
            if (product.Detail.PartnerId.IsNull()) throw new ArgumentNullException(nameof(product.Detail.PartnerId));

            return true;
        }
    }
}
