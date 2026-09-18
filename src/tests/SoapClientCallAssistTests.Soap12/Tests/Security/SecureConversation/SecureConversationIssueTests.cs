#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using SoapClientCallAssistTests.Soap12.Helpers.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.SecureConversation;

[TestClass]
public sealed class SecureConversationIssueTests
{

    private static readonly XNamespace Trust = SecureConversationTestSupport.TrustFebruary2005Namespace;

    private static readonly XNamespace Sc = SecureConversationTestSupport.ScFebruary2005Namespace;

    [TestMethod]
    public void ReadIssuedSession_FromAValidResponse_ComputesTheSessionKeyAsPsha1OverBothEntropies_Test()
    {
        var clientEntropy = SecureConversationTestSupport.RandomBytes(32);
        var serverEntropy = SecureConversationTestSupport.RandomBytes(32);

        var read = SecureConversationTestSupport.ReaderRead(Rstr().ServerEntropy(serverEntropy).Build(), clientEntropy);
        Assert.IsTrue(read.IsSuccess, NegativeTestSupport.Describe(read));

        using var session = read.Response;
        var request = SecureConversationTestSupport.BuildRequest(session);
        var document = SecureConversationTestSupport.Parse(SecureConversationTestSupport.Wire(request));
        var expectedSecret = SecureConversationTestSupport.ComputeKey(clientEntropy, serverEntropy, 32);

        var response = new SecureConversationResponseBuilder(expectedSecret, session.ContextIdentifier, SecureConversationTestSupport.MessageIdOf(document)).Build();

        Assert.IsTrue(new WsSecurityResponseSecurity().Verify(request, response).IsSuccess);
    }

    [TestMethod]
    public void ReadIssuedSession_CarriesTheContextIdentifierAndExpiryFromTheResponse_Test()
    {
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        var read = SecureConversationTestSupport.ReaderRead(Rstr().Context("urn:uuid:the-context").Expires(expiry).Build(), SecureConversationTestSupport.RandomBytes(32));

        Assert.IsTrue(read.IsSuccess, NegativeTestSupport.Describe(read));
        using var session = read.Response;

        Assert.AreEqual("urn:uuid:the-context", session.ContextIdentifier);
        Assert.AreEqual(expiry.ToUnixTimeSeconds(), session.ExpiresUtc.ToUnixTimeSeconds());
    }

    [TestMethod]
    public void ReadIssuedSession_RefusesEachMalformedResponseByName_Test()
    {
        var cases = new (string Name, string Code, Func<RstrBuilder, RstrBuilder> Shape)[]
        {
            ("a proof token carrying a BinarySecret the service chose alone", "V-SEC-066", builder => builder.ProofBinarySecret()),
            ("a proof token naming another computed key algorithm", "V-SEC-066", builder => builder.ComputedKeyAlgorithm("urn:not-psha1")),
            ("no server entropy", "V-SEC-067", builder => builder.WithoutEntropy()),
            ("a server entropy shorter than 32 bytes", "V-SEC-067", builder => builder.ServerEntropy(new byte[16])),
            ("an all-zero server entropy", "V-SEC-067", builder => builder.ServerEntropy(new byte[32])),
            ("a key size other than 256", "V-SEC-068", builder => builder.KeySize(128)),
            ("no security context token identifier", "V-SEC-069", builder => builder.WithoutContext()),
            ("no lifetime", "V-SEC-070", builder => builder.WithoutLifetime()),
            ("an already expired lifetime", "V-SEC-070", builder => builder.Expires(DateTimeOffset.UtcNow.AddMinutes(-1)))
        };

        foreach (var (name, code, shape) in cases)
        {
            var read = SecureConversationTestSupport.ReaderRead(shape(Rstr()).Build(), SecureConversationTestSupport.RandomBytes(32));

            Assert.IsFalse(read.IsSuccess, $"{name}");
            Assert.IsNull(read.Response, $"{name}");
            Assert.AreEqual(code, WsSecurityFoundationTestSupport.FirstCode(read), $"{name} | {NegativeTestSupport.Describe(read)}");
        }
    }

    [TestMethod]
    public void ReadIssuedSession_RefusesAnEnvelopeThatIsNotASingleRequestSecurityTokenResponse_Test()
    {
        var read = SecureConversationTestSupport.ReaderRead(
            "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body/></s:Envelope>",
            SecureConversationTestSupport.RandomBytes(32));

        Assert.AreEqual("V-SEC-072", WsSecurityFoundationTestSupport.FirstCode(read));
    }

