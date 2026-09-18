namespace SoapClientCallAssistTests.Wcf.Service.Saml;

public static class SamlProbeContract
{

    public const string Namespace = "http://SoapClientCallAssist.local/wcf/saml/";

    public const string WhoAmIAction = Namespace + "ISamlProbeService/WhoAmI";

    public const string IssuerName = "urn:soapclientcallassist:tests:saml-issuer";

    public const string BearerRelativePath = "saml-bearer";

    public const string HolderOfKeyRelativePath = "saml-hok";

    public const string SamlV11TokenType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1";

    public const string SamlV20TokenType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0";
}
