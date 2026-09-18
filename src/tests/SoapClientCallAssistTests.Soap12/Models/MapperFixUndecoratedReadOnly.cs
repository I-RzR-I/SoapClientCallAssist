
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Tolerant", Namespace = MapperBindNs.Contract)]
public sealed class MapperFixUndecoratedReadOnly
{
    [SoapMember(Name = "Name", Order = 0)]
    public string Name { get; set; }

    public string Computed => "computed";
}
