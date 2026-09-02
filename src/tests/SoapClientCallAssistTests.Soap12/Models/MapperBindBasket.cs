
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers;
using System.Collections.Generic;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Basket", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindBasket
{
    [SoapMember(Name = "Id", Order = 0)]
    public int Id { get; set; }

    [SoapMember(Name = "LocationIds", ItemName = "int", Order = 1)]
    public List<int> LocationIds { get; set; }


    [SoapMember(Name = "Tags", Order = 2)]
    public string[] Tags { get; set; }

    [SoapMember(Name = "Lines", ItemName = "Line", Order = 3)]
    public List<MapperBindLine> Lines { get; set; }
}
