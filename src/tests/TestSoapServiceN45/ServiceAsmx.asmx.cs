using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using DomainCommonExtensions.CommonExtensions;
using DomainCommonExtensions.DataTypeExtensions;
using TestSoapServiceN45.Dto;

namespace TestSoapServiceN45
{
    [WebService(Namespace = "http://SoapClientCallAssist.local/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]
    public class ServiceAsmx : System.Web.Services.WebService
    {
        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        [ScriptMethod]
        [WebMethod]
        public int IsValid(string id)
        {
            if (string.IsNullOrEmpty(id))
                return -1;
            return 1;

        }

        [ScriptMethod]
        [WebMethod]
        public bool AddRecordWithDetail(Product product)
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

        [ScriptMethod]
        [WebMethod]
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