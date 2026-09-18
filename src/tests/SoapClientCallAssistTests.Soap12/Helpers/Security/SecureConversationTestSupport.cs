#nullable disable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssistTests.Common;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal static class SecureConversationTestSupport
{

    internal const string ScFebruary2005Namespace = "http://schemas.xmlsoap.org/ws/2005/02/sc";

    internal const string TrustFebruary2005Namespace = "http://schemas.xmlsoap.org/ws/2005/02/trust";

    internal const string DefaultLabel = "WS-SecureConversationWS-SecureConversation";

    internal const int SignatureKeyLength = 24;

    internal const string DisposedBeforeVerifyCode = "V-SEC-073";

    internal const string ForeignContextCode = "V-SEC-074";

    internal const string DecryptionCode = "ER-SEC-DEC";

    private static readonly Type ReaderType = WsSecurityFoundationTestSupport.LibraryType("SoapClientCallAssist.Security.SecureConversationIssueReader");

    internal static SoapSecureConversationSession Session(byte[] secret, TimeSpan lifetime)
        => new("urn:uuid:" + Guid.NewGuid().ToString("D"), secret, DateTimeOffset.UtcNow.Add(lifetime), SoapSecureConversationVersionType.February2005);

    internal static SoapSecureConversationSession RandomSession(TimeSpan lifetime) => Session(RandomBytes(32), lifetime);

    internal static byte[] RandomBytes(int length) => RandomData.Bytes(length);

    internal static SoapSecurityDto Security(SoapSecureConversationSession session, Action<SoapSecurityDto> configure = null)
        => WsSecurityFoundationTestSupport.SecureConversationSecurity(session, security =>
        {
            security.ResponseSecurity = new SoapResponseSecurityDto { RequireSignatureConfirmation = false };
            configure?.Invoke(security);
        });

    internal static HttpRequestMessage BuildRequest(SoapSecureConversationSession session)
    {
        var built = WsSecurityTestSupport.BuildPost(Security(session), WsSecurityTestSupport.Body("sc"));
        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));

        return built.Response;
    }

    internal static string Wire(HttpRequestMessage request) => request.Content.ReadAsStringAsync().GetAwaiter().GetResult();

    internal static XmlDocument Parse(string wire)
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.LoadXml(wire);

        return document;
    }

    internal static string MessageIdOf(XmlDocument document)
        => document.GetElementsByTagName("MessageID", WsSecurityFoundationTestSupport.WsAddressing10Namespace).Cast<XmlElement>().Single().InnerText.Trim();

    internal static IResult<SoapSecureConversationSession> ReaderRead(string responseEnvelope, byte[] clientEntropy)
        => (IResult<SoapSecureConversationSession>)ReaderType
            .GetMethod("Read", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { responseEnvelope, SoapSecureConversationVersionType.February2005, clientEntropy, 32, 256 });

    internal static byte[] ComputeKey(byte[] clientEntropy, byte[] serverEntropy, int length)
        => WsTrustKeyDerivation.PSha1(clientEntropy, serverEntropy, length);

    internal static byte[] DeriveKey(byte[] secret, byte[] nonce, int length)
        => WsTrustKeyDerivation.DeriveKey(secret, DefaultLabel, nonce, 0, length);

    internal static string SessionContextIdentifierOf(XmlDocument requestDocument)
        => requestDocument.GetElementsByTagName("Identifier", ScFebruary2005Namespace).Cast<XmlElement>().Single().InnerText.Trim();

    internal static SoapSecurityDto Bootstrap()
        => SymmetricTestSupport.UserNameSecurity();

    internal static SoapSecureConversationClient IssueClientThrough(DelegatingHandler handler)
    {
        var services = new ServiceCollection();
        services.RegisterSoapClientsEndpoint();
        services.AddHttpClient(SoapClientEndpointExtensions.SoapHttpClientName).AddHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();

        return new SoapSecureConversationClient(provider.GetRequiredService<IHttpClientFactory>(), null);
    }

    internal static XElement DecryptedRequestSecurityToken(string requestWire)
    {
        var wire = new SymmetricWire(requestWire, WsSecurityFoundationTestSupport.ServiceCertificate);
        var content = TierBTestSupport.DecryptBodyContent(wire);

        return XElement.Parse(content);
    }

    internal static byte[] ClientEntropyOf(string requestWire)
    {
        var rst = DecryptedRequestSecurityToken(requestWire);
        XNamespace trust = TrustFebruary2005Namespace;
        var binarySecret = rst.Element(trust + "Entropy")?.Element(trust + "BinarySecret");
        Assert.IsNotNull(binarySecret);

        return Convert.FromBase64String(binarySecret.Value.Trim());
    }
}
