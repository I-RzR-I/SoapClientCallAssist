#region U S I N G

using SoapClientCallAssist.Attributes;

#endregion

namespace SoapClientCallAssistTests.Dto.SoapMembers
{
    [SoapContract(Name = "product", Namespace = "http://SoapClientCallAssist.local/")]
    public class SoapMemberProduct
    {
        [SoapMember]
        public int? Id { get; set; }

        [SoapMember]
        public string Code { get; set; }

        [SoapMember] 
        public string Name { get; set; }

        [SoapMember] 
        public bool IsActive { get; set; }

        [SoapMember] 
        public SoapMemberProductDetail Detail { get; set; }
    }
}