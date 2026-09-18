
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Charred", Namespace = MapperBindNs.Contract)]
public sealed class MapperFixCharModel
{
    [SoapMember(Name = "Separator", Order = 0)]
    public char Separator { get; set; }
}
