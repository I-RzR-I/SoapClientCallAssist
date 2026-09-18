#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Client;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class WsSecuritySigningCodePropagationTests
{

    private const string EnvelopeXml =
        "<soap:Envelope xmlns:soap=\"" + SoapAssert.Soap12Ns + "\">"
        + "<soap:Body><IsValid xmlns=\"" + SoapAssert.ServiceNs + "\"><id>s1</id></IsValid></soap:Body>"
        + "</soap:Envelope>";

    private X509Certificate2 _ecdsaCertificate;

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public void Initialize()
        => _ecdsaCertificate = CertificateScenarioSupport.CreateEcdsaCertificate("CN=Scca.Tests.Ecdsa.CodeLoss");

    [TestCleanup]
    public void Cleanup() => _ecdsaCertificate?.Dispose();

    [TestMethod]
    public void Sign_WithACertificateCarryingNoRsaKey_RaisesTheSigningKeyCodeAtTheSigner_Test()
    {
        var signed = new WsSecurityMessageSigner().Sign(XElement.Parse(EnvelopeXml), Security());

        Assert.IsFalse(signed.IsSuccess, NegativeTestSupport.Describe(signed));

        Assert.AreEqual(WsSecurityTestSupport.SigningKeyCode, NegativeTestSupport.Messages(signed)[0].Key, NegativeTestSupport.Describe(signed));
    }

    [TestMethod]
    public void BuildRequest_WithACertificateCarryingNoRsaKey_KeepsBothTheSignerTextAndItsCode_Test()
    {
        var signed = new WsSecurityMessageSigner().Sign(XElement.Parse(EnvelopeXml), Security());
        var built = WsSecurityTestSupport.BuildPost(Security());

        Assert.IsFalse(built.IsSuccess, NegativeTestSupport.Describe(built));

        TestContext.WriteLine(
            $"ISoapMessageSigner.Sign codes=[{Key(signed)}], BuildRequest codes=[{Key(built)}]");

        Assert.AreEqual(NegativeTestSupport.FirstMessageInfo(signed), NegativeTestSupport.FirstMessageInfo(built));

        Assert.AreEqual(Key(signed), Key(built), NegativeTestSupport.Describe(built));
    }

    [TestMethod]
    public void BuildRequest_AndVerifyResponseSignature_AgreeThatAValidationCodeReachesTheCaller_Test()
    {
        var verified = new Soap12Client().VerifyResponseSignature(
            WsSecurityTestSupport.SignedWire(), (X509Certificate2)null);

        var built = WsSecurityTestSupport.BuildPost(Security());

        Assert.AreEqual(WsSecurityTestSupport.MissingExpectedCertificateCode, Key(verified), NegativeTestSupport.Describe(verified));

        Assert.AreEqual(WsSecurityTestSupport.SigningKeyCode, Key(built), NegativeTestSupport.Describe(built));
    }

    [TestMethod]
    public void BuildRequest_WithACertificateCarryingNoRsaKey_ShouldSurfaceTheSigningKeyCode_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(Security());

        Assert.AreEqual(
            WsSecurityTestSupport.SigningKeyCode,
            NegativeTestSupport.Messages(built)[0].Key, NegativeTestSupport.Describe(built));
    }

    [TestMethod]
    public void Sign_WithACertificateCarryingNoRsaKey_LeaksNoKeyMaterialOnAnyMessageMember_Test()
    {
        var signed = new WsSecurityMessageSigner().Sign(XElement.Parse(EnvelopeXml), Security());

        Assert.IsFalse(signed.IsSuccess, NegativeTestSupport.Describe(signed));

        SecretLeakAssert.CarriesNoSecret(
            signed, "signing failure");

        Assert.IsFalse(
            MessageLeakSweep.Render(signed).Contains(
                _ecdsaCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase),
            MessageLeakSweep.Render(signed));
    }

    [TestMethod]
    public void BuildRequest_WithACertificateCarryingNoRsaKey_LeaksNoKeyMaterialOnAnyMessageMember_Test()
    {
        var built = WsSecurityTestSupport.BuildPost(Security());

        Assert.IsFalse(built.IsSuccess, NegativeTestSupport.Describe(built));

        SecretLeakAssert.CarriesNoSecret(built, "signing failure");

        Assert.IsFalse(
            MessageLeakSweep.Render(built).Contains(
                _ecdsaCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase),
            MessageLeakSweep.Render(built));
    }

    private SoapSecurityDto Security()
        => WsSecurityTestSupport.Security(security => security.SigningCertificate = _ecdsaCertificate);

    private static string Key(IResult result)
        => NegativeTestSupport.Messages(result)[0].Key ?? "<null>";
}
