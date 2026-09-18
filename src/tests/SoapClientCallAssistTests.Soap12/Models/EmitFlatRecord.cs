using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitFlatRecord
{
    [SoapMember(Name = "code", Order = 1)]
    public string Code { get; set; } = string.Empty;

    [SoapMember(Name = "quantity", Order = 2)]
    public int Quantity { get; set; }

    [SoapMember(Name = "active", Order = 3)]
    public bool Active { get; set; }
}
