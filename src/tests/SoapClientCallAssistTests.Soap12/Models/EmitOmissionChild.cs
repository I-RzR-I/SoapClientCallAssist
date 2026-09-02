using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitOmissionChild
{
    [SoapMember(Name = "childValue", Order = 1)]
    public string ChildValue { get; set; } = string.Empty;
}
