#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Hygiene;

[TestClass]
public sealed class KeyMaterialZeroingTests
{

    private const string HeaderRefusalCode = "V-BEC-HDR-001";

    [TestMethod]
    public void Build_FailingOnTheEndorsingSignatureAfterTheEncryptedKeyWasEmitted_LeavesTheKeySourceSecretZeroed_Test()
    {
        var document = KeyMaterialReflection.Envelope();
        var plan = KeyMaterialReflection.Plan(SymmetricTestSupport.CertificateSecurity());

        using var builder = KeyMaterialReflection.Builder(document, plan, null);

        var built = KeyMaterialReflection.Build(builder);

        Assert.IsFalse(built.IsSuccess);

        var encryptedKeys = document.GetElementsByTagName("EncryptedKey", SymmetricTestSupport.XencNamespace);
        Assert.AreEqual(1, encryptedKeys.Count);

        var wrapped = Convert.FromBase64String(((XmlElement)encryptedKeys[0]).GetElementsByTagName("CipherValue", SymmetricTestSupport.XencNamespace)[0].InnerText);
        var secretOnTheWire = SymmetricTestSupport.UnwrapSecret(wrapped, WsSecurityFoundationTestSupport.ServiceCertificate);

        Assert.IsTrue(secretOnTheWire.Any(value => value != 0));

        var keySource = KeyMaterialReflection.KeySourceOf(builder);
        Assert.IsNotNull(keySource);

        var secret = KeyMaterialReflection.SecretOf(keySource);
        Assert.IsNotNull(secret);
        Assert.AreEqual(secretOnTheWire.Length, secret.Length);
        KeyMaterialReflection.AssertAllZero(new[] { ("_secret", secret) }, "build");
    }

