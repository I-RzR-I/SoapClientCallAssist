using System.Runtime.Serialization;

namespace TestSoapServiceN45.Dto
{
    [DataContract(Namespace = "http://SoapClientCallAssist.local/")]
    public sealed class CallerIdentity
    {
        [DataMember(Order = 0)]
        public string AuthenticationType { get; set; }

        [DataMember(Order = 1)]
        public string Name { get; set; }

        [DataMember(Order = 2)]
        public string CertificateThumbprint { get; set; }
    }
}
