#nullable disable

using RzR.ResultMessage.Models;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal sealed class PlantedMessageDataModel : MessageDataModel
{

    internal PlantedMessageDataModel(string info, object planted) : base(info) => Planted = planted;

    public object Planted { get; }
}
