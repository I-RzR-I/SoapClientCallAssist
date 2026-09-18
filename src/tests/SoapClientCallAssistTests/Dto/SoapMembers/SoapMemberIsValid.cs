using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Dto.SoapMembers
{
    [SoapContract(Namespace = "http://SoapClientCallAssist.local/")]
    public class SoapMemberIsValid
    {
        [SoapMember]
        public string Id { get; set; }
    }
}