using System.ServiceModel.Channels;

namespace SoapClientCallAssistTests.Wcf.Service.Symmetric;

public sealed class SymmetricCapturingEncodingBindingElement : MessageEncodingBindingElement
{

    private readonly MessageEncodingBindingElement _inner;

    private readonly SymmetricWireCapture _capture;

    public SymmetricCapturingEncodingBindingElement(MessageEncodingBindingElement inner, SymmetricWireCapture capture)
    {
        _inner = inner;
        _capture = capture;
    }

    public override MessageVersion MessageVersion
    {
        get => _inner.MessageVersion;
        set => _inner.MessageVersion = value;
    }

    public override BindingElement Clone() => new SymmetricCapturingEncodingBindingElement((MessageEncodingBindingElement)_inner.Clone(), _capture);

    public override MessageEncoderFactory CreateMessageEncoderFactory()
        => new SymmetricCapturingEncoderFactory(_inner.CreateMessageEncoderFactory(), _capture);

    public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
    {
        context.BindingParameters.Add(this);

        return context.BuildInnerChannelFactory<TChannel>();
    }

    public override bool CanBuildChannelFactory<TChannel>(BindingContext context) => _inner.CanBuildChannelFactory<TChannel>(context);

    public override T GetProperty<T>(BindingContext context) => _inner.GetProperty<T>(context);
}