    [TestMethod]
    public void Build_Succeeding_HandsTheSecretToTheResultAndLeavesTheKeySourceHoldingNone_Test()
    {
        var document = KeyMaterialReflection.Envelope();
        var plan = KeyMaterialReflection.Plan(SymmetricTestSupport.UserNameSecurity());

        using var builder = KeyMaterialReflection.Builder(document, plan, null);

        var built = KeyMaterialReflection.Build(builder);
        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));

        using var result = (IDisposable)KeyMaterialReflection.Response(built);

        Assert.IsNull(KeyMaterialReflection.SecretOf(KeyMaterialReflection.KeySourceOf(builder)));
        Assert.IsTrue((bool)KeyMaterialReflection.BuildResultType.GetProperty("HasSessionSecret", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(result));
    }

    [TestMethod]
    public void BuildResult_Disposed_ZeroesTheSecretNobodyTook_Test()
    {
        var document = KeyMaterialReflection.Envelope();
        var plan = KeyMaterialReflection.Plan(SymmetricTestSupport.UserNameSecurity());

        using var builder = KeyMaterialReflection.Builder(document, plan, null);

        var built = KeyMaterialReflection.Build(builder);
        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));

        var result = (IDisposable)KeyMaterialReflection.Response(built);
        var secret = (byte[])KeyMaterialReflection.Field(result, "_sessionSecret");

        Assert.IsNotNull(secret);
        Assert.IsTrue(secret.Any(value => value != 0));

        result.Dispose();

        KeyMaterialReflection.AssertAllZero(new[] { ("_sessionSecret", secret) }, "Dispose");
    }

    [TestMethod]
    public void Snapshot_TakesTheSecretFromTheResult_SoDisposingTheResultZeroesNothingTheMaterialHolds_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
        var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

        var material = KeyMaterialReflection.MaterialOf(request);
        var secret = (byte[])KeyMaterialReflection.Field(material, "_sessionSecret");

        CollectionAssert.AreEqual(wire.Secret, secret);
    }

    [TestMethod]
    public void Consume_ThenDispose_ZeroesEverySecretBearingArrayOfTheSymmetricMaterial_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.CertificateSecurity());

        var material = KeyMaterialReflection.MaterialOf(request);
        var before = KeyMaterialReflection.SecretArraysOf(material);

        Assert.IsTrue(before.Any(entry => entry.Name == "_sessionSecret") && before.Any(entry => entry.Name.StartsWith("_requestDerivedKeys[")));

        var consumed = KeyMaterialReflection.Consume(material);
        var held = KeyMaterialReflection.SecretArraysOf(consumed);

        Assert.AreEqual(before.Count, held.Count);
        Assert.AreEqual(0, KeyMaterialReflection.SecretArraysOf(material).Count);

        consumed.Dispose();

        KeyMaterialReflection.AssertAllZero(before, "Dispose");
        KeyMaterialReflection.AssertAllZero(held, "Dispose");
        Assert.AreEqual(0, KeyMaterialReflection.SecretArraysOf(consumed).Count);
    }

    [TestMethod]
    public void Consume_ThenDispose_ZeroesEverySecretBearingArrayOfTheAsymmetricMaterial_Test()
    {
        using var request = WsSecurityTestSupport.BuildPost(WsSecurityTestSupport.Security(security =>
            security.ExpectedResponseCertificate = WsSecurityTestSupport.OtherCertificate)).Response;

        var material = KeyMaterialReflection.MaterialOf(request);
        var before = KeyMaterialReflection.SecretArraysOf(material);

        CollectionAssert.AreEquivalent(new[] { "_expectedCertificate", "_signatureValue" }, before.Select(entry => entry.Name).ToList());

        using (KeyMaterialReflection.Consume(material))
        {
        }

        KeyMaterialReflection.AssertAllZero(before, "Dispose");
    }

    [TestMethod]
    public void Verify_ThroughTheService_LeavesEveryArrayTheMaterialHeldZeroedWhetherItSucceededOrFailed_Test()
    {
        foreach (var succeed in new[] { true, false })
        {
            using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.UserNameSecurity());
            var wire = SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

            var before = KeyMaterialReflection.SecretArraysOf(KeyMaterialReflection.MaterialOf(request));

            var response = new SymmetricResponseBuilder(wire.Secret, wire.EncryptedKeySha1).RelatesTo(wire.MessageId).Confirm(wire.PrimarySignatureValue);
            var verified = new WsSecurityResponseSecurity().Verify(request, succeed ? response.Build() : response.ThenMutate(text => text.Replace("answer", "tampered")).Build());

            Assert.AreEqual(succeed, verified.IsSuccess, $"{(succeed ? "pass" : "fail")} | {NegativeTestSupport.Describe(verified)}");

            KeyMaterialReflection.AssertAllZero(before, $"{(succeed ? "successful" : "failed")}");
        }
    }

    [TestMethod]
    public void RequestMaterial_Disposed_ZeroesEveryArrayAndCountsAsConsumed_Test()
    {
        using var request = SymmetricTestSupport.BuildRequest(SymmetricTestSupport.CertificateSecurity());

        var material = KeyMaterialReflection.MaterialOf(request);
        var before = KeyMaterialReflection.SecretArraysOf(material);

        ((IDisposable)material).Dispose();

        KeyMaterialReflection.AssertAllZero(before, "Dispose");
        Assert.IsFalse(KeyMaterialReflection.TryConsume(material));

        WsSecurityAssert.Rejected(new WsSecurityResponseSecurity().Verify(request, WsSecurityTestSupport.SignedWire()), SymmetricTestSupport.ConsumedCode, "");
    }

    [TestMethod]
    public void BuildRequest_FailingOnAClientHeaderAfterSigning_HandsBackNoRequestAndAttachesNoMaterial_Test()
    {
        var built = WsSecurityTestSupport.Client(SoapClientCallAssist.Enums.SoapProtocolType.SOAP_1_2).BuildRequest(
            HttpMethod.Post,
            new BuildSoapRequestDto
            {
                Client = new HttpClientDto(WsSecurityTestSupport.Endpoint) { HttpClientHeaders = new Dictionary<string, IEnumerable<string>> { ["X-Broken"] = null } },
                Envelope = new SoapEnvelopeDto(new[] { WsSecurityTestSupport.DefaultBody() }, null, WsSecurityTestSupport.Action),
                Security = SymmetricTestSupport.UserNameSecurity()
            });

        WsSecurityFoundationTestSupport.RefusedWith(built, HeaderRefusalCode, "");
        Assert.IsNull(built.Response);
    }

    [TestMethod]
    public void KeySource_Disposed_ZeroesTheSecretInPlaceAndDerivesNothingAfterwards_Test()
    {
        var secret = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        var source = (IDisposable)Activator.CreateInstance(
            KeyMaterialReflection.KeySourceType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            new object[] { secret, null, SoapClientCallAssist.Enums.SoapSecureConversationVersionType.February2005, null, 24, 32 },
            null);

        source.GetType().GetProperty("SignatureNonce", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(source, new byte[16]);

        source.Dispose();
        source.Dispose();

        KeyMaterialReflection.AssertAllZero(new[] { ("_secret", secret) }, "Dispose");
        Assert.IsFalse((bool)source.GetType().GetProperty("HasSecret", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(source));
        Assert.IsNull(source.GetType().GetMethod("DeriveSignatureKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(source, Array.Empty<object>()));
        Assert.IsNull(source.GetType().GetMethod("TakeSecret", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(source, Array.Empty<object>()));
    }
}
