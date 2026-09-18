
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using System.Collections.Generic;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Nillable", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindNillable
{
    [SoapMember(Name = "Text", Order = 0)]
    public string Text { get; set; } = "preset-text";

    [SoapMember(Name = "Count", Order = 1)]
    public int? Count { get; set; } = 99;

    [SoapMember(Name = "Ids", ItemName = "int", Order = 2)]
    public List<int> Ids { get; set; }
}
