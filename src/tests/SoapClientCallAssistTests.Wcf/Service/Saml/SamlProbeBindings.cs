using System.IdentityModel.Tokens;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using System.Text;

namespace SoapClientCallAssistTests.Wcf.Service.Saml;

public static class SamlProbeBindings
{

    public static CustomBinding IssuedTokenOverTransport(MessageVersion version, SecurityKeyType keyType)
    {
        var parameters = new IssuedSecurityTokenParameters { KeyType = keyType };

        var security = SecurityBindingElement.CreateIssuedTokenOverTransportBindingElement(parameters);
        security.AllowInsecureTransport = true;
        security.IncludeTimestamp = true;
        security.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Basic256Sha256;

        return new CustomBinding(security, new TextMessageEncodingBindingElement(version, Encoding.UTF8), new HttpTransportBindingElement());
    }
}
