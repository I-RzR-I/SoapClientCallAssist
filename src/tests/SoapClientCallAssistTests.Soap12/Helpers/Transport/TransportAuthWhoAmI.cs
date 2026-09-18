#nullable disable

namespace SoapClientCallAssistTests.Soap12.Helpers.Transport;

internal sealed class TransportAuthWhoAmI
{

    internal string Scheme { get; init; }

    internal string ClientCertificateSha256 { get; init; }

    internal string ClientCertificateSubject { get; init; }

    internal string IdentityName { get; init; }

    internal string AuthenticationType { get; init; }

    internal bool IsAuthenticated { get; init; }

    public override string ToString()
        => $"scheme={Scheme} cert={ClientCertificateSha256} subject=[{ClientCertificateSubject}] identity=[{IdentityName}] "
           + $"authType=[{AuthenticationType}] authenticated={IsAuthenticated}";
}
