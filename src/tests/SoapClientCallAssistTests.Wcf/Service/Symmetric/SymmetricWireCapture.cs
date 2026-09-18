using System.Collections.Generic;

namespace SoapClientCallAssistTests.Wcf.Service.Symmetric;

public sealed class SymmetricWireCapture
{

    private readonly List<string> _requests = new();

    private readonly List<string> _responses = new();

    public IReadOnlyList<string> Requests => _requests;

    public IReadOnlyList<string> Responses => _responses;

    public void RecordRequest(string wire) => _requests.Add(wire);

    public void RecordResponse(string wire) => _responses.Add(wire);
}
