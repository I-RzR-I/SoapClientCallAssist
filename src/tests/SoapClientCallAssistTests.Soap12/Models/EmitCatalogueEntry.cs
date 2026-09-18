using SoapClientCallAssist.Attributes;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitCatalogueEntry
{
    [SoapMember(Name = "entryId", Order = 1)]
    public string EntryId { get; set; } = string.Empty;

    [SoapMember(Name = "product", Order = 2)]
    public EmitProductContract? Product { get; set; }
}
