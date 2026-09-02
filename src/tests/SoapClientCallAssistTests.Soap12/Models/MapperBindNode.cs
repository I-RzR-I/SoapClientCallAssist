
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Node", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindNode
{
    [SoapMember(Name = "Name", Order = 0)]
    public string Name { get; set; }

    [SoapMember(Name = "Child", Order = 1)]
    public MapperBindNode Child { get; set; }
}
