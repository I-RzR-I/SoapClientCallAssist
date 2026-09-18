#region U S I N G

using SoapClientCallAssist.Attributes;

#endregion

namespace SoapClientCallAssistTests.Dto.SoapMembers
{
    [SoapContract(Name = "HelloWorldResponse", Namespace = "http://SoapClientCallAssist.local/")]
    public class SoapMemberHelloWorldResponse
    {
        [SoapMember(Name = "HelloWorldResult")]
        public string Result { get; set; }
    }
}
