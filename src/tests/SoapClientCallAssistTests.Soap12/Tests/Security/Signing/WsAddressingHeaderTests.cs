#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Linq;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class WsAddressingHeaderTests
{

    private const string OtherActor = "urn:other-actor";

    [TestMethod]
    public void BuildRequest_WithAddressing_EmitsTheFourHeadersBeforeSecurityAndSignsEveryOne_Test()
    {
        var wire = WsSecurityTestSupport.SignedWire(WsSecurityTestSupport.Security(security =>
            security.Addressing = WsSecurityFoundationTestSupport.Addressing()));

        var document = WsSecurityTestSupport.ParseWire(wire);
        var children = WsSecurityFoundationTestSupport.HeaderChildren(document);

        var names = children.Select(element => element.LocalName).ToArray();

        CollectionAssert.AreEqual(
            new[] { "Action", "Action", "MessageID", "ReplyTo", "To", "Security" },
            names,
            $"{string.Join(", ", names)} | {wire}");

        var addressing = children.Where(element => element.NamespaceURI == WsSecurityFoundationTestSupport.WsAddressing10Namespace).ToList();

        Assert.AreEqual(4, addressing.Count, wire);

        var references = WsSecurityFoundationTestSupport.SignedReferenceUris(document);

        foreach (var header in addressing)
        {
            var id = WsSecurityTestSupport.WsuId(header);

            Assert.IsFalse(string.IsNullOrEmpty(id), $"{header.LocalName} | {wire}");
            CollectionAssert.Contains(references.ToList(), "#" + id, $"{header.LocalName} | {wire}");
        }

        var action = addressing.Single(element => element.LocalName == "Action");
        var to = addressing.Single(element => element.LocalName == "To");
        var messageId = addressing.Single(element => element.LocalName == "MessageID");
        var replyTo = addressing.Single(element => element.LocalName == "ReplyTo");

        Assert.AreEqual(WsSecurityTestSupport.Action, action.InnerText);
        Assert.AreEqual("1", action.GetAttribute("mustUnderstand", document.DocumentElement.NamespaceURI));
        Assert.AreEqual(WsSecurityTestSupport.Endpoint.AbsoluteUri, to.InnerText);
        Assert.AreEqual("1", to.GetAttribute("mustUnderstand", document.DocumentElement.NamespaceURI));
        StringAssert.StartsWith(messageId.InnerText, "urn:uuid:");
        Assert.IsTrue(Guid.TryParse(messageId.InnerText.Substring("urn:uuid:".Length), out _), $"{messageId.InnerText}");
        Assert.AreEqual("http://www.w3.org/2005/08/addressing/anonymous", WsSecurityFoundationTestSupport.ChildText(replyTo, "Address"));

        WsSecurityAssert.Accepted(
            new WsSecurityMessageVerifier().Verify(wire, WsSecurityTestSupport.SigningCertificate),
            "");
    }

    [TestMethod]
    public void BuildRequest_WithAddressingOverrides_HonoursToActionVersionAndTheOptionalHeaders_Test()
    {
        var destination = new Uri("https://override.invalid/Service.svc");

        var wire = WsSecurityTestSupport.SignedWire(WsSecurityTestSupport.Security(security =>
            security.Addressing = WsSecurityFoundationTestSupport.Addressing(addressing =>
            {
                addressing.To = destination;
                addressing.Action = WsSecurityTestSupport.Action;
                addressing.Version = SoapAddressingVersionType.WsAddressingAugust2004;
                addressing.IncludeMessageId = false;
                addressing.IncludeReplyTo = false;
            })));

        var document = WsSecurityTestSupport.ParseWire(wire);
        var addressing = WsSecurityFoundationTestSupport.HeaderChildren(document)
            .Where(element => element.NamespaceURI == WsSecurityFoundationTestSupport.WsAddressingAugust2004Namespace)
            .ToList();

        CollectionAssert.AreEqual(new[] { "Action", "To" }, addressing.Select(element => element.LocalName).ToArray(), wire);

        Assert.AreEqual(destination.AbsoluteUri, addressing.Single(element => element.LocalName == "To").InnerText);
        Assert.IsFalse(wire.Contains(WsSecurityFoundationTestSupport.WsAddressing10Namespace, StringComparison.Ordinal), wire);
    }

    [TestMethod]
    public void BuildRequest_WithAddressingButNoSignature_EmitsUnsignedHeaders_Test()
    {
        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildPost(WsSecurityFoundationTestSupport.TokenOnlySecurity(security =>
                security.Addressing = WsSecurityFoundationTestSupport.Addressing())),
            "Build");

        var document = WsSecurityTestSupport.ParseWire(wire);

        Assert.AreEqual(
            4,
            WsSecurityFoundationTestSupport.HeaderChildren(document).Count(element => element.NamespaceURI == WsSecurityFoundationTestSupport.WsAddressing10Namespace),
            wire);

        Assert.IsFalse(wire.Contains("<Signature", StringComparison.Ordinal), wire);
    }

    [TestMethod]
    public void BuildRequest_WithACallerSuppliedSecurityHeaderForTheSameActor_ReusesItRatherThanAddingASecond_Test()
    {
        var supplied = new XElement(
            XName.Get("Security", WsSecurityTestSupport.WsseNamespace),
            new XAttribute(XNamespace.Xmlns + "wsse", WsSecurityTestSupport.WsseNamespace),
            new XElement(XName.Get("Marker", "urn:caller"), "kept"));

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildWithHeaders(SoapProtocolType.SOAP_1_2, HttpMethod.Post, WsSecurityTestSupport.Security(), new[] { supplied }),
            "Build");

        var document = WsSecurityTestSupport.ParseWire(wire);
        var headers = WsSecurityFoundationTestSupport.HeaderChildren(document).Where(element => element.LocalName == "Security").ToList();

        Assert.AreEqual(1, headers.Count, wire);

        var children = headers[0].ChildNodes.Cast<XmlNode>().Select(node => node.LocalName).ToArray();

        CollectionAssert.AreEqual(
            new[] { "Marker", "BinarySecurityToken", "Timestamp", "Signature" },
            children,
            $"{string.Join(", ", children)} | {wire}");

        WsSecurityAssert.Accepted(
            new WsSecurityMessageVerifier().Verify(wire, WsSecurityTestSupport.SigningCertificate),
            "");
    }

    [TestMethod]
    public void BuildRequest_WithACallerSuppliedSecurityHeaderForAnotherActor_AddsItsOwn_Test()
    {
        XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";

        var supplied = new XElement(
            XName.Get("Security", WsSecurityTestSupport.WsseNamespace),
            new XAttribute(XNamespace.Xmlns + "wsse", WsSecurityTestSupport.WsseNamespace),
            new XAttribute(soap + "role", OtherActor));

        var wire = WsSecurityTestSupport.Wire(
            WsSecurityTestSupport.BuildWithHeaders(SoapProtocolType.SOAP_1_2, HttpMethod.Post, WsSecurityTestSupport.Security(), new[] { supplied }),
            "Build");

        var document = WsSecurityTestSupport.ParseWire(wire);
        var headers = WsSecurityFoundationTestSupport.HeaderChildren(document).Where(element => element.LocalName == "Security").ToList();

        Assert.AreEqual(2, headers.Count, wire);
        Assert.AreEqual(OtherActor, headers[0].GetAttribute("role", soap.NamespaceName));
        Assert.IsFalse(headers[1].HasAttribute("role", soap.NamespaceName));
        Assert.AreEqual(0, headers[0].ChildNodes.Count);
    }
}
