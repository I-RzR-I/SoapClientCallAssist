namespace SoapClientCallAssistTests.Wcf.Service.TierB;

public static class TierBProbeContract
{

    public const string Namespace = "http://SoapClientCallAssist.local/wcf/tierb/";

    public const string EchoAction = Namespace + "ITierBProbeService/Echo";

    public const string WhoAmIAction = Namespace + "ITierBProbeService/WhoAmI";
}
