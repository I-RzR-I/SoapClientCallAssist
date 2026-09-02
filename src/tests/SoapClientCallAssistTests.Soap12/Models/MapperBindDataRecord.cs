
#nullable disable

using SoapClientCallAssistTests.Soap12.Helpers;
using System.Runtime.Serialization;

namespace SoapClientCallAssistTests.Soap12.Models;

[DataContract(Name = "Record", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindDataRecord
{
    [DataMember(Name = "Id", Order = 0)]
    public int Id { get; set; }

    [DataMember(Name = "Label", Order = 1)]
    public string Label { get; set; }


    [DataMember(Name = "Secret", Order = 2)]
    [IgnoreDataMember]
    public string Secret { get; set; }


    public string Unmapped { get; set; }
}
