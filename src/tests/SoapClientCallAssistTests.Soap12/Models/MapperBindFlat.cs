
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;
using System;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Flat", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindFlat
{
    [SoapMember(Name = "Id", Order = 0)]
    public int Id { get; set; }

    [SoapMember(Name = "Name", Order = 1)]
    public string Name { get; set; }

    [SoapMember(Name = "Ratio", Order = 2)]
    public decimal Ratio { get; set; }

    [SoapMember(Name = "Active", Order = 3)]
    public bool Active { get; set; }

    [SoapMember(Name = "Reference", Order = 4)]
    public Guid Reference { get; set; }

    [SoapMember(Name = "CreatedOn", Order = 5)]
    public DateTime CreatedOn { get; set; }
}
