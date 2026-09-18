
#nullable disable

using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

public sealed class MapperBindTripwire
{
    public static int Constructions;

    public MapperBindTripwire() => Constructions++;

    [SoapMember(Name = "Id", Order = 0)]
    public int Id { get; set; }
}
