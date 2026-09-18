using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using System.Text;

namespace TestSoapServiceN45.Security
{
    public static class SecuredBindings
    {
        public const string UserNameSegment = "user";

        public const string CertificateSegment = "cert";

        public const string BothSegment = "both";

        public const string Soap11Segment = "soap11";

        public const string Soap12Segment = "soap12";

        public static CustomBinding UserNameOverTransport(MessageVersion version)
        {
            return Build(Insecure(SecurityBindingElement.CreateUserNameOverTransportBindingElement()), version);
        }

        public static CustomBinding CertificateOverTransport(MessageVersion version)
        {
            return Build(Insecure(SecurityBindingElement.CreateCertificateOverTransportBindingElement()), version);
        }

        public static CustomBinding UserNameWithEndorsingCertificate(MessageVersion version)
        {
            var security = Insecure(SecurityBindingElement.CreateUserNameOverTransportBindingElement());

            security.EndpointSupportingTokenParameters.Endorsing.Add(new X509SecurityTokenParameters
            {
                InclusionMode = SecurityTokenInclusionMode.AlwaysToRecipient,
                RequireDerivedKeys = false
            });

            return Build(security, version);
        }

        private static TransportSecurityBindingElement Insecure(TransportSecurityBindingElement security)
        {
            security.AllowInsecureTransport = true;
            security.IncludeTimestamp = true;
            security.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Basic256Sha256;

            return security;
        }

        private static CustomBinding Build(TransportSecurityBindingElement security, MessageVersion version)
        {
            return new CustomBinding(
                security,
                new TextMessageEncodingBindingElement(version, Encoding.UTF8),
                new HttpTransportBindingElement());
        }
    }
}