    [TestMethod]
    public async System.Threading.Tasks.Task IssueAsync_SendsAWellFormedRequestSecurityToken_Test()
    {
        var captured = new List<string>();
        var client = SecureConversationTestSupport.IssueClientThrough(new CannedResponseHttpMessageHandler(request =>
        {
            captured.Add(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            return Ok();
        }));

        await client.IssueAsync(WsSecurityTestSupport.Endpoint, SecureConversationTestSupport.Bootstrap());

        Assert.AreEqual(1, captured.Count);
        var rst = SecureConversationTestSupport.DecryptedRequestSecurityToken(captured[0]);

        Assert.AreEqual("RequestSecurityToken", rst.Name.LocalName);
        Assert.IsTrue(rst.Element(Trust + "RequestType")!.Value.EndsWith("/Issue", StringComparison.Ordinal));
        Assert.IsTrue(rst.Element(Trust + "TokenType")!.Value.EndsWith("/sct", StringComparison.Ordinal));
        Assert.IsTrue(rst.Element(Trust + "KeyType")!.Value.EndsWith("/SymmetricKey", StringComparison.Ordinal));
        Assert.AreEqual("256", rst.Element(Trust + "KeySize")!.Value);
        Assert.AreEqual(32, Convert.FromBase64String(rst.Element(Trust + "Entropy")!.Element(Trust + "BinarySecret")!.Value).Length);
        Assert.IsTrue(rst.Element(Trust + "ComputedKey")!.Value.EndsWith("/CK/PSHA1", StringComparison.Ordinal));
    }

    [TestMethod]
    public async System.Threading.Tasks.Task IssueAsync_TwoIssues_NeverReuseTheClientEntropy_Test()
    {
        var captured = new List<string>();
        var client = SecureConversationTestSupport.IssueClientThrough(new CannedResponseHttpMessageHandler(request =>
        {
            captured.Add(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            return Ok();
        }));

        await client.IssueAsync(WsSecurityTestSupport.Endpoint, SecureConversationTestSupport.Bootstrap());
        await client.IssueAsync(WsSecurityTestSupport.Endpoint, SecureConversationTestSupport.Bootstrap());

        Assert.AreEqual(2, captured.Count);

        var first = SecureConversationTestSupport.ClientEntropyOf(captured[0]);
        var second = SecureConversationTestSupport.ClientEntropyOf(captured[1]);

        Assert.IsFalse(first.SequenceEqual(second));
    }

    private static RstrBuilder Rstr() => new();

    private static HttpResponseMessage Ok()
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body/></s:Envelope>", Encoding.UTF8, "application/soap+xml")
        };

    private sealed class RstrBuilder
    {

        private string _context = "urn:uuid:" + Guid.NewGuid().ToString("D");

        private byte[] _entropy = SecureConversationTestSupport.RandomBytes(32);

        private bool _entropyPresent = true;

        private bool _contextPresent = true;

        private bool _lifetimePresent = true;

        private bool _proofBinarySecret;

        private string _computedKeyAlgorithm = SecureConversationTestSupport.TrustFebruary2005Namespace + "/CK/PSHA1";

        private int _keySize = 256;

        private DateTimeOffset _expires = DateTimeOffset.UtcNow.AddHours(1);

        internal RstrBuilder Context(string context) { _context = context; return this; }

        internal RstrBuilder WithoutContext() { _contextPresent = false; return this; }

        internal RstrBuilder ServerEntropy(byte[] entropy) { _entropy = entropy; return this; }

        internal RstrBuilder WithoutEntropy() { _entropyPresent = false; return this; }

        internal RstrBuilder ProofBinarySecret() { _proofBinarySecret = true; return this; }

        internal RstrBuilder ComputedKeyAlgorithm(string algorithm) { _computedKeyAlgorithm = algorithm; return this; }

        internal RstrBuilder KeySize(int keySize) { _keySize = keySize; return this; }

        internal RstrBuilder WithoutLifetime() { _lifetimePresent = false; return this; }

        internal RstrBuilder Expires(DateTimeOffset expires) { _expires = expires; return this; }

        internal string Build()
        {
            XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";
            XNamespace wsu = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

            var proof = _proofBinarySecret
                ? new XElement(Trust + "RequestedProofToken", new XElement(Trust + "BinarySecret", Convert.ToBase64String(SecureConversationTestSupport.RandomBytes(32))))
                : new XElement(Trust + "RequestedProofToken", new XElement(Trust + "ComputedKey", _computedKeyAlgorithm));

            var response = new XElement(
                Trust + "RequestSecurityTokenResponse",
                new XElement(Trust + "TokenType", Sc.NamespaceName + "/sct"),
                _contextPresent
                    ? new XElement(Trust + "RequestedSecurityToken", new XElement(Sc + "SecurityContextToken", new XElement(Sc + "Identifier", _context)))
                    : new XElement(Trust + "RequestedSecurityToken", new XElement(Sc + "SecurityContextToken")),
                proof,
                _entropyPresent ? new XElement(Trust + "Entropy", new XElement(Trust + "BinarySecret", Convert.ToBase64String(_entropy))) : null,
                _lifetimePresent ? new XElement(Trust + "Lifetime", new XElement(wsu + "Expires", _expires.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))) : null,
                new XElement(Trust + "KeySize", _keySize));

            var envelope = new XElement(soap + "Envelope", new XElement(soap + "Body", response));

            return envelope.ToString(SaveOptions.DisableFormatting);
        }
    }
}
