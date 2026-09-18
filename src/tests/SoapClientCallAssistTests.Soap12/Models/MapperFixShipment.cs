
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using System.Collections.Generic;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Shipment", Namespace = MapperBindNs.Contract)]
public sealed class MapperFixShipment
{
    [SoapMember(Name = "Lines", Order = 0)]
    public List<MapperBindLine> Lines { get; set; }
}
