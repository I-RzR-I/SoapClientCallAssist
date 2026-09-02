
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Customer", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindCustomer
{
    [SoapMember(Name = "FullName", Order = 0)]
    public string FullName { get; set; }

    [SoapMember(Name = "Age", Order = 1)]
    public int Age { get; set; }
}
