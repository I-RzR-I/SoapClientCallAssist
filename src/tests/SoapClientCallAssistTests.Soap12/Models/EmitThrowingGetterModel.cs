using SoapClientCallAssist.Attributes;
using System;

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed class EmitThrowingGetterModel
{
    internal const string SecretValue = "SECRET-a1b2c3-do-not-leak";

    internal const string BclFormatWording = "was not in a correct format";

    [SoapMember(Name = "safeValue", Order = 1)]
    public string SafeValue { get; set; } = "safe";

    [SoapMember(Name = "explodingValue", Order = 2)]
    public string ExplodingValue
    {
        get => throw new FormatException($"The input string '{SecretValue}' {BclFormatWording}.");
        set { _ = value; }
    }
}
