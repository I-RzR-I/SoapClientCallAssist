using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SoapClientCallAssistTests.Soap12.Models;

[DataContract]
internal sealed class EmitDataMemberOwner
{
    [DataMember(Name = "items", Order = 1)]
    public List<EmitDataMemberExplicitNamespace> Items { get; set; } = new();
}
