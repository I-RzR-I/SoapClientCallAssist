#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Symmetric;

[TestClass]
public sealed class SymmetricStatelessnessTests
{

    private const int InterleavedBuilds = 40;

    [TestMethod]
    public async Task BuildRequest_FortyInterleavedSymmetricBuildsForTwoServicesOnOneSingletonClient_KeepEveryWireBoundToItsOwnOptions_Test()
    {
        using var serviceA = CertificateScenarioSupport.CreateRsaCertificate("CN=Scca.Tests.Symmetric.Service.A", 2048);
        using var serviceB = CertificateScenarioSupport.CreateRsaCertificate("CN=Scca.Tests.Symmetric.Service.B", 2048);

        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var optionsA = Options(serviceA, "alice-a", "password-a");
        var optionsB = Options(serviceB, "bob-b", "password-b");

        var outcomes = await Task.WhenAll(
            Enumerable.Range(0, InterleavedBuilds).Select(index => Task.Run(() =>
            {
                var even = index % 2 == 0;

                var built = client.BuildRequest(
                    HttpMethod.Post,
                    new BuildSoapRequestDto
                    {
                        Client = new HttpClientDto(WsSecurityTestSupport.Endpoint),
                        Envelope = new SoapEnvelopeDto(new[] { WsSecurityTestSupport.Body("p" + index) }, null, WsSecurityTestSupport.Action),
                        Security = even ? optionsA : optionsB
                    });

                return (Index: index, Service: even ? serviceA : serviceB, Username: even ? "alice-a" : "bob-b", Wire: WsSecurityTestSupport.Wire(built, $"{index}"));
            })));

        var wires = outcomes.Select(outcome => (outcome.Index, outcome.Username, Parsed: SymmetricTestSupport.Parse(outcome.Wire, outcome.Service), outcome.Service)).ToList();

        foreach (var (index, username, parsed, service) in wires)
        {
            Assert.AreEqual(
                Convert.ToBase64String(SymmetricTestSupport.Sha1(service.RawData)),
                SymmetricWire.KeyInfoReference(parsed.EncryptedKey).InnerText,
                $"{index}");

            Assert.IsTrue(SymmetricTestSupport.PrimarySignatureVerifiesAfterDecryption(parsed), $"{index}");

            Assert.AreEqual(
                username,
                WsSecurityFoundationTestSupport.ChildText(SymmetricTestSupport.DecryptElement(parsed.EncryptedDatas[0], parsed.EncryptionKey), "Username"),
                $"{index}");
        }

        Assert.AreEqual(InterleavedBuilds, wires.Select(wire => Convert.ToBase64String(wire.Parsed.Secret)).Distinct().Count());
        Assert.AreEqual(InterleavedBuilds * 2, wires.SelectMany(wire => wire.Parsed.Nonces).Select(Convert.ToBase64String).Distinct().Count());
    }

    [DataTestMethod]
    [DataRow("SoapClientCallAssist.Security.WsSecurity.WsSecuritySymmetricResponseVerifier")]
    [DataRow("SoapClientCallAssist.Security.WsSecurity.WsSecurityKeyDerivation")]
    [DataRow("SoapClientCallAssist.Security.WsSecurity.WsSecurityEncryptor")]
    public void SymmetricHelpers_AreStaticAndDeclareNoInstanceState_Test(string fullName)
    {
        var type = WsSecurityFoundationTestSupport.LibraryType(fullName);

        Assert.IsTrue(type.IsAbstract && type.IsSealed, fullName);
        Assert.AreEqual(0, type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            .Count(field => field.FieldType == typeof(byte[])), fullName);
    }

    private static SoapSecurityDto Options(X509Certificate2 service, string username, string password)
        => new()
        {
            Enabled = true,
            SymmetricBinding = new SoapSymmetricBindingDto { ServiceCertificate = service },
            Addressing = new SoapAddressingDto(),
            UsernameToken = new SoapUsernameTokenDto { Username = username, Password = password }
        };
}
