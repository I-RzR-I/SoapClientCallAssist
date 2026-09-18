using System;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace TestSoapServiceN45.Security
{
    public sealed class ThumbprintCertificateValidator : X509CertificateValidator
    {
        public override void Validate(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            if (!TestCredentials.IsTrustedThumbprint(certificate.Thumbprint))
            {
                SecuredDiagnosticsLog.Write(nameof(ThumbprintCertificateValidator), "Refused client certificate " + certificate.Subject + " thumbprint " + certificate.Thumbprint + ".");

                throw new SecurityTokenValidationException("The client certificate is not on the allow-list.");
            }

            SecuredDiagnosticsLog.Write(nameof(ThumbprintCertificateValidator), "Accepted client certificate thumbprint " + certificate.Thumbprint + ".");
        }
    }
}
