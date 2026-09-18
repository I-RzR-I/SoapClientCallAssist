#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Linq;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.SecureConversation;

[TestClass]
public sealed class SecureConversationRequestTests
{

    [TestMethod]
    public void BuildRequest_KeyedByASession_EmitsASecurityContextTokenTheDerivedKeyTokensReferenceByItsLocalId_Test()
    {
        using var session = SecureConversationTestSupport.RandomSession(TimeSpan.FromMinutes(10));

        var document = SecureConversationTestSupport.Parse(SecureConversationTestSupport.Wire(SecureConversationTestSupport.BuildRequest(session)));
        var security = WsSecurityFoundationTestSupport.SecurityHeader(document);

        var sct = security.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Single(element => element.LocalName == "SecurityContextToken");
        var sctId = sct.GetAttribute("Id", WsSecurityTestSupport.WsuNamespace);

        Assert.AreEqual(session.ContextIdentifier, SecureConversationTestSupport.SessionContextIdentifierOf(document));
        Assert.IsFalse(string.IsNullOrEmpty(sctId));

        var derivedKeyToken = security.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().First(element => element.LocalName == "DerivedKeyToken");
        var reference = derivedKeyToken.GetElementsByTagName("Reference", WsSecurityTestSupport.WsseNamespace).Cast<XmlElement>().Single();

        Assert.AreEqual("#" + sctId, reference.GetAttribute("URI"));
        Assert.AreEqual(SecureConversationTestSupport.ScFebruary2005Namespace + "/sct", reference.GetAttribute("ValueType"));
    }

    [TestMethod]
    public void BuildRequest_KeyedByASession_SignsUnderAKeyDerivedFromTheSessionSecret_Test()
    {
        var secret = SecureConversationTestSupport.RandomBytes(32);
        using var session = SecureConversationTestSupport.Session(secret, TimeSpan.FromMinutes(10));

        var request = SecureConversationTestSupport.BuildRequest(session);
        var verified = new WsSecurityResponseSecurity().Verify(
            request,
            new SecureConversationResponseBuilder(secret, session.ContextIdentifier, SecureConversationTestSupport.MessageIdOf(SecureConversationTestSupport.Parse(SecureConversationTestSupport.Wire(request)))).Build());

        Assert.IsTrue(verified.IsSuccess, NegativeTestSupport.Describe(verified));
    }

    [TestMethod]
    public void BuildRequest_WithAnExpiredSession_IsRefusedAtSignTimeByName_Test()
    {
        using var session = SecureConversationTestSupport.Session(SecureConversationTestSupport.RandomBytes(32), TimeSpan.FromMinutes(-1));

        WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(SecureConversationTestSupport.Security(session), WsSecurityTestSupport.Body("sc")),
            "V-SEC-062",
            "");
    }

    [TestMethod]
    public void BuildRequest_WithADisposedSession_IsRefusedByName_Test()
    {
        var session = SecureConversationTestSupport.Session(SecureConversationTestSupport.RandomBytes(32), TimeSpan.FromMinutes(10));
        session.Dispose();

        WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(SecureConversationTestSupport.Security(session), WsSecurityTestSupport.Body("sc")),
            "V-SEC-063",
            "");
    }

    [TestMethod]
    public void BuildRequest_AfterASessionIsDisposed_RefusesEveryFurtherBuild_Test()
    {
        var secret = SecureConversationTestSupport.RandomBytes(32);
        var session = SecureConversationTestSupport.Session(secret, TimeSpan.FromMinutes(10));

        Assert.IsTrue(WsSecurityTestSupport.BuildPost(SecureConversationTestSupport.Security(session), WsSecurityTestSupport.Body("a")).IsSuccess);

        session.Dispose();

        WsSecurityFoundationTestSupport.RefusedWith(
            WsSecurityTestSupport.BuildPost(SecureConversationTestSupport.Security(session), WsSecurityTestSupport.Body("b")),
            "V-SEC-063",
            "");
    }

    [TestMethod]
    public void BuildRequest_ManyBuildsOnOneImmutableSession_EachMintsItsOwnNonce_Test()
    {
        var secret = SecureConversationTestSupport.RandomBytes(32);
        using var session = SecureConversationTestSupport.Session(secret, TimeSpan.FromMinutes(10));

        var nonces = Enumerable.Range(0, 20)
            .AsParallel()
            .Select(index =>
            {
                var document = SecureConversationTestSupport.Parse(SecureConversationTestSupport.Wire(SecureConversationTestSupport.BuildRequest(session)));

                return document.GetElementsByTagName("Nonce", SecureConversationTestSupport.ScFebruary2005Namespace).Cast<XmlElement>().First().InnerText;
            })
            .ToList();

        Assert.AreEqual(nonces.Count, nonces.Distinct(StringComparer.Ordinal).Count());
    }
}
