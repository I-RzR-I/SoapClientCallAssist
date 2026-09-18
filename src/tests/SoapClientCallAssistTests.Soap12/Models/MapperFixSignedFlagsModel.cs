
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Signed", Namespace = MapperBindNs.Contract)]
public sealed class MapperFixSignedFlagsModel
{
    [SoapMember(Name = "Mode", Order = 0)]
    public MapperFixSignedRights Mode { get; set; }
}
