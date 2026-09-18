namespace SoapClientCallAssistTests.Wcf.Service.Common;

public static class WcfProbeContract
{

    public const string Namespace = "http://SoapClientCallAssist.local/wcf/";

    public const string EchoAction = Namespace + "IWcfProbeService/Echo";

    public const string WhoAmIAction = Namespace + "IWcfProbeService/WhoAmI";

    public const string AnonymousAuthenticationType = "Anonymous";

    public const string X509AuthenticationType = "X509";
}
