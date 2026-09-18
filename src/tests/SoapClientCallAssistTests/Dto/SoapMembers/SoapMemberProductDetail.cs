#region U S I N G

using SoapClientCallAssist.Attributes;

#endregion

namespace SoapClientCallAssistTests.Dto.SoapMembers
{
    [SoapContract]
    public class SoapMemberProductDetail
    {
        [SoapMember] 
        public int PartnerId { get; set; }

        [SoapMember] 
        public int ManufacturerId { get; set; }

        [SoapMember]
        public int SupplierId { get; set; }
    }
}