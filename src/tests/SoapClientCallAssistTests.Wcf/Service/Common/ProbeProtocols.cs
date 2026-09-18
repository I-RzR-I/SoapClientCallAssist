using SoapClientCallAssist.Enums;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;

namespace SoapClientCallAssistTests.Wcf.Service.Common;

public static class ProbeProtocols
{

    public static readonly Dictionary<SoapProtocolType, (string Suffix, MessageVersion Version)> Plain = new()
    {
        { SoapProtocolType.SOAP_1_1, ("soap11", MessageVersion.Soap11) },
        { SoapProtocolType.SOAP_1_2, ("soap12", MessageVersion.Soap12) }
    };

    public static readonly Dictionary<SoapProtocolType, (string Suffix, MessageVersion Version)> Addressing10 = new()
    {
        { SoapProtocolType.SOAP_1_1, ("soap11", MessageVersion.Soap11WSAddressing10) },
        { SoapProtocolType.SOAP_1_2, ("soap12", MessageVersion.Soap12WSAddressing10) }
    };

    public static readonly Dictionary<SoapSecureConversationVersionType, (string Suffix, MessageSecurityVersion Version)> SecureConversationVersions = new()
    {
        { SoapSecureConversationVersionType.February2005, ("sc2005", SymmetricProbeBindings.SecureConversationFebruary2005) },
        { SoapSecureConversationVersionType.December2005, ("sc2007", SymmetricProbeBindings.SecureConversationDecember2005) }
    };

    public static readonly Dictionary<MessageProtectionOrder, string> ProtectionOrders = new()
    {
        { MessageProtectionOrder.SignBeforeEncryptAndEncryptSignature, "encsig" },
        { MessageProtectionOrder.SignBeforeEncrypt, "plainsig" }
    };
}
