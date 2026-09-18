
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Unmapped", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindNoMappedMembers
{
    public string Anything { get; set; }
}
