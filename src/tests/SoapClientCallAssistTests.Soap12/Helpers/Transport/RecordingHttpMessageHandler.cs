#nullable disable

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Helpers.Transport;

internal sealed class RecordingHttpMessageHandler : DelegatingHandler
{

    private readonly string _cannedEnvelope;

    private readonly TimeSpan _delay;

    internal RecordingHttpMessageHandler(string cannedEnvelope) : this(cannedEnvelope, TimeSpan.Zero)
    {
    }

    internal RecordingHttpMessageHandler(string cannedEnvelope, TimeSpan delay)
    {
        _cannedEnvelope = cannedEnvelope;
        _delay = delay;
    }

    internal bool Invoked { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Invoked = true;

        if (_delay > TimeSpan.Zero)
            await Task.Delay(_delay, cancellationToken);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = new StringContent(_cannedEnvelope)
        };
    }
}
