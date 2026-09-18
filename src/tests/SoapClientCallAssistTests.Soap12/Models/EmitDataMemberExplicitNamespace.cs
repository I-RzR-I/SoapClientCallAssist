using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using System.Runtime.Serialization;

namespace SoapClientCallAssistTests.Soap12.Models;

[DataContract(Name = "explicitContract", Namespace = MapperEmitNs.Explicit)]
internal sealed class EmitDataMemberExplicitNamespace
{
    [DataMember(Name = "gamma", Order = 1)]
    public string Gamma { get; set; } = string.Empty;

    [DataMember(Name = "delta", Order = 2)]
    public string Delta { get; set; } = string.Empty;
}
