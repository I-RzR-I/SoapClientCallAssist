
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Enums;
using SoapClientCallAssistTests.Soap12.Helpers;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Account", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindAccount
{
    [SoapMember(Name = "Status", Order = 0)]
    public MapperBindStatus Status { get; set; }

    [SoapMember(Name = "Rights", Order = 1)]
    public MapperBindRights Rights { get; set; }
}
