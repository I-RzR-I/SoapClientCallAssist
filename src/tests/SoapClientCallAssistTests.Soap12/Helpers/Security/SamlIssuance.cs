#nullable disable

using System;
using System.Security.Cryptography.X509Certificates;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal sealed class SamlIssuance
{

    internal bool Saml20 { get; set; } = true;

    internal bool HolderOfKey { get; set; }

    internal X509Certificate2 ProofCertificate { get; set; }

    internal X509Certificate2 IssuerCertificate { get; set; } = SamlTestSupport.IssuerCertificate;

    internal bool Sign { get; set; } = true;

    internal bool PrettyPrint { get; set; }

    internal string Audience { get; set; } = SamlTestSupport.Audience;

    internal string SubjectName { get; set; } = SamlTestSupport.SubjectName;

    internal string SubjectRole { get; set; } = SamlTestSupport.SubjectRole;

    internal DateTime NotBefore { get; set; } = DateTime.UtcNow.AddMinutes(-2);

    internal DateTime NotOnOrAfter { get; set; } = DateTime.UtcNow.AddMinutes(10);

    internal string NotOnOrAfterOverride { get; set; }
}
