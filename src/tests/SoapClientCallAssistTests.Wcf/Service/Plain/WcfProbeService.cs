using SoapClientCallAssistTests.Wcf.Service.Common;
using System.ServiceModel;

namespace SoapClientCallAssistTests.Wcf.Service.Plain;

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
public sealed class WcfProbeService : IWcfProbeService
{

    public string Echo(string value) => value;

    public WcfCallerIdentity WhoAmI() => ProbeCallerIdentity.Current();
}
