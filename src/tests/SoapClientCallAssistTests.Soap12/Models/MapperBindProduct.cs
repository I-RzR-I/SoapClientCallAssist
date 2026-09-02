
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Enums;
using SoapClientCallAssistTests.Soap12.Helpers;
using System;
using System.Collections.Generic;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Product", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindProduct
{
    [SoapMember(Name = "Code", Order = 0)]
    public string Code { get; set; }

    [SoapMember(Name = "Quantity", Order = 1)]
    public int Quantity { get; set; }

    [SoapMember(Name = "Price", Order = 2)]
    public decimal Price { get; set; }

    [SoapMember(Name = "ReleasedOn", Order = 3)]
    public DateTime ReleasedOn { get; set; }

    [SoapMember(Name = "Reference", Order = 4)]
    public Guid Reference { get; set; }

    [SoapMember(Name = "Status", Order = 5)]
    public MapperBindStatus Status { get; set; }

    [SoapMember(Name = "Detail", Order = 6)]
    public MapperBindDetail Detail { get; set; }

    [SoapMember(Name = "LocationIds", ItemName = "int", Order = 7)]
    public List<int> LocationIds { get; set; }

    [SoapMember(Name = "Lines", ItemName = "Line", Order = 8)]
    public List<MapperBindLine> Lines { get; set; }
}
