#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal static class TierBTestSupport
{

    internal const string ContentType = "http://www.w3.org/2001/04/xmlenc#Content";

    internal const string ElementType = "http://www.w3.org/2001/04/xmlenc#Element";

    internal const string Aes128Cbc = "http://www.w3.org/2001/04/xmlenc#aes128-cbc";

    internal const string Aes256Gcm = "http://www.w3.org/2009/xmlenc11#aes256-gcm";

    internal const string TripleDesCbc = "http://www.w3.org/2001/04/xmlenc#tripledes-cbc";

    internal const string DecryptionCode = "ER-SEC-DEC";

    internal const string CipherCapCode = "V-SEC-054";

    internal const string RecipientCertificateCode = "V-SEC-057";

    internal const string DecryptionNotAllowedCode = "V-SEC-058";

    internal const string EncryptedShapeCode = "V-SEC-059";

    internal const string TokenUnreadableCode = "V-SEC-046";

    internal const string TokenForeignCode = "V-SEC-047";

    internal const string ConsumedCode = "V-SEC-031";

    internal const string Canary = "canary-plaintext-7f3a9c1e-never-in-a-message";

    internal static SoapSecurityDto Security(Action<SoapSecurityDto> configure = null)
        => SymmetricTestSupport.UserNameSecurity(security =>
        {
            security.Encryption = new SoapEncryptionDto { EncryptBody = true, EncryptSignature = true };
            security.ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true };
            configure?.Invoke(security);
        });

    internal static SoapSecurityDto CertificateSecurity(Action<SoapSecurityDto> configure = null)
        => SymmetricTestSupport.CertificateSecurity(security =>
        {
            security.Encryption = new SoapEncryptionDto { EncryptBody = true, EncryptSignature = true };
            security.ResponseSecurity = new SoapResponseSecurityDto { AllowDecryption = true };
            configure?.Invoke(security);
        });

    internal static HttpRequestMessage BuildRequest(Action<SoapSecurityDto> configure = null)
        => SymmetricTestSupport.BuildRequest(Security(configure));

    internal static SymmetricWire Parse(HttpRequestMessage request) => SymmetricTestSupport.Parse(SymmetricTestSupport.Wire(request));

    internal static XmlElement Body(XmlDocument document)
        => document.DocumentElement.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Single(element => element.LocalName == "Body");

    internal static XmlElement BodyEncryptedData(XmlDocument document)
        => SymmetricTestSupport.ChildOf(Body(document), "EncryptedData", SymmetricTestSupport.XencNamespace);

    internal static byte[] CipherValueOf(XmlElement encryptedData)
        => Convert.FromBase64String(SymmetricTestSupport.ChildText(
            SymmetricTestSupport.ChildOf(encryptedData, "CipherData", SymmetricTestSupport.XencNamespace), "CipherValue", SymmetricTestSupport.XencNamespace));

    internal static string CipherValuePrefix(XmlElement encryptedData)
        => Convert.ToBase64String(CipherValueOf(encryptedData))[..40];

    internal static string DecryptBodyContent(SymmetricWire wire)
        => Encoding.UTF8.GetString(SymmetricTestSupport.DecryptCipherValue(CipherValueOf(BodyEncryptedData(wire.Document)), wire.EncryptionKey));

    internal static XmlDocument FullyDecryptedClone(SymmetricWire wire)
    {
        var clone = SymmetricTestSupport.DecryptedClone(wire);
        var body = Body(clone);
        var encryptedData = SymmetricTestSupport.ChildOf(body, "EncryptedData", SymmetricTestSupport.XencNamespace);

        if (encryptedData is null)
            return clone;

        var content = Encoding.UTF8.GetString(SymmetricTestSupport.DecryptCipherValue(CipherValueOf(encryptedData), wire.EncryptionKey));

        body.RemoveChild(encryptedData);
        body.InnerXml = content;

        return clone;
    }

    internal static string RequestSignatureValue(SymmetricWire wire)
    {
        var clone = SymmetricTestSupport.DecryptedClone(wire);
        var security = WsSecurityFoundationTestSupport.SecurityHeader(clone);
        var signature = SymmetricTestSupport.ChildrenOf(security, "Signature", WsSecurityTestSupport.DsNamespace)
            .Single(candidate => SymmetricWire.SignatureMethod(candidate).Contains("hmac"));

        return SymmetricTestSupport.ChildText(signature, "SignatureValue", WsSecurityTestSupport.DsNamespace);
    }

    internal static bool PrimarySignatureVerifiesOverThePlaintextBody(SymmetricWire wire)
    {
        var clone = FullyDecryptedClone(wire);
        var security = WsSecurityFoundationTestSupport.SecurityHeader(clone);
        var signature = SymmetricTestSupport.ChildrenOf(security, "Signature", WsSecurityTestSupport.DsNamespace)
            .Single(candidate => SymmetricWire.SignatureMethod(candidate).Contains("hmac"));

        return SymmetricTestSupport.SignedXmlVerifies(clone, signature, wire.SignatureKey, WsSecurityTestSupport.HmacSha256Signature);
    }

    internal static TierBResponseBuilder Response(SymmetricWire wire)
        => new(wire, new SymmetricResponseBuilder(wire.Secret, wire.EncryptedKeySha1).RelatesTo(wire.MessageId).Confirm(RequestSignatureValue(wire)).WithBody(Canary));

    internal static ForbiddenSecret[] ForbiddenValues(SymmetricWire wire, TierBResponseBuilder response = null)
    {
        var forbidden = new List<ForbiddenSecret>(SymmetricTestSupport.ForbiddenValues(wire, WsSecurityFoundationTestSupport.Password))
        {
            ForbiddenSecret.OfText("the plaintext canary", Canary)
        };

        if (response is not null)
        {
            forbidden.Add(ForbiddenSecret.OfBytes("the response encryption key", response.EncryptionKey));
            forbidden.Add(ForbiddenSecret.OfBytes("the response signature key", response.SignatureKey));

            if (response.BodyCipherValuePrefix is not null)
                forbidden.Add(ForbiddenSecret.OfText("the response cipher value", response.BodyCipherValuePrefix));
        }

        return forbidden.ToArray();
    }

    internal static void Refused(IResult<string> result, string expectedCode, string because, SymmetricWire wire, TierBResponseBuilder response = null)
    {
        Assert.IsFalse(result.IsSuccess, because);
        Assert.IsNull(result.Response, because);

        var codes = WsSecurityAssert.Codes(result);

        Assert.IsTrue(codes.Contains(expectedCode), $"{because} | {expectedCode} | {NegativeTestSupport.Describe(result)}");

        SecretLeakAssert.CarriesNoSecret(result, expectedCode, ForbiddenValues(wire, response));
    }

    internal static string Accepted(IResult<string> result, string because)
    {
        Assert.IsTrue(result.IsSuccess, $"{because} | {NegativeTestSupport.Describe(result)}");
        Assert.IsFalse(string.IsNullOrEmpty(result.Response), because);

        return result.Response;
    }

    internal static X509Certificate2 CreateServiceCertificate(int keySize, DateTimeOffset notBefore, DateTimeOffset notAfter, X509KeyUsageFlags? keyUsage)
        => TestCertificates.SelfSignedRsa("CN=SoapClientCallAssist.Tests.TierB.Service", keySize, notBefore, notAfter, keyUsage);

    internal static byte[] EncryptWithIv(byte[] plaintext, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.ISO10126;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();

        return aes.IV.Concat(encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length)).ToArray();
    }

    internal static byte[] EncryptContent(XmlDocument document, XmlElement element, byte[] key)
        => EncryptSerialised(new EncryptedXml(document), element, key, true);

    internal static byte[] EncryptElement(XmlDocument document, XmlElement element, byte[] key)
        => EncryptSerialised(new EncryptedXml(document), element, key, false);

    private static byte[] EncryptSerialised(EncryptedXml encryptedXml, XmlElement element, byte[] key, bool content)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.ISO10126;
        aes.GenerateIV();

        return encryptedXml.EncryptData(element, aes, content);
    }
}
