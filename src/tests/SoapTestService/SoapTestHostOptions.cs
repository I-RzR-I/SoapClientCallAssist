namespace SoapTestService;

public sealed class SoapTestHostOptions
{
    public IReadOnlyList<SoapTlsListenerOptions> TlsListeners { get; init; } = Array.Empty<SoapTlsListenerOptions>();
}
