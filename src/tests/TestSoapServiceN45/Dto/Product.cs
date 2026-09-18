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