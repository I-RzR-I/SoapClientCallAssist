using System.Runtime.Serialization;

namespace SoapClientCallAssistTests.Wcf.Service.Saml;

[DataContract(Namespace = SamlProbeContract.Namespace)]
public sealed class SamlCallerClaims
{

    [DataMember(Order = 0)]
    public string AuthenticationType { get; set; } = string.Empty;

    [DataMember(Order = 1)]
    public string Name { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Role { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Issuer { get; set; } = string.Empty;
}
