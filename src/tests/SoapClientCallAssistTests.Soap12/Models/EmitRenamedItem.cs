using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "itemOnWire")]
internal sealed class EmitRenamedItem
{
    [SoapMember(Name = "itemScalarOnWire", Order = 1)]
    public string ClrItemProperty { get; set; } = string.Empty;
}
