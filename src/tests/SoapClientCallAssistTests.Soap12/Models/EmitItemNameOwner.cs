using SoapClientCallAssist.Attributes;
using System.Collections.Generic;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitItemNameOwner
{
    [SoapMember(Name = "idsOnWire", Order = 1, ItemName = "identifier")]
    public List<int> ClrIdsProperty { get; set; } = new();
}
