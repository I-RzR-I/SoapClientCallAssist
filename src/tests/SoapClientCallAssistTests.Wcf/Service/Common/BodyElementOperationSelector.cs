using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace SoapClientCallAssistTests.Wcf.Service.Common;

public sealed class BodyElementOperationSelector : IDispatchOperationSelector
{

    public string SelectOperation(ref Message message)
    {
        if (message.IsEmpty)
            return string.Empty;

        var buffer = message.CreateBufferedCopy(int.MaxValue);

        using var copy = buffer.CreateMessage();
        using var reader = copy.GetReaderAtBodyContents();

        reader.MoveToContent();

        var operationName = reader.LocalName;

        message = buffer.CreateMessage();

        return operationName;
    }
}
