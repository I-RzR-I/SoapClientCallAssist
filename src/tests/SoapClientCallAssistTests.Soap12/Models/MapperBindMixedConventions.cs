
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers;
using System.Runtime.Serialization;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Mixed", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindMixedConventions
{
    [SoapMember(Name = "A", Order = 0)]
    public string A { get; set; }

    [DataMember(Name = "B", Order = 1)]
    public string B { get; set; }
}
