
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Presets", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindPresets
{
    [SoapMember(Name = "Text", Order = 0)]
    public string Text { get; set; } = "preset-text";

    [SoapMember(Name = "Count", Order = 1)]
    public int Count { get; set; } = 99;
}
