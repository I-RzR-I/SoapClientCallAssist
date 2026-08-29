using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class ResponseBodyExtractionTests
{
    private const string PayloadElementName = "HelloWorldResponse";

    private const string NoSingleBodyMessage = "No or more than one SOAP Body in response.";

    private const string InvalidSoapMessage = "Invalid SOAP Message in response.";

    [TestMethod]
    public void GetXmlNodeResponseBody_WithUnindentedEnvelope_ReturnsThePayloadElement()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.UnindentedSoap12Envelope);

        Assert.IsTrue(result.IsSuccess, $"The control envelope must extract cleanly. Got: {NegativeTestSupport.Describe(result)}");
        Assert.IsNotNull(result.Response, "A successful extraction must carry a node.");

        Assert.AreEqual(XmlNodeType.Element, result.Response.NodeType, "The extracted node must be the payload element.");
        Assert.AreEqual(PayloadElementName, result.Response.LocalName, "The extracted node must be the operation response element.");
    }

    [TestMethod]
    public void GetXmlNodeResponseBody_WithIndentedEnvelope_ReturnsAWhitespaceNodeInsteadOfThePayload()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.IndentedSoap12Envelope);

        Assert.IsTrue(
            result.IsSuccess,
            $"Pinning current behaviour: the extraction reports success. Got: {NegativeTestSupport.Describe(result)}");

        Assert.IsNotNull(result.Response, "The extraction reported success, so it must carry a node.");

        var node = result.Response;

        Assert.AreNotEqual(
            XmlNodeType.Element,
            node.NodeType,
            "Pinning current behaviour: the node handed back is not an element.");

        Assert.IsTrue(
            node.NodeType is XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace or XmlNodeType.Text,
            $"Expected the indentation text node; the node type was {node.NodeType}.");

        Assert.IsNotNull(node.Value, "The indentation node must carry its whitespace as a value.");
        Assert.AreEqual(0, node.Value.Trim().Length, $"The returned node must be whitespace only, but it was [{node.Value}].");
        Assert.AreNotEqual(PayloadElementName, node.LocalName, "Pinning current behaviour: the payload element is not what comes back.");
    }

    [TestMethod]
    [Ignore("DEFECT-WS-FIRSTCHILD: unpins when fixed")]
    public void GetXmlNodeResponseBody_WithIndentedEnvelope_ShouldReturnThePayloadElement()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.IndentedSoap12Envelope);

        Assert.IsTrue(result.IsSuccess, "An indented response is still a valid response.");
        Assert.AreEqual(PayloadElementName, result.Response.LocalName, "Indentation must not change which node is extracted.");
    }

    [TestMethod]
    public void GetXNodeResponseBody_WithIndentedEnvelope_RejectsAValidResponseAsInvalid()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.AreEqual(
            PayloadElementName,
            SoapAssert.GetBodyChild(NegativeTestSupport.IndentedSoap12Envelope).Name.LocalName,
            "The indented fixture must be a valid SOAP 1.2 envelope carrying the payload.");

        var result = client.GetXNodeResponseBody(NegativeTestSupport.IndentedSoap12Envelope);

        Assert.IsFalse(
            result.IsSuccess,
            $"Pinning current behaviour: an indented but valid response is rejected. Got: {NegativeTestSupport.Describe(result)}");

        Assert.AreEqual(
            InvalidSoapMessage,
            NegativeTestSupport.FirstMessageInfo(result),
            "Pinning current behaviour: the caller is told the response was invalid.");
    }

    [TestMethod]
    public void GetXmlNodeResponseBody_WhenTheEnvelopeUsesAnotherPrefix_FailsAlthoughTheNamespaceIsCorrect()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.AreEqual(
            PayloadElementName,
            SoapAssert.GetBodyChild(NegativeTestSupport.EnvPrefixedSoap12Envelope).Name.LocalName,
            "The env-prefixed fixture must be a valid SOAP 1.2 envelope carrying the payload.");

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.EnvPrefixedSoap12Envelope);

        Assert.IsFalse(
            result.IsSuccess,
            $"Pinning current behaviour: a correctly namespaced envelope on another prefix is not understood. Got: {NegativeTestSupport.Describe(result)}");

        Assert.AreEqual(
            NoSingleBodyMessage,
            NegativeTestSupport.FirstMessageInfo(result),
            "Pinning current behaviour: the caller is told there is no single body.");
    }

    [TestMethod]
    [Ignore("DEFECT-PREFIX-BOUND-BODY-LOOKUP: unpins when fixed")]
    public void GetXmlNodeResponseBody_WhenTheEnvelopeUsesAnotherPrefix_ShouldStillFindTheBody()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.EnvPrefixedSoap12Envelope);

        Assert.IsTrue(result.IsSuccess, "The prefix carries no meaning; the namespace does.");
        Assert.AreEqual(PayloadElementName, result.Response.LocalName, "The payload must be found regardless of the prefix.");
    }

    [TestMethod]
    public void GetXmlNodeResponseBody_WithSPrefixedEnvelope_FailsAlthoughAFallbackForItExists()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.AreEqual(
            PayloadElementName,
            SoapAssert.GetBodyChild(NegativeTestSupport.SPrefixedSoap12Envelope).Name.LocalName,
            "The s-prefixed fixture must be a valid SOAP 1.2 envelope carrying the payload.");

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.SPrefixedSoap12Envelope);

        Assert.IsFalse(
            result.IsSuccess,
            $"Pinning current behaviour: the s-prefixed fallback never runs. Got: {NegativeTestSupport.Describe(result)}");

        Assert.AreEqual(
            NoSingleBodyMessage,
            NegativeTestSupport.FirstMessageInfo(result),
            "Pinning current behaviour: the caller is told there is no single body.");
    }

    [TestMethod]
    [Ignore("DEFECT-S-BODY-FALLBACK-DEAD: unpins when fixed")]
    public void GetXmlNodeResponseBody_WithSPrefixedEnvelope_ShouldUseTheFallback()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.SPrefixedSoap12Envelope);

        Assert.IsTrue(result.IsSuccess, "The s:Body fallback exists precisely for this shape and must be reachable.");
        Assert.AreEqual(PayloadElementName, result.Response.LocalName, "The payload must be found through the fallback.");
    }
}
