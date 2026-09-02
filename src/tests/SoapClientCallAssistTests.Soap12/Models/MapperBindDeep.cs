
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Deep", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindDeep
{
    [SoapMember(Name = "Id", Order = 0)]
    public int Id { get; set; }
}
