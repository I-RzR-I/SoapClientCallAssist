using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Helpers;

internal static class WsSecurityNames
{

    internal static readonly XNamespace Wsse =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

    internal static readonly XNamespace Wsu =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    internal const string PasswordTextType =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText";

    internal const string AddressingNoneNamespace = "http://schemas.microsoft.com/ws/2005/05/addressing/none";
}
