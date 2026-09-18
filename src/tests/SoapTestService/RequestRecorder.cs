using System.Collections.Concurrent;

namespace SoapTestService;

public sealed class RequestRecorder
{
    public const int Capacity = 256;

    private readonly ConcurrentQueue<RecordedRequest> _entries = new();

    public void Add(RecordedRequest entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries.Enqueue(entry);

        while (_entries.Count > Capacity)
            _entries.TryDequeue(out _);
    }

    public IReadOnlyList<RecordedRequest> Snapshot() => _entries.ToArray();

    public void Clear() => _entries.Clear();
}
