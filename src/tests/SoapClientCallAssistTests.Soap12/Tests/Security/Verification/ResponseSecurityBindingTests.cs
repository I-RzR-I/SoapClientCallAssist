#nullable disable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Client;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using SoapClientCallAssistTests.Soap12.Helpers.Wire;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Verification;

[TestClass]
public sealed class ResponseSecurityBindingTests
{

    private const string RelatesToId = "rel-1";

    private const string ConfirmationId = "conf-1";

    private const string NoMaterialCode = "V-SEC-030";

    private const string ConsumedCode = "V-SEC-031";

    private const string SymmetricOverloadCode = "V-SEC-032";

    private const string RelatesToCode = "V-SEC-023";

    private const string ConfirmationCode = "V-SEC-040";

    private const string ReflectedNonceCode = "V-SEC-041";

    private const string ReflectedSignatureCode = "V-SEC-042";

    private const string DecryptModeCode = "V-SEC-053";

    private const string UnboundCode = "V-SEC-024";

    [TestMethod]
    public void BuildRequest_WithSecurityEnabled_StoresOpaqueKeyMaterialThatRendersAsAConstantAndExposesNothing_Test()
    {
        using var request = Build(WsSecurityTestSupport.Security(security =>
        {
            security.ExpectedResponseCertificate = WsSecurityTestSupport.OtherCertificate;
            security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken();
        }));

        var material = WsSecurityFoundationTestSupport.KeyMaterialOf(request);

        Assert.IsNotNull(material);
        Assert.AreEqual("RequestKeyMaterial", material.ToString());

        var type = material.GetType();

        Assert.AreEqual(0, type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Length);
        Assert.IsTrue(type.IsSealed && !type.IsPublic);

        var wire = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var signatureValue = Convert.FromBase64String(CertificateScenarioSupport.SignatureValue(wire));
        var nonce = Convert.FromBase64String(WsSecurityFoundationTestSupport.ChildText(
            WsSecurityFoundationTestSupport.UsernameTokenElement(WsSecurityTestSupport.ParseWire(wire)), "Nonce"));

        var json = JsonSerializer.Serialize(request.Options);

        var forbidden = ForbiddenSecret.OfBytes("the request signature value", signatureValue).Renderings()
            .Concat(ForbiddenSecret.OfBytes("the username token nonce", nonce).Renderings())
            .Concat(ForbiddenSecret.OfText("the username token password", WsSecurityFoundationTestSupport.Password).Renderings())
            .Concat(ForbiddenSecret.OfBytes("the expected certificate", WsSecurityTestSupport.OtherCertificate.RawData).Renderings());

        foreach (var (name, value) in forbidden)
        {
            Assert.IsFalse(json.Contains(value, StringComparison.OrdinalIgnoreCase), $"{name} | {value} | {json}");
        }
    }

    [TestMethod]
    public void BuildRequest_WithoutSecurityOrWithACustomSigner_StoresNoKeyMaterial_Test()
    {
        using var plain = Build(null);
        using var custom = WsSecurityTestSupport.BuildPost(WsSecurityTestSupport.Security(security =>
            security.Signer = new PassThroughSigner())).Response;

        Assert.IsNull(WsSecurityFoundationTestSupport.KeyMaterialOf(plain));
        Assert.IsNull(WsSecurityFoundationTestSupport.KeyMaterialOf(custom));
    }

    [TestMethod]
    public void Verify_WithTheSentRequest_AcceptsAResponseSignedWithTheExpectedCertificateAndConsumesTheMaterial_Test()
    {
        using var request = Build(UnboundSecurity());

        var response = WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null);

        var service = new WsSecurityResponseSecurity();

        var coverage = WsSecurityAssert.Accepted(service.Verify(request, response), "");

        Assert.IsTrue(coverage.BodySigned);

