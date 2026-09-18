#region U S I N G

using SoapClientCallAssist.Attributes;

#endregion

namespace SoapClientCallAssistTests.Dto.SoapMembers
{
    [SoapContract(Name = "AddRecordWithDetailResponse", Namespace = "http://SoapClientCallAssist.local/")]
    public class SoapMemberAddRecordWithDetailResponse
    {
        [SoapMember(Name = "AddRecordWithDetailResult")]
        public bool Result { get; set; }
    }
}
