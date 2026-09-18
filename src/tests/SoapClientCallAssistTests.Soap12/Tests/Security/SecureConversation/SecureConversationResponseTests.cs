#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Net.Http;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.SecureConversation;

[TestClass]
public sealed class SecureConversationResponseTests
{

    [TestMethod]
    public void Verify_AResponseKeyedByThisSessionsContext_Accepts_Test()
    {
        var secret = SecureConversationTestSupport.RandomBytes(32);
        using var session = SecureConversationTestSupport.Session(secret, TimeSpan.FromMinutes(10));

        var request = SecureConversationTestSupport.BuildRequest(session);
        var response = Response(secret, session.ContextIdentifier, request).Build();

        var verified = new WsSecurityResponseSecurity().Verify(request, response);

        Assert.IsTrue(verified.IsSuccess, NegativeTestSupport.Describe(verified));
    }

    [TestMethod]
    public void Verify_AResponseReferencingAnotherContext_IsRefusedByName_Test()
    {
        var secret = SecureConversationTestSupport.RandomBytes(32);
        using var session = SecureConversationTestSupport.Session(secret, TimeSpan.FromMinutes(10));

        var request = SecureConversationTestSupport.BuildRequest(session);
        var response = Response(secret, session.ContextIdentifier, request).WithForeignContextReference("urn:uuid:" + Guid.NewGuid().ToString("D")).Build();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), SecureConversationTestSupport.ForeignContextCode, "");
    }

    [TestMethod]
    public void Verify_AResponseKeyedByAForeignSecret_FailsToVerify_Test()
    {
        var secret = SecureConversationTestSupport.RandomBytes(32);
        using var session = SecureConversationTestSupport.Session(secret, TimeSpan.FromMinutes(10));

        var request = SecureConversationTestSupport.BuildRequest(session);
        var response = Response(secret, session.ContextIdentifier, request).WithSigningKey(SecureConversationTestSupport.RandomBytes(24)).Build();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), "ER-SEC-VER", "");
    }

    [TestMethod]
    public void Verify_AResponseCarryingAnEncryptedKey_IsRefusedAsUndecryptable_Test()
    {
        var secret = SecureConversationTestSupport.RandomBytes(32);
        using var session = SecureConversationTestSupport.Session(secret, TimeSpan.FromMinutes(10));

        var request = SecureConversationTestSupport.BuildRequest(session);
        var response = Response(secret, session.ContextIdentifier, request).WithEncryptedKey().Build();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), SecureConversationTestSupport.DecryptionCode, "");
    }

    [TestMethod]
    public void Verify_AResponseWhoseDerivedKeyTokenCarriesProperties_IsRefusedByName_Test()
    {
        var secret = SecureConversationTestSupport.RandomBytes(32);
        using var session = SecureConversationTestSupport.Session(secret, TimeSpan.FromMinutes(10));

        var request = SecureConversationTestSupport.BuildRequest(session);
        var response = Response(secret, session.ContextIdentifier, request).WithProperties().Build();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), "V-SEC-075", "");
    }

    [TestMethod]
    public void Verify_AfterTheSessionIsDisposedBeforeTheCheck_IsRefusedByName_Test()
    {
        var secret = SecureConversationTestSupport.RandomBytes(32);
        var session = SecureConversationTestSupport.Session(secret, TimeSpan.FromMinutes(10));

        var request = SecureConversationTestSupport.BuildRequest(session);
        var response = Response(secret, session.ContextIdentifier, request).Build();

        session.Dispose();

        Rejected(new WsSecurityResponseSecurity().Verify(request, response), SecureConversationTestSupport.DisposedBeforeVerifyCode, "");
    }

    private static SecureConversationResponseBuilder Response(byte[] secret, string contextIdentifier, HttpRequestMessage request)
        => new(secret, contextIdentifier, SecureConversationTestSupport.MessageIdOf(SecureConversationTestSupport.Parse(SecureConversationTestSupport.Wire(request))));

    private static void Rejected(IResult result, string expectedCode, string because)
    {
        Assert.IsFalse(result.IsSuccess, because);
        Assert.AreEqual(expectedCode, WsSecurityFoundationTestSupport.FirstCode(result), $"{because} | {NegativeTestSupport.Describe(result)}");
    }
}
