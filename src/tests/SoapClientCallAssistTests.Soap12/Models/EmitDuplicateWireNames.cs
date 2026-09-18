using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitDuplicateWireNames
{
    [SoapMember(Name = "sameName", Order = 1)]
    public string First { get; set; } = string.Empty;

    [SoapMember(Name = "sameName", Order = 2)]
    public string Second { get; set; } = string.Empty;
}
