namespace SoapTestService;

public sealed record RecordedRequest(string Method, string Path, string QueryString,
    string? ContentType, IReadOnlyDictionary<string, IReadOnlyList<string>> Headers, string Body)
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; init; }
        = new Dictionary<string, IReadOnlyList<string>>(Headers, StringComparer.OrdinalIgnoreCase);
}
