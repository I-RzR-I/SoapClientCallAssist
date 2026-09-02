using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

internal abstract class EmitOrderingBase
{
    [SoapMember(Name = "baseAlpha", Order = 1)]
    public string BaseAlpha { get; set; } = string.Empty;

    [SoapMember(Name = "baseBeta", Order = 2)]
    public string BaseBeta { get; set; } = string.Empty;
}
