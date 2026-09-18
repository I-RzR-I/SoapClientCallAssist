using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Wcf.Helpers;

public sealed class CapturingHttpClientFactory : IHttpClientFactory
{

    private readonly Recorder _recorder = new();

    public IReadOnlyList<string> Requests => _recorder.Requests;

    public IReadOnlyList<string> Responses => _recorder.Responses;

    public HttpClient CreateClient(string name) => new(_recorder);

    private sealed class Recorder : DelegatingHandler
    {

        private readonly List<string> _requests = new();

        private readonly List<string> _responses = new();

        public Recorder() : base(new HttpClientHandler()) { }

        public IReadOnlyList<string> Requests => _requests;

        public IReadOnlyList<string> Responses => _responses;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync());
            var response = await base.SendAsync(request, cancellationToken);
            _responses.Add(response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync());

            return response;
        }
    }
}
