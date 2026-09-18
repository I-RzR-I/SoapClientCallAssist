#nullable disable

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Helpers.Transport;

internal sealed class CannedResponseHttpMessageHandler : DelegatingHandler
{

    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    internal CannedResponseHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

    internal int Invocations { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Invocations++;

        var response = _respond(request);
        response.RequestMessage = request;

        return Task.FromResult(response);
    }
}
