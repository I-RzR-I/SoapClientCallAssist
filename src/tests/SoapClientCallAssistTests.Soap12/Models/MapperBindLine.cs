
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Line", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindLine
{
    [SoapMember(Name = "Sku", Order = 0)]
    public string Sku { get; set; }

    [SoapMember(Name = "Qty", Order = 1)]
    public int Qty { get; set; }
}
