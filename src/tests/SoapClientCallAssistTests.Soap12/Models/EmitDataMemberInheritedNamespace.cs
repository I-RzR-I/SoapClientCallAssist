using System.Runtime.Serialization;

namespace SoapClientCallAssistTests.Soap12.Models;

[DataContract]
internal sealed class EmitDataMemberInheritedNamespace
{
    [DataMember(Name = "alpha", Order = 1)]
    public string Alpha { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Beta { get; set; } = string.Empty;

    [DataMember(Name = "ignoredOnWire", Order = 3)]
    [IgnoreDataMember]
    public string IgnoredMember { get; set; } = string.Empty;

    public string UndecoratedMember { get; set; } = string.Empty;
}
