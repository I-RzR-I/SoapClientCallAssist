using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "basketLine")]
internal sealed class EmitBasketLine
{
    [SoapMember(Name = "sku", Order = 1)]
    public string Sku { get; set; } = string.Empty;

    [SoapMember(Name = "count", Order = 2)]
    public int Count { get; set; }
}
