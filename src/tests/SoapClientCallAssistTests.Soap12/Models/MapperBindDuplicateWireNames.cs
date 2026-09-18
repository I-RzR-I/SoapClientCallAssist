
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Duplicated", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindDuplicateWireNames
{
    [SoapMember(Name = "Same", Order = 0)]
    public string First { get; set; }

    [SoapMember(Name = "Same", Order = 1)]
    public string Second { get; set; }
}
