using SoapClientCallAssist.Attributes;
using System;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitCultureModel
{
    [SoapMember(Name = "amount", Order = 1)]
    public decimal Amount { get; set; }

    [SoapMember(Name = "ratio", Order = 2)]
    public double Ratio { get; set; }

    [SoapMember(Name = "occurredOn", Order = 3)]
    public DateTime OccurredOn { get; set; }

    [SoapMember(Name = "elapsed", Order = 4)]
    public TimeSpan Elapsed { get; set; }
}
