using SoapClientCallAssistTests.Wcf.Service.Common;
using System.ServiceModel;

namespace SoapClientCallAssistTests.Wcf.Service.Symmetric;

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
public sealed class SymmetricProbeService : ISymmetricProbeService
{

    public string Echo(string value) => value;

    public WcfCallerIdentity WhoAmI() => ProbeCallerIdentity.Current();
}
