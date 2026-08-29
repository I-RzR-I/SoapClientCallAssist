using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapTestService;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers;

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

        Assert.AreEqual(
            "Envelope",
            root.Name.LocalName,
            $"Expected the root element to be Envelope. Document was: {Snippet(xml)}");

        Assert.AreEqual(
            Soap12Ns,
            root.Name.NamespaceName,
            $"Expected the SOAP 1.2 envelope namespace. SOAP 1.1 uses {Soap11Ns}. Document was: {Snippet(xml)}");

        return root;
    }

    public static void AssertEnvelopeNamespaceIsNot11(string xml)
    {
        var root = ParseRoot(xml);

        Assert.AreNotEqual(
            Soap11Ns,
            root.Name.NamespaceName,
            $"The envelope is in the SOAP 1.1 namespace but SOAP 1.2 was expected. Document was: {Snippet(xml)}");
    }

    public static XElement GetBodyChild(string xml)
    {
        var root = AssertIsSoap12Envelope(xml);

        var body = root.Element(Soap12Namespace + "Body")
                   ?? throw Failed($"The envelope has no SOAP 1.2 Body element. Document was: {Snippet(xml)}");

        return body.Elements().FirstOrDefault()
               ?? throw Failed($"The SOAP body is empty. Document was: {Snippet(xml)}");
    }

    public static void AssertElementValue(XElement scope, string localName, string expected)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(localName);

        var match = scope.Descendants().FirstOrDefault(element => element.Name.LocalName == localName);

        if (match is null)
        {
            var present = string.Join(", ", scope.Descendants().Select(element => element.Name.LocalName).Distinct());

            throw Failed(
                $"No descendant element named {localName} was found under {scope.Name.LocalName}. " +
                $"Elements present: [{present}].");
        }

        Assert.AreEqual(expected, match.Value, $"Unexpected value for element {localName}.");
    }

    public static void AssertContentTypeIsSoap12(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contentType = request.Content?.Headers.ContentType
                          ?? throw Failed("The request carries no Content-Type header, so it cannot be a SOAP 1.2 request.");

        Assert.AreEqual(
            Soap12MediaType,
            contentType.MediaType,
            $"Expected the SOAP 1.2 media type; SOAP 1.1 would produce {Soap11MediaType}. " +
            $"The full header was: {contentType}");
    }

    private static XElement ParseRoot(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw Failed("Expected an XML document but the content was null, empty or whitespace.");

        XDocument document;

        try
        {
            using var reader = SoapXml.CreateReader(new StringReader(xml));
            document = XDocument.Load(reader);
        }
        catch (XmlException exception)
        {
            throw Failed($"The content is not well formed XML: {exception.Message}. Content was: {Snippet(xml)}");
        }

        return document.Root ?? throw Failed($"The document has no root element. Content was: {Snippet(xml)}");
    }

    private static string Snippet(string xml)
        => xml.Length <= SnippetLength ? xml : $"{xml[..SnippetLength]}... (truncated)";

    private static AssertFailedException Failed(string message) => new(message);
}
