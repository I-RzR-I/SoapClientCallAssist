namespace SoapClientCallAssistTests.Wcf.Service.Symmetric;

public static class SymmetricProbeContract
{

    public const string Namespace = "http://SoapClientCallAssist.local/wcf/symmetric/";

    public const string EchoAction = Namespace + "ISymmetricProbeService/Echo";

    public const string WhoAmIAction = Namespace + "ISymmetricProbeService/WhoAmI";
}
