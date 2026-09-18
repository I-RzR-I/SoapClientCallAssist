using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapTestService;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Protocol;

public static class SoapAssert
{

    public const string Soap12Ns = "http://www.w3.org/2003/05/soap-envelope";

    public const string Soap11Ns = "http://schemas.xmlsoap.org/soap/envelope/";

    public const string ServiceNs = "http://SoapClientCallAssist.local/";

    public const string Soap12MediaType = "application/soap+xml";

    public const string Soap11MediaType = "text/xml";

    private const int SnippetLength = 512;

    private static readonly XNamespace Soap12Namespace = Soap12Ns;

    public static XElement AssertIsSoap12Envelope(string xml)
    {
        var root = ParseRoot(xml);

        Assert.AreEqual("Envelope", root.Name.LocalName, Snippet(xml));

        Assert.AreEqual(Soap12Ns, root.Name.NamespaceName, $"{Soap11Ns} | {Snippet(xml)}");

        return root;
    }

    public static void AssertEnvelopeNamespaceIsNot11(string xml)
    {
        var root = ParseRoot(xml);

        Assert.AreNotEqual(Soap11Ns, root.Name.NamespaceName, Snippet(xml));
    }

    public static XElement AssertIsSoap11Envelope(string xml)
    {
        var root = ParseRoot(xml);

        Assert.AreEqual("Envelope", root.Name.LocalName, Snippet(xml));

        Assert.AreEqual(Soap11Ns, root.Name.NamespaceName, $"{Soap12Ns} | {Snippet(xml)}");

        return root;
    }

    public static XElement GetBodyChild(string xml)
    {
        var root = AssertIsSoap12Envelope(xml);

        var body = root.Element(Soap12Namespace + "Body")
                   ?? throw Failed(Snippet(xml));

        return body.Elements().FirstOrDefault()
               ?? throw Failed(Snippet(xml));
    }

    public static XElement GetBodyChild(string xml, string envelopeNamespace)
    {
        var root = ParseRoot(xml);

        Assert.AreEqual("Envelope", root.Name.LocalName, Snippet(xml));

        Assert.AreEqual(envelopeNamespace, root.Name.NamespaceName, Snippet(xml));

        XNamespace ns = envelopeNamespace;

        var body = root.Element(ns + "Body")
                   ?? throw Failed($"{envelopeNamespace} | {Snippet(xml)}");

        return body.Elements().FirstOrDefault()
               ?? throw Failed(Snippet(xml));
    }

    public static XElement GetSoap11BodyChild(string xml) => GetBodyChild(xml, Soap11Ns);

    public static void AssertElementValue(XElement scope, string localName, string expected)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(localName);

        var match = scope.Descendants().FirstOrDefault(element => element.Name.LocalName == localName);

        if (match is null)
        {
            var present = string.Join(", ", scope.Descendants().Select(element => element.Name.LocalName).Distinct());

            throw Failed($"{localName} | {scope.Name.LocalName} | {present}");
        }

        Assert.AreEqual(expected, match.Value, localName);
    }

    public static void AssertContentTypeIsSoap12(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contentType = request.Content?.Headers.ContentType
                          ?? throw Failed("Content-Type");

        Assert.AreEqual(Soap12MediaType, contentType.MediaType, $"{Soap11MediaType} | {contentType}");
    }

    public static void AssertContentTypeIsSoap11(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contentType = request.Content?.Headers.ContentType
                          ?? throw Failed("Content-Type");

        Assert.AreEqual(Soap11MediaType, contentType.MediaType, $"{Soap12MediaType} | {contentType}");
    }

    private static XElement ParseRoot(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw Failed("empty content");

        XDocument document;

        try
        {
            using var reader = SoapXml.CreateReader(new StringReader(xml));
            document = XDocument.Load(reader);
        }
        catch (XmlException exception)
        {
            throw Failed($"{exception.Message} | {Snippet(xml)}");
        }

        return document.Root ?? throw Failed(Snippet(xml));
    }

    private static string Snippet(string xml)
        => xml.Length <= SnippetLength ? xml : $"{xml[..SnippetLength]}... (truncated)";

    private static AssertFailedException Failed(string message) => new(message);
}
