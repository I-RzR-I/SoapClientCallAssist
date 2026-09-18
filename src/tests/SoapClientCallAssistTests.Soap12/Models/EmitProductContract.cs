using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "ProductContract", Namespace = MapperEmitNs.Product)]
internal sealed class EmitProductContract
{
    [SoapMember(Name = "name", Order = 1)]
    public string Name { get; set; } = string.Empty;

    [SoapMember(Name = "price", Order = 2)]
    public decimal Price { get; set; }
}
