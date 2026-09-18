using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace TestSoapServiceN45.Security
{
    public sealed class BodyElementOperationSelector : IDispatchOperationSelector
    {
        public string SelectOperation(ref Message message)
        {
            if (message.IsEmpty)
                return string.Empty;

            var buffer = message.CreateBufferedCopy(int.MaxValue);
            string operationName;

            using (var copy = buffer.CreateMessage())
            using (var reader = copy.GetReaderAtBodyContents())
            {
                reader.MoveToContent();
                operationName = reader.LocalName;
            }

            message = buffer.CreateMessage();

            return operationName;
        }
    }
}
