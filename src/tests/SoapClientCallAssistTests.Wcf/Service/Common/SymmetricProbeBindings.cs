using SoapClientCallAssist.Enums;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Text;

namespace SoapClientCallAssistTests.Wcf.Service.Common;

public static class SymmetricProbeBindings
{

    public static readonly MessageSecurityVersion SecureConversationFebruary2005 =
        MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;

    public static readonly MessageSecurityVersion SecureConversationDecember2005 =
        MessageSecurityVersion.WSSecurity11WSTrust13WSSecureConversation13WSSecurityPolicy12BasicSecurityProfile10;

    public static CustomBinding For(string credential, SoapSecureConversationVersionType secureConversation, MessageProtectionOrder protectionOrder, SoapProtocolType protocol)
    {
        var version = ProbeProtocols.Addressing10[protocol].Version;
        var securityVersion = ProbeProtocols.SecureConversationVersions[secureConversation].Version;

        return credential == "cert"
            ? Certificate(version, securityVersion, protectionOrder)
            : UserName(version, securityVersion, protectionOrder);
    }

    public static CustomBinding Certificate(MessageVersion version, MessageSecurityVersion securityVersion, MessageProtectionOrder protectionOrder)
        => Build(SecurityBindingElement.CreateMutualCertificateBindingElement(securityVersion, false), version, securityVersion, protectionOrder);

    public static CustomBinding UserName(MessageVersion version, MessageSecurityVersion securityVersion, MessageProtectionOrder protectionOrder)
        => Build(SecurityBindingElement.CreateUserNameForCertificateBindingElement(), version, securityVersion, protectionOrder);

    private static CustomBinding Build(
        SecurityBindingElement security, MessageVersion version, MessageSecurityVersion securityVersion, MessageProtectionOrder protectionOrder)
    {
        var symmetric = (SymmetricSecurityBindingElement)security;

        symmetric.MessageSecurityVersion = securityVersion;
        symmetric.MessageProtectionOrder = protectionOrder;
        symmetric.IncludeTimestamp = true;
        symmetric.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Basic256Sha256;
        symmetric.SetKeyDerivation(true);

        return new CustomBinding(symmetric, new TextMessageEncodingBindingElement(version, Encoding.UTF8), new HttpTransportBindingElement());
    }
}
