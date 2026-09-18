using SoapClientCallAssistTests.Wcf.Service.Common;
using System.ServiceModel;

namespace SoapClientCallAssistTests.Wcf.Service.SecureConversation;

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
public sealed class SecureConversationProbeService : ISecureConversationProbeService
{

    public string Echo(string value) => value;

    public WcfCallerIdentity WhoAmI() => ProbeCallerIdentity.Current();
}
