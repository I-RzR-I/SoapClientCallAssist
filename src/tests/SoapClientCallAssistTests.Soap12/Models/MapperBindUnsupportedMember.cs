
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Unsupported", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindUnsupportedMember
{
    [SoapMember(Name = "Handle", Order = 0)]
    public nint Handle { get; set; }
}
