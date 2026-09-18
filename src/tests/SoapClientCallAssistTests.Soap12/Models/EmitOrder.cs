using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitOrder
{
    [SoapMember(Name = "orderNumber", Order = 1)]
    public string OrderNumber { get; set; } = string.Empty;

    [SoapMember(Name = "shipTo", Order = 2)]
    public EmitAddress? ShipTo { get; set; }
}
