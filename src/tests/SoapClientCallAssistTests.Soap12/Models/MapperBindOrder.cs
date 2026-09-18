
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Order", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindOrder
{
    [SoapMember(Name = "Code", Order = 0)]
    public string Code { get; set; }

    [SoapMember(Name = "Customer", Order = 1)]
    public MapperBindCustomer Customer { get; set; }
}
