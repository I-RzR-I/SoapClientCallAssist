using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitUnsupportedMemberType
{
    [SoapMember(Name = "supported", Order = 1)]
    public string Supported { get; set; } = string.Empty;

    [SoapMember(Name = "unsupported", Order = 2)]
    public object Unsupported { get; set; } = new();
}
