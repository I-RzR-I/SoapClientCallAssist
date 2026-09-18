using System.ServiceModel.Channels;

namespace SoapClientCallAssistTests.Wcf.Service.Symmetric;

public sealed class SymmetricCapturingEncoderFactory : MessageEncoderFactory
{

    private readonly MessageEncoderFactory _inner;

    private readonly SymmetricCapturingEncoder _encoder;

    public SymmetricCapturingEncoderFactory(MessageEncoderFactory inner, SymmetricWireCapture capture)
    {
        _inner = inner;
        _encoder = new SymmetricCapturingEncoder(inner.Encoder, capture);
    }

    public override MessageEncoder Encoder => _encoder;

    public override MessageVersion MessageVersion => _inner.MessageVersion;
}
