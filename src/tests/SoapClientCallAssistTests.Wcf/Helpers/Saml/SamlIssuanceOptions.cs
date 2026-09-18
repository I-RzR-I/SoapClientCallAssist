#nullable disable

using System;
using System.Security.Cryptography.X509Certificates;

namespace SoapClientCallAssistTests.Wcf.Helpers.Saml;

internal sealed class SamlIssuanceOptions
{

    internal bool Saml20 { get; set; } = true;

    internal X509Certificate2 ProofCertificate { get; set; }

    internal X509Certificate2 IssuerCertificate { get; set; }

    internal bool Sign { get; set; } = true;

    internal bool PrettyPrint { get; set; }

    internal string Audience { get; set; }

    internal string SubjectName { get; set; } = SamlCallSupport.SubjectName;

    internal string SubjectRole { get; set; } = SamlCallSupport.SubjectRole;

    internal DateTime NotBefore { get; set; } = DateTime.UtcNow.AddMinutes(-2);

    internal DateTime NotOnOrAfter { get; set; } = DateTime.UtcNow.AddMinutes(10);
}
