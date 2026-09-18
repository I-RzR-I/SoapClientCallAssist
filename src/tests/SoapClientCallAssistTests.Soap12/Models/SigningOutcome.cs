#nullable disable

namespace SoapClientCallAssistTests.Soap12.Models;

internal sealed record SigningOutcome(string Payload, string Wire, string Failure);
