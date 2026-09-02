
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "AddProduct", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindAddProductResult
{
    [SoapMember(Name = "product", Order = 0)]
    public MapperBindProduct Product { get; set; }
}
