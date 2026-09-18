using System;
using System.Net.Http;

namespace SoapClientCallAssistTests.Wcf.Helpers.Symmetric;

internal sealed class SymmetricExchange : IDisposable
{

    internal SymmetricExchange(HttpRequestMessage request, string requestWire, string responseWire)
    {
        Request = request;
        RequestWire = requestWire;
        ResponseWire = responseWire;
    }

    internal HttpRequestMessage Request { get; }

    internal string RequestWire { get; }

    internal string ResponseWire { get; }

    public void Dispose() => Request.Dispose();
}
