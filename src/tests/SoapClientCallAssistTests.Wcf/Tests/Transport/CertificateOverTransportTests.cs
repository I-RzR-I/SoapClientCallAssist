using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Wcf.Helpers;
using SoapClientCallAssistTests.Wcf.Service.Common;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Wcf.Tests.Transport;

[TestClass]
public sealed class CertificateOverTransportTests
{

    private const string InvalidSecurityCode = "InvalidSecurity";

    private const string FailedAuthenticationCode = "FailedAuthentication";

    public TestContext TestContext { get; set; } = null!;

    private static IEnumerable<string> CertificateSecrets
    {
        get
        {
            foreach (var certificate in new[] { WcfHostFixture.TrustedCertificate, WcfHostFixture.UntrustedCertificate })
            {
                yield return certificate.Thumbprint;
                yield return certificate.Certificate.GetNameInfo(X509NameType.SimpleName, false);
            }
        }
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_SignedTimestampWithTrustedCertificate_IsAuthenticatedAsThatCertificate_Test(SoapProtocolType protocol)
    {
        var client = WcfCallSupport.CreateClient(protocol);

        var request = WcfCallSupport.BuildPost(
            client,
            WcfHostFixture.Address(WcfHostFixture.CertificateTransportRelativePath, protocol),
            WcfCallSupport.WhoAmIBody(),
            WcfProbeContract.WhoAmIAction,
            security: Security(WcfHostFixture.TrustedCertificate, signBody: false));

        var identity = WcfCallSupport.ReadWhoAmI(
            client,
            await WcfCallSupport.SendAcceptedAsync(client, request, $"WhoAmI({protocol}, signed timestamp)"));

        TestContext.WriteLine($"WhoAmI={identity.AuthenticationType}:{identity.Name} signing thumbprint={WcfHostFixture.TrustedCertificate.Thumbprint}");

        Assert.AreEqual(WcfProbeContract.X509AuthenticationType, identity.AuthenticationType);
        Assert.AreEqual(WcfHostFixture.TrustedCertificate.Thumbprint, identity.Name);
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task Echo_SignedTimestampWithTrustedCertificate_RoundTrips_Test(SoapProtocolType protocol)
    {
        var client = WcfCallSupport.CreateClient(protocol);
        var value = $"signed-{protocol}";

        var request = WcfCallSupport.BuildPost(
            client,
            WcfHostFixture.Address(WcfHostFixture.CertificateTransportRelativePath, protocol),
            WcfCallSupport.EchoBody(value),
            WcfProbeContract.EchoAction,
            security: Security(WcfHostFixture.TrustedCertificate, signBody: false));

        var body = await WcfCallSupport.SendAcceptedAsync(client, request, $"Echo({protocol}, signed timestamp)");

        Assert.AreEqual(value, WcfCallSupport.ReadEcho(client, body));
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_SignedWithAnUntrustedCertificate_IsRejected_Test(SoapProtocolType protocol)
    {
        var rejected = await SendRejectedAsync(protocol, Security(WcfHostFixture.UntrustedCertificate, signBody: false), "untrusted certificate");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, FailedAuthenticationCode, "untrusted certificate");
        rejected.AssertNoSecret(CertificateSecrets, "untrusted certificate");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_WithTheBodySigned_IsRejectedByTransportCredentialMode_Test(SoapProtocolType protocol)
    {
        var rejected = await SendRejectedAsync(protocol, Security(WcfHostFixture.TrustedCertificate, signBody: true), "signed body");

        rejected.AssertFault(
            WsSecurityNames.Wsse.NamespaceName,
            InvalidSecurityCode,
            "signed body");
        rejected.AssertNoSecret(CertificateSecrets, "signed body");
    }

    [DataTestMethod]
    [DataRow(SoapProtocolType.SOAP_1_1, DisplayName = "SOAP 1.1")]
    [DataRow(SoapProtocolType.SOAP_1_2, DisplayName = "SOAP 1.2")]
    public async Task WhoAmI_Unsigned_IsRejected_Test(SoapProtocolType protocol)
    {
        var rejected = await SendRejectedAsync(protocol, null, "unsigned");

        rejected.AssertFault(WsSecurityNames.Wsse.NamespaceName, InvalidSecurityCode, "unsigned");
        rejected.AssertNoSecret(CertificateSecrets, "unsigned");
    }

    private async Task<RejectedCall> SendRejectedAsync(SoapProtocolType protocol, SoapSecurityDto? security, string scenario)
    {
        var client = WcfCallSupport.CreateClient(protocol);

        var request = WcfCallSupport.BuildPost(
            client,
            WcfHostFixture.Address(WcfHostFixture.CertificateTransportRelativePath, protocol),
            WcfCallSupport.WhoAmIBody(),
            WcfProbeContract.WhoAmIAction,
            security: security);

        var rejected = await WcfCallSupport.SendRejectedAsync(client, request, $"WhoAmI({protocol}, {scenario})");

        TestContext.WriteLine($"{protocol} {scenario}: {rejected.Fault}");

        return rejected;
    }

    private static SoapSecurityDto Security(TestCertificate certificate, bool signBody)
        => new()
        {
            SigningCertificate = certificate.Certificate,
            SignBody = signBody,
            IncludeTimestamp = true,
            SignTimestamp = true
        };
}
