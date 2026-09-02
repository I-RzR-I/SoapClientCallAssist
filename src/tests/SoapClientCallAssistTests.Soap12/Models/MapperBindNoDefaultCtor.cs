
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "NoCtor", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindNoDefaultCtor
{
    public MapperBindNoDefaultCtor(int seed) => Id = seed;

    [SoapMember(Name = "Id", Order = 0)]
    public int Id { get; set; }
}
