using SoapClientCallAssist.Attributes;
using System.Collections.Generic;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitBasket
{
    [SoapMember(Name = "basketId", Order = 1)]
    public string BasketId { get; set; } = string.Empty;

    [SoapMember(Name = "lines", Order = 2)]
    public List<EmitBasketLine> Lines { get; set; } = new();
}
