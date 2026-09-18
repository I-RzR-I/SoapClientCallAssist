
#nullable disable

using SoapClientCallAssist.Attributes;
using SoapClientCallAssistTests.Soap12.Helpers.Mapper;

namespace SoapClientCallAssistTests.Soap12.Models;

[SoapContract(Name = "Pathed", Namespace = MapperBindNs.Contract)]
public sealed class MapperBindPathed
{
    [SoapMember(Name = "Id", Order = 0)]
    public int Id { get; set; }

    [SoapMember(Name = "City", Path = "Address/Primary", Order = 1)]
    public string City { get; set; }

    [SoapMember(Name = "Zip", Path = "Address/Primary", Order = 2)]
    public string Zip { get; set; }
    
    [SoapMember(Name = "City", Path = "Address/Backup", Order = 3)]
    public string BackupCity { get; set; }
}
