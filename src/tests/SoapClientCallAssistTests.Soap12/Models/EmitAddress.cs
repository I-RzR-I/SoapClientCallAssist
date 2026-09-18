using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitAddress
{
    [SoapMember(Name = "city", Order = 1)]
    public string City { get; set; } = string.Empty;

    [SoapMember(Name = "postCode", Order = 2)]
    public string PostCode { get; set; } = string.Empty;
}
