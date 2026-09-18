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