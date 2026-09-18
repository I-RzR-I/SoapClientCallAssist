using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace SoapClientCallAssistTests.Wcf.Service.Common;

public static class ProbeCredentials
{

    public static void Configure(ServiceHost host, X509Certificate2 serviceCertificate, string trustedClientThumbprint, string userName, string password)
    {
        host.Credentials.ServiceCertificate.Certificate = serviceCertificate;

        var clientCertificates = host.Credentials.ClientCertificate.Authentication;
        clientCertificates.CertificateValidationMode = X509CertificateValidationMode.Custom;
        clientCertificates.RevocationMode = X509RevocationMode.NoCheck;
        clientCertificates.CustomCertificateValidator = new ThumbprintCertificateValidator(new[] { trustedClientThumbprint });

        var userNames = host.Credentials.UserNameAuthentication;
        userNames.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
        userNames.CustomUserNamePasswordValidator = new InMemoryUserNamePasswordValidator(
            new Dictionary<string, string> { { userName, password } });
    }
}
