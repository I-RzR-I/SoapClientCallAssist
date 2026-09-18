using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Text;

namespace SoapClientCallAssistTests.Wcf.Service.Symmetric;

public sealed class SymmetricCapturingEncoder : MessageEncoder
{

    private readonly MessageEncoder _inner;

    private readonly SymmetricWireCapture _capture;

    public SymmetricCapturingEncoder(MessageEncoder inner, SymmetricWireCapture capture)
    {
        _inner = inner;
        _capture = capture;
    }

    public override string ContentType => _inner.ContentType;

    public override string MediaType => _inner.MediaType;

    public override MessageVersion MessageVersion => _inner.MessageVersion;

    public override bool IsContentTypeSupported(string contentType) => _inner.IsContentTypeSupported(contentType);

    public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
    {
        _capture.RecordResponse(Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count));

        return _inner.ReadMessage(buffer, bufferManager, contentType);
    }

    public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
    {
        using var copy = new MemoryStream();
        stream.CopyTo(copy);

        var bytes = copy.ToArray();
        _capture.RecordResponse(Encoding.UTF8.GetString(bytes));

        return _inner.ReadMessage(new MemoryStream(bytes), maxSizeOfHeaders, contentType);
    }

    public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
    {
        var written = _inner.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);

        _capture.RecordRequest(Encoding.UTF8.GetString(written.Array, written.Offset, written.Count));

        return written;
    }

    public override void WriteMessage(Message message, Stream stream)
    {
        using var copy = new MemoryStream();
        _inner.WriteMessage(message, copy);

        var bytes = copy.ToArray();
        _capture.RecordRequest(Encoding.UTF8.GetString(bytes));

        stream.Write(bytes, 0, bytes.Length);
    }
}
