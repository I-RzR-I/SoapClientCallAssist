#nullable disable

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Helpers.Transport;

internal sealed class RecordingPrimaryHttpClientHandler : HttpClientHandler
{

    private readonly string _cannedEnvelope;

    internal RecordingPrimaryHttpClientHandler(string cannedEnvelope)
    {
        _cannedEnvelope = cannedEnvelope;
        AutomaticDecompression = DecompressionMethods.None;
    }

    internal bool Invoked { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Invoked = true;

        return Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(_cannedEnvelope)
            });
    }
}
