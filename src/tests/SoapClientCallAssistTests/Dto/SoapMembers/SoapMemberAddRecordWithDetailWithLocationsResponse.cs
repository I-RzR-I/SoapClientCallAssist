#region U S I N G

using SoapClientCallAssist.Attributes;

#endregion

namespace SoapClientCallAssistTests.Dto.SoapMembers
{
    [SoapContract(Name = "AddRecordWithDetailWithLocationsResponse", Namespace = "http://SoapClientCallAssist.local/")]
    public class SoapMemberAddRecordWithDetailWithLocationsResponse
    {
        [SoapMember(Name = "AddRecordWithDetailWithLocationsResult")]
        public bool Result { get; set; }
    }
}
