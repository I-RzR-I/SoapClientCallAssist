#region U S I N G

using SoapClientCallAssist.Attributes;

#endregion

namespace SoapClientCallAssistTests.Dto.SoapMembers
{
    [SoapContract(Name = "IsValidResponse", Namespace = "http://SoapClientCallAssist.local/")]
    public class SoapMemberIsValidResponse
    {
        [SoapMember(Name = "IsValidResult")]
        public int Result { get; set; }
    }
}
