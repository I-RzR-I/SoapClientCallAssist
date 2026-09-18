using SoapClientCallAssistTests.Wcf.Service.Common;
using System.ServiceModel;

namespace SoapClientCallAssistTests.Wcf.Service.TierB;

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
public sealed class TierBProbeService : ITierBProbeService
{

    public string Echo(string value) => value;

    public WcfCallerIdentity WhoAmI() => ProbeCallerIdentity.Current();
}
