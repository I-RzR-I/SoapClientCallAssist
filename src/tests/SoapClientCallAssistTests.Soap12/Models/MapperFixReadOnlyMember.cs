
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "ReadOnly", Namespace = MapperBindNs.Contract)]
public sealed class MapperFixReadOnlyMember
{
    [SoapMember(Name = "Name", Order = 0)]
    public string Name { get; set; }

    [SoapMember(Name = "Computed", Order = 1)]
    public string Computed => "computed";
}
