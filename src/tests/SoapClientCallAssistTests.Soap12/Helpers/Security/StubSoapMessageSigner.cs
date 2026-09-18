#nullable disable

using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using RzR.ResultMessage.Models;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal sealed class StubSoapMessageSigner : ISoapMessageSigner
{

    internal const string PlantedDetailSecret = "PLANTED-DETAIL-SECRET-8f2c41";

    private readonly string _detail;

    private readonly object _planted;

    internal StubSoapMessageSigner(string detail) => _detail = detail;

    private StubSoapMessageSigner(object planted) => _planted = planted;

    internal static StubSoapMessageSigner Planting(object planted) => new(planted);

    public IResult<string> Sign(XElement soapEnvelope, SoapSecurityDto options)
        => Result<string>.Failure(WsSecurityTestSupport.SigningKeyCode, Message());

    private MessageDataModel Message()
        => _planted is null
            ? new MessageDataModel(
                WsSecurityTestSupport.MissingSigningKeyMessage,
                string.IsNullOrEmpty(_detail) ? new string[0] : new[] { _detail })
            : new PlantedMessageDataModel(WsSecurityTestSupport.MissingSigningKeyMessage, _planted);
}
