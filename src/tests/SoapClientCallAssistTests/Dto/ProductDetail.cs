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