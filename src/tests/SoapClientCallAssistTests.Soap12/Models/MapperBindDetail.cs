
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Detail", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindDetail
{
    [SoapMember(Name = "Description", Order = 0)]
    public string Description { get; set; }

    [SoapMember(Name = "Weight", Order = 1)]
    public double Weight { get; set; }
}
