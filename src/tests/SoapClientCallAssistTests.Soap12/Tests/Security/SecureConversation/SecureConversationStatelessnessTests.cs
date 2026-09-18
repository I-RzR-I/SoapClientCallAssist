#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.SecureConversation;

[TestClass]
public sealed class SecureConversationStatelessnessTests
{

    private const int InterleavedBuilds = 60;

    [TestMethod]
    public async Task BuildRequest_InterleavedBuildsForTwoSessionsOnOneSingletonClient_KeepEveryWireBoundToItsOwnSession_Test()
    {
        var secretA = SecureConversationTestSupport.RandomBytes(32);
        var secretB = SecureConversationTestSupport.RandomBytes(32);

        using var sessionA = SecureConversationTestSupport.Session(secretA, TimeSpan.FromMinutes(10));
        using var sessionB = SecureConversationTestSupport.Session(secretB, TimeSpan.FromMinutes(10));

        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var outcomes = await Task.WhenAll(Enumerable.Range(0, InterleavedBuilds).Select(index => Task.Run(() =>
        {
            var useA = index % 2 == 0;
            var session = useA ? sessionA : sessionB;
            var wire = Build(client, session, "p" + index);

            return (Index: index, UseA: useA, Wire: wire);
        })));

        foreach (var outcome in outcomes)
        {
            var document = SecureConversationTestSupport.Parse(outcome.Wire);
            var own = outcome.UseA ? sessionA : sessionB;
            var ownSecret = outcome.UseA ? secretA : secretB;
            var otherSecret = outcome.UseA ? secretB : secretA;

            Assert.AreEqual(own.ContextIdentifier, SecureConversationTestSupport.SessionContextIdentifierOf(document), $"{outcome.Index}");

            Assert.IsTrue(VerifiesUnder(document, ownSecret), $"{outcome.Index}");
            Assert.IsFalse(VerifiesUnder(document, otherSecret), $"{outcome.Index}");
        }

        var identifiers = outcomes.Select(outcome => SecureConversationTestSupport.SessionContextIdentifierOf(SecureConversationTestSupport.Parse(outcome.Wire))).Distinct().ToList();

        CollectionAssert.AreEquivalent(new[] { sessionA.ContextIdentifier, sessionB.ContextIdentifier }, identifiers);
    }

    [TestMethod]
    public async Task BuildRequest_AfterOneSessionIsCancelled_ThatSessionsBuildsFailWhileTheOtherKeepsSucceeding_Test()
    {
        var secretA = SecureConversationTestSupport.RandomBytes(32);
        var secretB = SecureConversationTestSupport.RandomBytes(32);

        var sessionA = SecureConversationTestSupport.Session(secretA, TimeSpan.FromMinutes(10));
        using var sessionB = SecureConversationTestSupport.Session(secretB, TimeSpan.FromMinutes(10));

        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.IsNotNull(Build(client, sessionA, "before"));

        sessionA.Dispose();

        var outcomes = await Task.WhenAll(Enumerable.Range(0, InterleavedBuilds).Select(index => Task.Run(() =>
        {
            var useA = index % 2 == 0;
            var built = client.BuildRequest(
                HttpMethod.Post,
                new BuildSoapRequestDto
                {
                    Client = new HttpClientDto(WsSecurityTestSupport.Endpoint),
                    Envelope = new SoapEnvelopeDto(new[] { WsSecurityTestSupport.Body("p" + index) }, null, WsSecurityTestSupport.Action),
                    Security = SecureConversationTestSupport.Security(useA ? sessionA : sessionB)
                });

            using (built.Response) { }

            return (UseA: useA, built.IsSuccess, Code: built.IsSuccess ? null : WsSecurityFoundationTestSupport.FirstCode(built));
        })));

        Assert.IsTrue(outcomes.Where(outcome => outcome.UseA).All(outcome => !outcome.IsSuccess && outcome.Code == "V-SEC-063"));
        Assert.IsTrue(outcomes.Where(outcome => !outcome.UseA).All(outcome => outcome.IsSuccess));
    }

    private static string Build(ISoapClientEndpoint client, SoapSecureConversationSession session, string payload)
    {
        var built = client.BuildRequest(
            HttpMethod.Post,
            new BuildSoapRequestDto
            {
                Client = new HttpClientDto(WsSecurityTestSupport.Endpoint),
                Envelope = new SoapEnvelopeDto(new[] { WsSecurityTestSupport.Body(payload) }, null, WsSecurityTestSupport.Action),
                Security = SecureConversationTestSupport.Security(session)
            });

        return WsSecurityTestSupport.Wire(built, "Build");
    }

    private static bool VerifiesUnder(XmlDocument document, byte[] secret)
    {
        var security = WsSecurityFoundationTestSupport.SecurityHeader(document);
        var nonce = Convert.FromBase64String(document.GetElementsByTagName("Nonce", SecureConversationTestSupport.ScFebruary2005Namespace).Cast<XmlElement>().First().InnerText);
        var key = SecureConversationTestSupport.DeriveKey(secret, nonce, SecureConversationTestSupport.SignatureKeyLength);

        var signature = security.ChildNodes.Cast<XmlNode>().OfType<XmlElement>()
            .Single(element => element.LocalName == "Signature" && element.NamespaceURI == WsSecurityTestSupport.DsNamespace);

        return SymmetricTestSupport.SignedXmlVerifies(document, signature, key, WsSecurityTestSupport.HmacSha256Signature);
    }
}
