#nullable disable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Client;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using SoapClientCallAssistTests.Soap12.Helpers.Wire;
using SoapClientCallAssistTests.Soap12.Tests;
using SoapClientCallAssistTests.Soap12.Tests.Protocol;
using System;
using System.Security.Cryptography.X509Certificates;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Verification;

[TestClass]
public sealed class ResponseVerificationTests
{
    private const string StubSecret = "stub-secret-value";

    private const string SameKeyCode = "V-SEC-015";

    private const string DepthCode = "ER-XML-DEPTH";

    private const int DepthCapPlusOne = 65;

    private Soap12Client _client;

    private string _signed;

    [TestInitialize]
    public void Initialize()
    {
        _client = new Soap12Client();
        _signed = WsSecurityTestSupport.SignedWire();
    }

    [TestMethod]
    public void VerifyResponseSignature_WithTheExpectedCertificate_AcceptsTheSignedResponse_Test()
    {
        var coverage = WsSecurityAssert.Accepted(
            _client.VerifyResponseSignature(_signed, WsSecurityTestSupport.SigningCertificate),
            "");

        Assert.IsTrue(coverage.BodySigned);
        Assert.IsTrue(coverage.TimestampSigned);
    }

    [TestMethod]
    public void VerifyResponseSignature_FromASoap11Client_AcceptsTheSignedResponse_Test()
        => WsSecurityAssert.Accepted(
            new Soap11Client().VerifyResponseSignature(_signed, WsSecurityTestSupport.SigningCertificate),
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithATamperedBody_RejectsTheResponse_Test()
        => WsSecurityAssert.Rejected(
            _client.VerifyResponseSignature(
                WsSecurityWireMutator.WithTamperedBody(_signed), WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.SignatureVerificationCode,
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithAnUnsignedResponse_RejectsIt_Test()
        => WsSecurityAssert.Rejected(
            _client.VerifyResponseSignature(UnsignedWire(), WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.SignatureCountCode,
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithAnUnrelatedCertificate_RejectsTheResponse_Test()
        => WsSecurityAssert.Rejected(
            _client.VerifyResponseSignature(_signed, WsSecurityTestSupport.OtherCertificate),
            WsSecurityTestSupport.SignatureVerificationCode,
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithNoExpectedCertificate_Fails_Test()
        => WsSecurityAssert.Rejected(
            _client.VerifyResponseSignature(_signed, (X509Certificate2)null),
            WsSecurityTestSupport.MissingExpectedCertificateCode,
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithSecurityOptionsCarryingTheExpectedCertificate_AcceptsTheResponse_Test()
        => WsSecurityAssert.Accepted(
            _client.VerifyResponseSignature(
                _signed,
                new SoapSecurityDto { ExpectedResponseCertificate = WsSecurityTestSupport.SigningCertificate }),
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithSecurityOptionsNamingNoCertificate_Fails_Test()
        => WsSecurityAssert.Rejected(
            _client.VerifyResponseSignature(_signed, new SoapSecurityDto()),
            WsSecurityTestSupport.MissingExpectedCertificateCode,
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithNullSecurityOptions_Fails_Test()
        => WsSecurityAssert.Rejected(
            _client.VerifyResponseSignature(_signed, (SoapSecurityDto)null),
            WsSecurityTestSupport.MissingSecurityOptionsCode,
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithAnUnsignedTimestamp_FailsUnderTheStrictDefault_Test()
        => WsSecurityAssert.Rejected(
            _client.VerifyResponseSignature(WithoutTimestamp(), WsSecurityTestSupport.SigningCertificate),
            WsSecurityTestSupport.TimestampNotSignedCode,
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithAPolicyRelaxingTheTimestamp_AcceptsTheResponse_Test()
        => WsSecurityAssert.Accepted(
            _client.VerifyResponseSignature(
                WithoutTimestamp(),
                WsSecurityTestSupport.SigningCertificate,
                new SoapVerificationPolicyDto { RequireValidTimestamp = false }),
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithAPolicyOnTheSecurityOptions_AppliesThatPolicy_Test()
        => WsSecurityAssert.Accepted(
            _client.VerifyResponseSignature(
                WithoutTimestamp(),
                new SoapSecurityDto
                {
                    ExpectedResponseCertificate = WsSecurityTestSupport.SigningCertificate,
                    ResponseVerificationPolicy = new SoapVerificationPolicyDto { RequireValidTimestamp = false }
                }),
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithACustomVerifier_UsesIt_Test()
    {
        var verifier = new StubSoapMessageVerifier(() => null);

        _client.VerifyResponseSignature(_signed, WithVerifier(verifier));

        Assert.IsTrue(verifier.WasCalled);
    }

    [TestMethod]
    public void VerifyResponseSignature_WhenACustomVerifierReturnsNothing_ReportsFailure_Test()
        => WsSecurityAssert.Rejected(
            _client.VerifyResponseSignature(_signed, WithVerifier(new StubSoapMessageVerifier(() => null))),
            WsSecurityTestSupport.VerifierFailureCode,
            "");

    [TestMethod]
    public void VerifyResponseSignature_WhenACustomVerifierThrows_ReportsFailureInsteadOfThrowing_Test()
        => WsSecurityAssert.Rejected(
            _client.VerifyResponseSignature(_signed, WithVerifier(Throwing())),
            WsSecurityTestSupport.VerifierFailureCode,
            "");

    [TestMethod]
    public void VerifyResponseSignature_WhenACustomVerifierThrows_DoesNotLeakTheExceptionText_Test()
    {
        var result = _client.VerifyResponseSignature(_signed, WithVerifier(Throwing()));

        Assert.IsFalse(result.IsSuccess);

        var reported = NegativeTestSupport.Describe(result);

        Assert.IsFalse(reported.Contains(StubSecret, StringComparison.Ordinal), reported);
    }

    [TestMethod]
    public void RegisterSoapClientsEndpoint_RegistersTheMessageVerifier_Test()
    {
        var services = new ServiceCollection();
        services.RegisterSoapClientsEndpoint();

        using var provider = services.BuildServiceProvider();

        var verifier = provider.GetRequiredService<ISoapMessageVerifier>();

        Assert.IsInstanceOfType<WsSecurityMessageVerifier>(verifier);

        Assert.AreSame(verifier, provider.GetRequiredService<ISoapMessageVerifier>());
    }

    [TestMethod]
    public void VerifyResponseSignature_WithAVerifierRegisteredInTheContainer_UsesItForTheOptionsOverload_Test()
    {
        var verifier = new StubSoapMessageVerifier(() => null);
        var client = ClientResolvedWith(verifier);

        client.VerifyResponseSignature(
            _signed,
            new SoapSecurityDto { ExpectedResponseCertificate = WsSecurityTestSupport.SigningCertificate });

        Assert.IsTrue(verifier.WasCalled);
    }

    [TestMethod]
    public void VerifyResponseSignature_WithAVerifierRegisteredInTheContainer_UsesItForTheCertificateOverload_Test()
    {
        var verifier = new StubSoapMessageVerifier(() => null);
        var client = ClientResolvedWith(verifier);

        client.VerifyResponseSignature(_signed, WsSecurityTestSupport.SigningCertificate);

        Assert.IsTrue(verifier.WasCalled);
    }

    [TestMethod]
    public void VerifyResponseSignature_WithAVerifierRegisteredInTheContainer_ReachesTheSoap11ClientToo_Test()
    {
        var verifier = new StubSoapMessageVerifier(() => null);
        var services = new ServiceCollection();
        services.AddSingleton<ISoapMessageVerifier>(verifier);
        services.RegisterSoapClientsEndpoint();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<Soap11Client>().VerifyResponseSignature(_signed, WsSecurityTestSupport.SigningCertificate);

        Assert.IsTrue(verifier.WasCalled);
    }

    [TestMethod]
    public void VerifyResponseSignature_WhenTheOptionsNameTheirOwnVerifier_ItWinsOverTheRegisteredOne_Test()
    {
        var registered = new StubSoapMessageVerifier(() => null);
        var onOptions = new StubSoapMessageVerifier(() => null);
        var client = ClientResolvedWith(registered);

        client.VerifyResponseSignature(_signed, WithVerifier(onOptions));

        Assert.IsTrue(onOptions.WasCalled);
        Assert.IsFalse(registered.WasCalled);
    }

    [TestMethod]
    public void VerifyResponseSignature_WithNoVerifierRegistered_StillVerifiesWithTheLibraryImplementation_Test()
        => WsSecurityAssert.Accepted(
            new Soap12Client(null, null).VerifyResponseSignature(_signed, WsSecurityTestSupport.SigningCertificate),
            "");

    [TestMethod]
    public void VerifyResponseSignature_WhenTheExpectedCertificateIsTheSigningCertificate_RefusesToVerify_Test()
    {
        var verifier = new StubSoapMessageVerifier(() => null);

        var result = _client.VerifyResponseSignature(
            _signed,
            new SoapSecurityDto
            {
                SigningCertificate = WsSecurityTestSupport.SigningCertificate,
                ExpectedResponseCertificate = WsSecurityTestSupport.SigningCertificate,
                ResponseVerifier = verifier
            });

        WsSecurityAssert.Rejected(
            result,
            SameKeyCode,
            "");

        Assert.IsFalse(verifier.WasCalled);
    }

    [TestMethod]
    public void VerifyResponseSignature_WhenTheExpectedCertificateSharesOnlyThePublicKey_RefusesToVerify_Test()
        => WsSecurityAssert.Rejected(
            _client.VerifyResponseSignature(
                _signed,
                new SoapSecurityDto
                {
                    SigningCertificate = WsSecurityTestSupport.SigningCertificate,
                    ExpectedResponseCertificate = WsSecurityTestSupport.PublicOnlyCertificate
                }),
            SameKeyCode,
            "");

    [TestMethod]
    public void VerifyResponseSignature_WhenTheSigningAndExpectedCertificatesDiffer_StillVerifies_Test()
        => WsSecurityAssert.Accepted(
            _client.VerifyResponseSignature(
                _signed,
                new SoapSecurityDto
                {
                    SigningCertificate = WsSecurityTestSupport.OtherCertificate,
                    ExpectedResponseCertificate = WsSecurityTestSupport.SigningCertificate
                }),
            "");

    [TestMethod]
    public void VerifyResponseSignature_WithAResponseNestedPastTheDepthCap_FailsWithTheDepthCodeBeforeAnySignatureWork_Test()
    {
        var result = _client.VerifyResponseSignature(
            ResponseHardeningTests.NestedEnvelope(DepthCapPlusOne),
            WsSecurityTestSupport.SigningCertificate);

        WsSecurityAssert.Rejected(
            result,
            DepthCode,
            "");
    }

    private static Soap12Client ClientResolvedWith(ISoapMessageVerifier verifier)
    {
        var services = new ServiceCollection();
        services.AddSingleton(verifier);
        services.RegisterSoapClientsEndpoint();

        var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<Soap12Client>();
    }

    private static SoapSecurityDto WithVerifier(ISoapMessageVerifier verifier)
        => new()
        {
            ExpectedResponseCertificate = WsSecurityTestSupport.SigningCertificate,
            ResponseVerifier = verifier
        };

    private static StubSoapMessageVerifier Throwing()
        => new(() => throw new InvalidOperationException(StubSecret));

    private static string UnsignedWire()
        => WsSecurityTestSupport.Wire(WsSecurityTestSupport.BuildPost(null), "Build");

    private static string WithoutTimestamp()
        => WsSecurityTestSupport.SignedWire(
            WsSecurityTestSupport.Security(security =>
            {
                security.IncludeTimestamp = false;
                security.SignTimestamp = false;
            }));
}
