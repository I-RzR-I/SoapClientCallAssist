using SoapClientCallAssist.Attributes;
using System.Collections.Generic;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitOmissionModel
{
    [SoapMember(Name = "presentValue", Order = 1)]
    public string PresentValue { get; set; } = string.Empty;

    [SoapMember(Name = "nullText", Order = 2)]
    public string? NullText { get; set; }

    [SoapMember(Name = "nullNumber", Order = 3)]
    public int? NullNumber { get; set; }

    [SoapMember(Name = "nullComplex", Order = 4)]
    public EmitOmissionChild? NullComplex { get; set; }

    [SoapMember(Name = "emptyCollection", Order = 5)]
    public List<int> EmptyCollection { get; set; } = new();
}
