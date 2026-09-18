using System.ServiceModel.Channels;
using System.ServiceModel.Security;

namespace SoapClientCallAssistTests.Wcf.Service.Plain;

public static class WcfProbeBindings
{

    public static CustomBinding Plain(MessageVersion version)
        => new(TextEncoding(version), new HttpTransportBindingElement());

    public static CustomBinding CertificateOverTransport(MessageVersion version)
        => new(
            InsecureTransportSecurity(SecurityBindingElement.CreateCertificateOverTransportBindingElement()),
            TextEncoding(version),
            new HttpTransportBindingElement());

    public static CustomBinding UserNameOverTransport(MessageVersion version)
        => new(
            InsecureTransportSecurity(SecurityBindingElement.CreateUserNameOverTransportBindingElement()),
            TextEncoding(version),
            new HttpTransportBindingElement());

    private static TransportSecurityBindingElement InsecureTransportSecurity(TransportSecurityBindingElement security)
    {
        security.AllowInsecureTransport = true;
        security.IncludeTimestamp = true;
        security.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Basic256Sha256;

        return security;
    }

    private static TextMessageEncodingBindingElement TextEncoding(MessageVersion version)
        => new(version, System.Text.Encoding.UTF8);
}
