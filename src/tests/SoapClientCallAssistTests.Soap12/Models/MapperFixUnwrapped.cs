
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Unwrapped", Namespace = MapperBindNs.Contract)]
public sealed class MapperFixUnwrapped
{
    [SoapMember(Name = "Score", Order = 0)]
    public int[] Scores { get; set; }
}
