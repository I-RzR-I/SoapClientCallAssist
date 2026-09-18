using SoapClientCallAssistTests.Wcf.Service.Common;
using System.ServiceModel;

namespace SoapClientCallAssistTests.Wcf.Service.SecureConversation;

[ServiceContract(Namespace = SecureConversationProbeContract.Namespace)]
public interface ISecureConversationProbeService
{

    [OperationContract]
    string Echo(string value);

    [OperationContract]
    WcfCallerIdentity WhoAmI();
}
