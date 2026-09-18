using System.Runtime.Serialization;

namespace SoapClientCallAssistTests.Wcf.Service.Common;

[DataContract(Namespace = WcfProbeContract.Namespace)]
public sealed class WcfCallerIdentity
{

    [DataMember(Order = 0)]
    public string AuthenticationType { get; set; } = WcfProbeContract.AnonymousAuthenticationType;

    [DataMember(Order = 1)]
    public string Name { get; set; } = string.Empty;
}
