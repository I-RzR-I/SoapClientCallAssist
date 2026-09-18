
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Fault", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindFaultNamedPayload
{
    [SoapMember(Name = "Id", Order = 0)]
    public int Id { get; set; }
}
