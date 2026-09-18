#nullable disable

using System.Collections.Generic;

namespace SoapClientCallAssistTests.Soap12.Helpers.Hosting;

internal sealed class RedirectTargetRecord
{

    internal string Method { get; init; }

    internal string Path { get; init; }

    internal string Body { get; init; }

    internal IReadOnlyDictionary<string, string> Headers { get; init; }

    internal string Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;
}
