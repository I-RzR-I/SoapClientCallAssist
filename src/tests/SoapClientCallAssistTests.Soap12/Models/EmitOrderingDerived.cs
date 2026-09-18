using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitOrderingDerived : EmitOrderingBase
{
    [SoapMember(Name = "derivedGamma", Order = 1)]
    public string DerivedGamma { get; set; } = string.Empty;

    [SoapMember(Name = "derivedDelta", Order = 2)]
    public string DerivedDelta { get; set; } = string.Empty;
}
