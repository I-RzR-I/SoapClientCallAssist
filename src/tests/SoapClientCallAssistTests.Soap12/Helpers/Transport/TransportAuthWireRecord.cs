#nullable disable

namespace SoapClientCallAssistTests.Soap12.Helpers.Transport;

internal sealed class TransportAuthWireRecord
{

    internal string Path { get; init; }

    internal string Host { get; init; }

    internal string Authorization { get; init; }

    internal bool CarriesAuthorization => !string.IsNullOrEmpty(Authorization);

    internal string AuthorizationScheme
        => CarriesAuthorization ? Authorization.Split(' ', 2)[0] : null;

    public override string ToString()
        => $"{Path} host=[{Host}] authorization=[{(CarriesAuthorization ? AuthorizationScheme + " <token withheld>" : "absent")}]";
}
