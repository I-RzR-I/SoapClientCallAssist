using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Wcf.Helpers.Symmetric;
using SoapClientCallAssistTests.Wcf.Service.Symmetric;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Text;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Tests.Symmetric;

[TestClass]
public sealed class SymmetricReferenceWireTests
{

    private const string DefaultLabel = "WS-SecureConversationWS-SecureConversation";

    private static readonly XNamespace Xenc = "http://www.w3.org/2001/04/xmlenc#";

    private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";

    private static readonly XNamespace Wsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

    private static readonly XNamespace Wsu = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    private static SymmetricProbeHost? _host;

    public TestContext TestContext { get; set; } = null!;

    private static SymmetricProbeHost Host => _host ?? throw new InvalidOperationException("The symmetric host did not start.");

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _host = SymmetricProbeHost.Open();

        foreach (var endpoint in _host.Endpoints())
            context.WriteLine(endpoint);
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _host?.Dispose();
        _host = null;
    }

    [DataTestMethod]
    [DataRow(SymmetricProbeHost.CertificateCredential, SoapSecureConversationVersionType.February2005, SoapProtocolType.SOAP_1_1, 2, DisplayName = "certificate, SC Feb 2005, SOAP 1.1")]
    [DataRow(SymmetricProbeHost.CertificateCredential, SoapSecureConversationVersionType.December2005, SoapProtocolType.SOAP_1_2, 2, DisplayName = "certificate, SC Dec 2005, SOAP 1.2")]
    [DataRow(SymmetricProbeHost.UserNameCredential, SoapSecureConversationVersionType.February2005, SoapProtocolType.SOAP_1_1, 0, DisplayName = "username, SC Feb 2005, SOAP 1.1")]
    [DataRow(SymmetricProbeHost.UserNameCredential, SoapSecureConversationVersionType.December2005, SoapProtocolType.SOAP_1_2, 0, DisplayName = "username, SC Dec 2005, SOAP 1.2")]
    public void WcfClient_WhoAmI_ProducesTheWireShapeTheLibraryReproducesAndItsHmacDerivesUnderPSha1_Test(
        string credential, SoapSecureConversationVersionType secureConversation, SoapProtocolType protocol, int expectedConfirmations)
    {
        var capture = Capture(credential, secureConversation, protocol);

        var request = XDocument.Parse(capture.Requests.Single());
        var response = XDocument.Parse(capture.Responses.Single());

        var encryptedKey = request.Descendants(Xenc + "EncryptedKey").Single();
        var wrapped = Convert.FromBase64String(encryptedKey.Element(Xenc + "CipherData")!.Element(Xenc + "CipherValue")!.Value);

        Assert.AreEqual("http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p", encryptedKey.Element(Xenc + "EncryptionMethod")!.Attribute("Algorithm")!.Value);
        Assert.AreEqual(
            Convert.ToBase64String(SHA1.Create().ComputeHash(Host.ServiceCertificate.Certificate.RawData)),
            encryptedKey.Descendants(Wsse + "KeyIdentifier").Single().Value);

        using var privateKey = Host.ServiceCertificate.Certificate.GetRSAPrivateKey();
        var secret = privateKey.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA1);

        Assert.AreEqual(32, secret.Length);

        var scNamespace = XNamespace.Get(SymmetricCallSupport.SecureConversationNamespace(secureConversation));
        var requestSignature = request.Descendants(Ds + "Signature").First(signature => SignatureMethod(signature).Contains("hmac"));
        var requestToken = DerivedKeyTokenOf(request, requestSignature, scNamespace);

        Assert.AreEqual("24", requestToken.Element(scNamespace + "Length")!.Value);
        Assert.AreEqual("0", requestToken.Element(scNamespace + "Offset")!.Value);
        Assert.AreEqual(
            Convert.ToBase64String(HmacOverSignedInfo(requestSignature, DerivedKey(secret, requestToken, scNamespace))),
            requestSignature.Element(Ds + "SignatureValue")!.Value);

        var responseSignature = response.Descendants(Ds + "Signature").Single();
        var responseToken = DerivedKeyTokenOf(response, responseSignature, scNamespace);
        var keyIdentifier = responseToken.Descendants(Wsse + "KeyIdentifier").Single();

        Assert.AreEqual("http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKeySHA1", keyIdentifier.Attribute("ValueType")!.Value);
        Assert.AreEqual(Convert.ToBase64String(SHA1.Create().ComputeHash(wrapped)), keyIdentifier.Value);
        Assert.AreEqual(
            Convert.ToBase64String(HmacOverSignedInfo(responseSignature, DerivedKey(secret, responseToken, scNamespace))),
            responseSignature.Element(Ds + "SignatureValue")!.Value);

        Assert.AreEqual(expectedConfirmations, response.Descendants().Count(element => element.Name.LocalName == "SignatureConfirmation"));

        if (credential == SymmetricProbeHost.UserNameCredential)
        {
            Assert.IsFalse(capture.Requests.Single().Contains(SymmetricProbeHost.KnownPassword));
            Assert.AreEqual(2, request.Descendants(scNamespace + "DerivedKeyToken").Count());
            Assert.IsTrue(request.Descendants(Xenc + "ReferenceList").Any());
        }

        TestContext.WriteLine($"secret={Convert.ToBase64String(secret)} requestNonce={requestToken.Element(scNamespace + "Nonce")!.Value} responseNonce={responseToken.Element(scNamespace + "Nonce")!.Value}");
    }

    private SymmetricWireCapture Capture(string credential, SoapSecureConversationVersionType secureConversation, SoapProtocolType protocol)
    {
        var capture = new SymmetricWireCapture();
        var binding = SymmetricProbeHost.Binding(credential, secureConversation, MessageProtectionOrder.SignBeforeEncrypt, protocol);

        for (var index = 0; index < binding.Elements.Count; index++)
            if (binding.Elements[index] is MessageEncodingBindingElement encoding)
                binding.Elements[index] = new SymmetricCapturingEncodingBindingElement(encoding, capture);

        var address = new EndpointAddress(
            Host.Address(credential, secureConversation, MessageProtectionOrder.SignBeforeEncrypt, protocol),
            EndpointIdentity.CreateX509CertificateIdentity(Host.ServiceCertificate.Certificate));

        var factory = new ChannelFactory<ISymmetricProbeService>(binding, address);

        factory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
        factory.Credentials.ServiceCertificate.DefaultCertificate = Host.ServiceCertificate.Certificate;

        if (credential == SymmetricProbeHost.CertificateCredential)
            factory.Credentials.ClientCertificate.Certificate = Host.ClientCertificate.Certificate;
        else
        {
            factory.Credentials.UserName.UserName = SymmetricProbeHost.KnownUserName;
            factory.Credentials.UserName.Password = SymmetricProbeHost.KnownPassword;
        }

        var channel = factory.CreateChannel();

        try
        {
            var identity = channel.WhoAmI();
            TestContext.WriteLine($"WhoAmI={identity.AuthenticationType}:{identity.Name}");
            ((IClientChannel)channel).Close();
        }
        catch (Exception)
        {
            ((IClientChannel)channel).Abort();

            throw;
        }
        finally
        {
            factory.Close();
        }

        return capture;
    }

    private static string SignatureMethod(XElement signature)
        => signature.Element(Ds + "SignedInfo")!.Element(Ds + "SignatureMethod")!.Attribute("Algorithm")!.Value;

    private static XElement DerivedKeyTokenOf(XDocument document, XElement signature, XNamespace scNamespace)
    {
        var uri = signature.Element(Ds + "KeyInfo")!.Descendants(Wsse + "Reference").Single().Attribute("URI")!.Value.Substring(1);

        return document.Descendants(scNamespace + "DerivedKeyToken").Single(token => token.Attribute(Wsu + "Id")!.Value == uri);
    }

    private static byte[] DerivedKey(byte[] secret, XElement token, XNamespace scNamespace)
    {
        var nonce = Convert.FromBase64String(token.Element(scNamespace + "Nonce")!.Value);
        var length = int.Parse(token.Element(scNamespace + "Length")?.Value ?? "32");
        var offset = int.Parse(token.Element(scNamespace + "Offset")?.Value ?? "0");
        var seed = Encoding.UTF8.GetBytes(DefaultLabel).Concat(nonce).ToArray();

        return WsTrustKeyDerivation.PSha1(secret, seed, offset + length).Skip(offset).Take(length).ToArray();
    }

    private static byte[] HmacOverSignedInfo(XElement signature, byte[] key)
    {
        var document = new System.Xml.XmlDocument { PreserveWhitespace = true };
        document.LoadXml(signature.Element(Ds + "SignedInfo")!.ToString(SaveOptions.DisableFormatting));

        var transform = new XmlDsigExcC14NTransform();
        transform.LoadInput(document.DocumentElement!.SelectNodes("descendant-or-self::node() | descendant-or-self::*/@*"));

        using var output = (Stream)transform.GetOutput(typeof(Stream));
        using var buffer = new MemoryStream();
        output.CopyTo(buffer);

        using var hmac = new HMACSHA256(key);

        return hmac.ComputeHash(buffer.ToArray());
    }
}