        WsSecurityAssert.Rejected(
            service.Verify(request, response),
            ConsumedCode,
            "");
    }

    [TestMethod]
    public void Verify_WithARequestCarryingNoMaterial_RefusesByName_Test()
    {
        using var plain = Build(null);

        var service = new WsSecurityResponseSecurity();

        WsSecurityAssert.Rejected(service.Verify(plain, WsSecurityTestSupport.SignedWire()), NoMaterialCode, "");
        WsSecurityAssert.Rejected(service.Verify(null, WsSecurityTestSupport.SignedWire()), NoMaterialCode, "");
    }

    [TestMethod]
    public void Verify_WithTheSentRequestButNoExpectedCertificateConfigured_RefusesUnderTheExpectedCertificateCode_Test()
    {
        using var request = Build(WsSecurityTestSupport.Security());

        WsSecurityAssert.Rejected(
            new WsSecurityResponseSecurity().Verify(request, WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null)),
            WsSecurityTestSupport.MissingExpectedCertificateCode,
            "");
    }

    [TestMethod]
    public void Verify_AfterAFailedCheck_StillReportsTheMaterialConsumed_Test()
    {
        using var request = Build(UnboundSecurity());

        var service = new WsSecurityResponseSecurity();

        WsSecurityAssert.Rejected(
            service.Verify(request, WsSecurityTestSupport.SignedWire()),
            WsSecurityTestSupport.SignatureVerificationCode,
            "");

        WsSecurityAssert.Rejected(
            service.Verify(request, WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null)),
            ConsumedCode,
            "");
    }

    [TestMethod]
    public void Verify_WithAddressing_RequiresASignedRelatesToEqualToTheRequestMessageId_Test()
    {
        using var request = Build(WsSecurityTestSupport.Security(security =>
        {
            security.ExpectedResponseCertificate = WsSecurityTestSupport.OtherCertificate;
            security.Addressing = WsSecurityFoundationTestSupport.Addressing();
        }));

        var messageId = MessageIdOf(request);

        WsSecurityAssert.Accepted(
            new WsSecurityResponseSecurity().Verify(request, ResponseRelatingTo(messageId, true)),
            "");
    }

    [TestMethod]
    public void Verify_WithAddressing_RefusesAResponseWhoseRelatesToIsMissingUnsignedOrForeign_Test()
    {
        var cases = new (string Name, Func<string, string> Response)[]
        {
            ("missing", _ => WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null)),
            ("unsigned", messageId => ResponseRelatingTo(messageId, false)),
            ("foreign", _ => ResponseRelatingTo("urn:uuid:" + Guid.NewGuid().ToString("D"), true))
        };

        foreach (var (name, response) in cases)
        {
            using var request = Build(WsSecurityTestSupport.Security(security =>
            {
                security.ExpectedResponseCertificate = WsSecurityTestSupport.OtherCertificate;
                security.Addressing = WsSecurityFoundationTestSupport.Addressing();
            }));

            WsSecurityAssert.Rejected(
                new WsSecurityResponseSecurity().Verify(request, response(MessageIdOf(request))),
                RelatesToCode,
                $"{name}");
        }
    }

    [TestMethod]
    public void Verify_WhenSignatureConfirmationIsRequired_AcceptsOnlyASignedConfirmationEqualToOurSignatureValue_Test()
    {
        using var accepted = Build(ConfirmationSecurity());
        var ours = CertificateScenarioSupport.SignatureValue(accepted.Content.ReadAsStringAsync().GetAwaiter().GetResult());

        WsSecurityAssert.Accepted(
            new WsSecurityResponseSecurity().Verify(accepted, ResponseConfirming(ours, true)),
            "");

        var cases = new (string Name, Func<string, string> Response)[]
        {
            ("missing", _ => WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null)),
            ("unsigned", value => ResponseConfirming(value, false)),
            ("foreign", _ => ResponseConfirming(Convert.ToBase64String(new byte[256]), true))
        };

        foreach (var (name, response) in cases)
        {
            using var request = Build(ConfirmationSecurity());
            var value = CertificateScenarioSupport.SignatureValue(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            WsSecurityAssert.Rejected(
                new WsSecurityResponseSecurity().Verify(request, response(value)),
                ConfirmationCode,
                $"{name}");
        }
    }

    [TestMethod]
    public void Verify_WithNeitherAMessageIdNorAConfirmationRequirement_RefusesTheResponseAsUnbindableBeforeAnyVerifierRuns_Test()
    {
        using var request = Build(WsSecurityTestSupport.Security(security =>
            security.ExpectedResponseCertificate = WsSecurityTestSupport.OtherCertificate));

        var registered = new StubSoapMessageVerifier(() => Result<SoapSignatureVerificationResult>.Failure());
        var service = new WsSecurityResponseSecurity(registered);

        WsSecurityAssert.Rejected(
            service.Verify(request, WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null)),
            UnboundCode,
            "");

        Assert.IsFalse(registered.WasCalled);

        WsSecurityAssert.Rejected(
            service.Verify(request, WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null)),
            ConsumedCode,
            "");
    }

    [TestMethod]
    public void Verify_WhenSignatureConfirmationIsLeftAtTheAsymmetricDefault_DoesNotRequireItOnceAnUnboundResponseIsAllowed_Test()
    {
        using var request = Build(UnboundSecurity());

        WsSecurityAssert.Accepted(
            new WsSecurityResponseSecurity().Verify(request, WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null)),
            "");
    }

    [TestMethod]
    public void Verify_WithAddressingButNoMessageIdAndNoConfirmation_RefusesTheResponseAsUnbindable_Test()
    {
        using var request = Build(WsSecurityTestSupport.Security(security =>
        {
            security.ExpectedResponseCertificate = WsSecurityTestSupport.OtherCertificate;
            security.Addressing = WsSecurityFoundationTestSupport.Addressing(addressing => addressing.IncludeMessageId = false);
        }));

        WsSecurityAssert.Rejected(
            new WsSecurityResponseSecurity().Verify(request, WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null)),
            UnboundCode,
            "");
    }

    [TestMethod]
    public void Verify_OnASymmetricRequestWithNoMessageIdAndNoConfirmation_RefusesTheResponseAsUnbindable_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security =>
        {
            security.Addressing.IncludeMessageId = false;
            security.ResponseSecurity = new SoapResponseSecurityDto { RequireSignatureConfirmation = false };
        }));
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        WsSecurityAssert.Rejected(
            new WsSecurityResponseSecurity().Verify(request, new SymmetricResponseBuilder(wire.Secret, wire.EncryptedKeySha1).Build()),
            UnboundCode,
            "");

        using var decrypting = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity(security =>
        {
            security.Addressing.IncludeMessageId = false;
            security.ResponseSecurity = new SoapResponseSecurityDto { RequireSignatureConfirmation = false, AllowDecryption = true };
        }));

        var decrypted = new WsSecurityResponseSecurity().DecryptAndVerify(decrypting, new SymmetricResponseBuilder(wire.Secret, wire.EncryptedKeySha1).Build());

        Assert.IsFalse(decrypted.IsSuccess);
        Assert.AreEqual(UnboundCode, WsSecurityFoundationTestSupport.FirstCode(decrypted));
    }

    [TestMethod]
    public void Verify_WithAResponseReflectingOurNonce_RefusesIt_Test()
    {
        using var request = Build(UnboundSecurity(security =>
            security.UsernameToken = WsSecurityFoundationTestSupport.UsernameToken()));

        var nonce = WsSecurityFoundationTestSupport.ChildText(
            WsSecurityFoundationTestSupport.UsernameTokenElement(WsSecurityTestSupport.ParseWire(request.Content.ReadAsStringAsync().GetAwaiter().GetResult())), "Nonce");

        var reflecting = WsSecurityWireMutator.ReplaceOnce(
            WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null),
            "</wsse:Security>",
            $"<wsse:Nonce>{nonce}</wsse:Nonce></wsse:Security>");

        WsSecurityAssert.Rejected(
            new WsSecurityResponseSecurity().Verify(request, reflecting),
            ReflectedNonceCode,
            "");
    }

    [TestMethod]
    public void ReflectedSignatureValue_IsRefusedByTheBindingHelper_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire();
        var ours = Convert.FromBase64String(CertificateScenarioSupport.SignatureValue(wire));

        var document = WsSecurityTestSupport.ParseWire(wire);
        var signature = WsSecurityTestSupport.RequireNode(document, "//ds:Signature", WsSecurityTestSupport.Namespaces(document), "signature");

        var signedXmlType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.WsuSignedXml");
        var signedXml = (System.Security.Cryptography.Xml.SignedXml)Activator.CreateInstance(
            signedXmlType, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new object[] { document }, null);
        signedXml.LoadXml(signature);

        var helper = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.WsSecurityResponseBinding");
        var refuse = helper.GetMethod("RefuseReflectedSignatureValue", BindingFlags.Static | BindingFlags.NonPublic);

        var reflected = (IResult)refuse.Invoke(null, new object[] { signedXml, ours });
        var distinct = (IResult)refuse.Invoke(null, new object[] { signedXml, new byte[ours.Length] });

        Assert.IsFalse(reflected.IsSuccess);
        Assert.AreEqual(ReflectedSignatureCode, WsSecurityFoundationTestSupport.FirstCode(reflected));
        Assert.IsTrue(distinct.IsSuccess);
    }

    [TestMethod]
    public void FixedTimeEquals_ComparesWholeArraysAndNeverThrows_Test()
    {
        var helper = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.WsSecurityResponseBinding");
        var equals = helper.GetMethod("FixedTimeEquals", BindingFlags.Static | BindingFlags.NonPublic);

        bool Compare(byte[] left, byte[] right) => (bool)equals.Invoke(null, new object[] { left, right });

        Assert.IsTrue(Compare(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 }));
        Assert.IsFalse(Compare(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 4 }));
        Assert.IsFalse(Compare(new byte[] { 1, 2, 3 }, new byte[] { 1, 2 }));
        Assert.IsFalse(Compare(null, new byte[] { 1 }));
        Assert.IsFalse(Compare(new byte[] { 1 }, null));
        Assert.IsFalse(Compare(null, null));
    }

    [TestMethod]
    public void Decrypt_OnAnAsymmetricRequest_RefusesByNameAndConsumesTheMaterial_Test()
    {
        using var request = Build(WsSecurityTestSupport.Security(security =>
            security.ExpectedResponseCertificate = WsSecurityTestSupport.OtherCertificate));

        var service = new WsSecurityResponseSecurity();

        var decrypted = service.Decrypt(request, WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null));

        Assert.IsFalse(decrypted.IsSuccess);
        Assert.AreEqual(DecryptModeCode, WsSecurityFoundationTestSupport.FirstCode(decrypted));

        WsSecurityAssert.Rejected(
            service.Verify(request, WsSecurityFoundationTestSupport.SignedResponse(WsSecurityTestSupport.OtherCertificate, null)),
            ConsumedCode,
            "");
    }

    [TestMethod]
    public void Verify_WithTheOptionsOverloadOnASymmetricBinding_RefusesUnderTheDedicatedCode_Test()
    {
        var security = WsSecurityFoundationTestSupport.SymmetricSecurity();

        WsSecurityAssert.Rejected(
            new WsSecurityResponseSecurity().Verify(WsSecurityTestSupport.SignedWire(), security),
            SymmetricOverloadCode,
            "");

        WsSecurityAssert.Rejected(
            new Soap12Client().VerifyResponseSignature(WsSecurityTestSupport.SignedWire(), security),
            SymmetricOverloadCode,
            "");
    }

    [TestMethod]
    public void RegisterSoapClientsEndpoint_RegistersTheResponseSecurityServiceAsASingletonHonouringTheRegisteredVerifier_Test()
    {
        var verifier = new StubSoapMessageVerifier(() => null);
        var services = new ServiceCollection();
        services.AddSingleton<ISoapMessageVerifier>(verifier);
        services.RegisterSoapClientsEndpoint();

        using var provider = services.BuildServiceProvider();

        var service = provider.GetRequiredService<ISoapResponseSecurity>();

        Assert.IsInstanceOfType<WsSecurityResponseSecurity>(service);
        Assert.AreSame(service, provider.GetRequiredService<ISoapResponseSecurity>());

        service.Verify(WsSecurityTestSupport.SignedWire(), WsSecurityTestSupport.SigningCertificate, null);

        Assert.IsTrue(verifier.WasCalled);
    }

    [TestMethod]
    public void RegisterSoapClientsEndpoint_ResolvesBothClientsThroughTheRegisteredResponseSecurityService_Test()
    {
        var stub = new StubResponseSecurity();
        var services = new ServiceCollection();
        services.AddSingleton<ISoapResponseSecurity>(stub);
        services.RegisterSoapClientsEndpoint();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<Soap11Client>().VerifyResponseSignature(WsSecurityTestSupport.SignedWire(), WsSecurityTestSupport.SigningCertificate);
        provider.GetRequiredService<Soap12Client>().VerifyResponseSignature(WsSecurityTestSupport.SignedWire(), new SoapSecurityDto());

        Assert.AreEqual(2, stub.Calls);
    }

    private static HttpRequestMessage Build(SoapSecurityDto security)
    {
        var built = WsSecurityTestSupport.BuildPost(security);

        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));

        return built.Response;
    }

    private static SoapSecurityDto ConfirmationSecurity()
        => WsSecurityTestSupport.Security(security =>
        {
            security.ExpectedResponseCertificate = WsSecurityTestSupport.OtherCertificate;
            security.ResponseSecurity = new SoapResponseSecurityDto { RequireSignatureConfirmation = true };
        });

    private static SoapSecurityDto UnboundSecurity(Action<SoapSecurityDto> configure = null)
        => WsSecurityTestSupport.Security(security =>
        {
            security.ExpectedResponseCertificate = WsSecurityTestSupport.OtherCertificate;
            security.ResponseSecurity = new SoapResponseSecurityDto { AllowUnboundResponse = true };
            configure?.Invoke(security);
        });

    private static string MessageIdOf(HttpRequestMessage request)
    {
        var document = WsSecurityTestSupport.ParseWire(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        var header = WsSecurityFoundationTestSupport.HeaderChildren(document).Single(element => element.LocalName == "MessageID");

        return header.InnerText;
    }

    private static string ResponseRelatingTo(string messageId, bool signed)
        => WsSecurityFoundationTestSupport.SignedResponse(
            WsSecurityTestSupport.OtherCertificate,
            new[] { WsSecurityFoundationTestSupport.RelatesToHeader(messageId, RelatesToId) },
            signed ? new[] { RelatesToId } : Array.Empty<string>());

    private static string ResponseConfirming(string value, bool signed)
        => WsSecurityFoundationTestSupport.SignedResponse(
            WsSecurityTestSupport.OtherCertificate,
            new[] { WsSecurityFoundationTestSupport.SignatureConfirmationHeader(value, ConfirmationId) },
            signed ? new[] { ConfirmationId } : Array.Empty<string>());

    private sealed class PassThroughSigner : ISoapMessageSigner
    {
        public IResult<string> Sign(XElement soapEnvelope, SoapSecurityDto options)
            => Result<string>.Success(soapEnvelope.ToString(SaveOptions.DisableFormatting));
    }

    private sealed class StubResponseSecurity : ISoapResponseSecurity
    {
        public int Calls { get; private set; }

        public IResult<SoapSignatureVerificationResult> Verify(HttpRequestMessage sentRequest, string soapResponse) => Count();

        public IResult<SoapSignatureVerificationResult> Verify(string soapResponse, System.Security.Cryptography.X509Certificates.X509Certificate2 expectedCertificate, SoapVerificationPolicyDto policy) => Count();

        public IResult<SoapSignatureVerificationResult> Verify(string soapResponse, SoapSecurityDto security) => Count();

        public IResult<string> DecryptAndVerify(HttpRequestMessage sentRequest, string soapResponse) => Result<string>.Failure();

        public IResult<string> Decrypt(HttpRequestMessage sentRequest, string soapResponse) => Result<string>.Failure();

        private IResult<SoapSignatureVerificationResult> Count()
        {
            Calls++;

            return Result<SoapSignatureVerificationResult>.Failure();
        }
    }
}
