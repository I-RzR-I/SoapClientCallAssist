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