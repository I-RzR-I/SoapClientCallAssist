using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace SoapClientCallAssistTests.Wcf.Service.Common;

public sealed class ThumbprintCertificateValidator : X509CertificateValidator
{

    private readonly HashSet<string> _allowedThumbprints;

    public ThumbprintCertificateValidator(IEnumerable<string> allowedThumbprints)
        => _allowedThumbprints = new HashSet<string>(allowedThumbprints, StringComparer.OrdinalIgnoreCase);

    public override void Validate(X509Certificate2 certificate)
    {
        if (certificate is null)
            throw new ArgumentNullException(nameof(certificate));

        if (!_allowedThumbprints.Contains(certificate.Thumbprint))
            throw new SecurityTokenValidationException("The client certificate is not on the allow-list.");
    }
}
