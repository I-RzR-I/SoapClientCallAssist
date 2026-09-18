namespace SoapClientCallAssistTests.Wcf.Service.SecureConversation;

public static class SecureConversationProbeContract
{

    public const string Namespace = "http://SoapClientCallAssist.local/wcf/sc/";

    public const string EchoAction = Namespace + "ISecureConversationProbeService/Echo";

    public const string WhoAmIAction = Namespace + "ISecureConversationProbeService/WhoAmI";
}
