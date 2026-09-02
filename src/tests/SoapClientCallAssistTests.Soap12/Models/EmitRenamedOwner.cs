using SoapClientCallAssist.Attributes;
using System.Collections.Generic;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "wrapperOnWire")]
internal sealed class EmitRenamedOwner
{
    [SoapMember(Name = "scalarOnWire", Order = 1)]
    public string ClrScalarProperty { get; set; } = string.Empty;

    [SoapMember(Name = "collectionOnWire", Order = 2)]
    public List<EmitRenamedItem> ClrCollectionProperty { get; set; } = new();
}
