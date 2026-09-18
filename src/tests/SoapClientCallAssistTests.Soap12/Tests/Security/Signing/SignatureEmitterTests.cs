#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class SignatureEmitterTests
{

    private const string RsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

    private const string HmacSha256 = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256";

    private const string Sha256 = "http://www.w3.org/2001/04/xmlenc#sha256";

    private const string PrimaryId = "sig-primary";

    private const string SpecFailureCode = "ER-SEC-SGN";

    private static readonly Type EmitterType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.SignatureEmitter");

    private static readonly Type SpecType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.SignatureSpec");

    private static readonly Type FamilyType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Enums.SignatureKeyFamily");

    private XmlDocument _document;

    private XmlElement _security;

    private string _bodyId;

    [TestInitialize]
    public void Initialize()
    {
        _document = WsSecurityTestSupport.ParseWire(WsSecurityTestSupport.Wire(WsSecurityTestSupport.BuildPost(null), "Build"));

        var body = WsSecurityTestSupport.RequireNode(_document, "/*/*[local-name()='Body']", WsSecurityTestSupport.Namespaces(_document), "Body");
        var header = _document.CreateElement("soap", "Header", _document.DocumentElement.NamespaceURI);
        _document.DocumentElement.PrependChild(header);

        _security = _document.CreateElement("wsse", "Security", WsSecurityTestSupport.WsseNamespace);
        _security.SetAttribute("xmlns:wsu", WsSecurityTestSupport.WsuNamespace);
        header.AppendChild(_security);

        _bodyId = "body-" + Guid.NewGuid().ToString("N");
        var id = _document.CreateAttribute("wsu", "Id", WsSecurityTestSupport.WsuNamespace);
        id.Value = _bodyId;
        body.Attributes.Append(id);
    }

    [TestMethod]
    public void Emit_AnEndorsingSignatureOverThePrimary_ResolvesThePlainIdAndVerifiesIndependently_Test()
    {
        using var rsa = WsSecurityTestSupport.SigningCertificate.GetRSAPrivateKey();

        var primary = Emit(Spec("Rsa", RsaSha256, new[] { _bodyId }, spec =>
        {
            Set(spec, "RsaKey", rsa);
            Set(spec, "Id", PrimaryId);
        }));

        var primaryElement = Element(primary);

        Assert.AreEqual(PrimaryId, primaryElement.GetAttribute("Id"));

        var endorsing = Emit(Spec("Rsa", RsaSha256, new[] { PrimaryId }, spec => Set(spec, "RsaKey", rsa)));

        Assert.AreNotEqual(Convert.ToBase64String(SignatureValue(primary)), Convert.ToBase64String(SignatureValue(endorsing)));

        using var publicKey = WsSecurityTestSupport.SigningCertificate.GetRSAPublicKey();

        Assert.IsTrue(VerifiesWith(primaryElement, signedXml => signedXml.CheckSignature(publicKey)));
        Assert.IsTrue(VerifiesWith(Element(endorsing), signedXml => signedXml.CheckSignature(publicKey), true));
    }

    [TestMethod]
    public void Emit_AnHmacSignature_VerifiesWithTheSameKeyAndFailsWithAnother_Test()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var other = RandomNumberGenerator.GetBytes(32);

        var emitted = Emit(Spec("Hmac", HmacSha256, new[] { _bodyId }, spec => Set(spec, "HmacKey", (byte[])key.Clone())));
        var element = Element(emitted);

        var method = WsSecurityTestSupport.RequireNode(element, "ds:SignedInfo/ds:SignatureMethod", WsSecurityTestSupport.Namespaces(_document), "signature method");

        Assert.AreEqual(HmacSha256, method.GetAttribute("Algorithm"));
        Assert.IsNull(method.SelectSingleNode("ds:HMACOutputLength", WsSecurityTestSupport.Namespaces(_document)));

        Assert.IsTrue(VerifiesWith(element, signedXml => signedXml.CheckSignature(new HMACSHA256(key))));
        Assert.IsFalse(VerifiesWith(element, signedXml => signedXml.CheckSignature(new HMACSHA256(other))));
    }

    [TestMethod]
    public void Emit_WithAKeyFamilyContradictingTheMethodOrKey_RefusesTheSpec_Test()
    {
        using var rsa = WsSecurityTestSupport.SigningCertificate.GetRSAPrivateKey();

        var cases = new (string Name, object Spec)[]
        {
            ("RSA family with an HMAC method", Spec("Rsa", HmacSha256, new[] { _bodyId }, spec => Set(spec, "RsaKey", rsa))),
            ("HMAC family with an RSA method", Spec("Hmac", RsaSha256, new[] { _bodyId }, spec => Set(spec, "HmacKey", new byte[32]))),
            ("HMAC family with a truncation", Spec("Hmac", HmacSha256, new[] { _bodyId }, spec =>
            {
                Set(spec, "HmacKey", new byte[32]);
                Set(spec, "HmacOutputLength", 80);
            })),
            ("RSA family without a key", Spec("Rsa", RsaSha256, new[] { _bodyId }, null)),
            ("HMAC family with an RSA key attached", Spec("Hmac", HmacSha256, new[] { _bodyId }, spec =>
            {
                Set(spec, "HmacKey", new byte[32]);
                Set(spec, "RsaKey", rsa);
            })),
            ("no references", Spec("Rsa", RsaSha256, Array.Empty<string>(), spec => Set(spec, "RsaKey", rsa)))
        };

        foreach (var (name, spec) in cases)
        {
            var result = Invoke(spec);

            Assert.IsFalse(result.IsSuccess, $"{name}");
            Assert.AreEqual(SpecFailureCode, WsSecurityFoundationTestSupport.FirstCode(result), $"{name}");
        }

        Assert.AreEqual(0, _security.ChildNodes.Count);
    }

    private static object Spec(string family, string method, IReadOnlyList<string> references, Action<object> configure)
    {
        var spec = Activator.CreateInstance(
            SpecType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            new object[] { Enum.Parse(FamilyType, family), method, Sha256, SoapCanonicalizationType.ExclusiveC14N, references },
            null);

        configure?.Invoke(spec);

        return spec;
    }

    private static void Set(object spec, string property, object value)
        => SpecType.GetProperty(property, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(spec, value);

    private IResult Invoke(object spec)
        => (IResult)EmitterType.GetMethod("Emit", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { _document, _security, spec });

    private object Emit(object spec)
    {
        var result = Invoke(spec);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));

        return result.GetType().GetProperty("Response").GetValue(result);
    }

    private static XmlElement Element(object emitted)
        => (XmlElement)emitted.GetType().GetProperty("Element", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(emitted);

    private static byte[] SignatureValue(object emitted)
        => (byte[])emitted.GetType().GetProperty("SignatureValue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(emitted);

    private bool VerifiesWith(XmlElement signature, Func<SignedXml, bool> check, bool plainSignedXml = false)
    {
        var signedXml = plainSignedXml
            ? new SignedXml(_document)
            : (SignedXml)Activator.CreateInstance(
                WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.WsuSignedXml"),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new object[] { _document },
                null);

        signedXml.Resolver = null;
        signedXml.LoadXml(signature);

        return check(signedXml);
    }
}
