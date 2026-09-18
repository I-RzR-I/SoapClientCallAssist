using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

[TestClass]
public sealed class ResponseBodyExtractionTests
{
    private const string PayloadElementName = "HelloWorldResponse";

    private const string NoSingleBodyMessage = "No or more than one SOAP Body in response.";

    [TestMethod]
    public void GetXmlNodeResponseBody_WithUnindentedEnvelope_ReturnsThePayloadElement_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.UnindentedSoap12Envelope);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));
        Assert.IsNotNull(result.Response);

        Assert.AreEqual(XmlNodeType.Element, result.Response.NodeType);
        Assert.AreEqual(PayloadElementName, result.Response.LocalName);
    }

    [TestMethod]
    public void GetXmlNodeResponseBody_WithIndentedEnvelope_ShouldReturnThePayloadElement_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.IndentedSoap12Envelope);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(XmlNodeType.Element, result.Response.NodeType);
        Assert.AreEqual(PayloadElementName, result.Response.LocalName);
    }

    [TestMethod]
    public void GetXNodeResponseBody_WithIndentedEnvelope_ReturnsThePayloadDocument_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.AreEqual(PayloadElementName, SoapAssert.GetBodyChild(NegativeTestSupport.IndentedSoap12Envelope).Name.LocalName);

        var result = client.GetXNodeResponseBody(NegativeTestSupport.IndentedSoap12Envelope);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));

        var document = result.Response as XDocument;

        Assert.IsNotNull(document, $"{result.Response?.GetType().Name}");
        Assert.IsNotNull(document!.Root);

        Assert.AreEqual(XName.Get(PayloadElementName, SoapAssert.ServiceNs), document.Root!.Name);
    }

    [DataTestMethod]
    [DataRow("Header", DisplayName = "bare Header tag")]
    [DataRow("soap:Header", DisplayName = "prefixed Header tag")]
    [DataRow("HelloWorldResponse", DisplayName = "payload element tag")]
    public void GetXmlNodeResponseBody_WithABodyTagNamingAnotherElement_RefusesIt_Test(string bodyTag)
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.IsTrue(NegativeTestSupport.Soap12HeaderFaultEnvelope.Contains("<soap:Header>"));

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.Soap12HeaderFaultEnvelope, soapXmlBodyTag: bodyTag);

        Assert.IsFalse(result.IsSuccess, NegativeTestSupport.Describe(result));

        Assert.AreEqual(NoSingleBodyMessage, NegativeTestSupport.FirstMessageInfo(result));
    }

    [DataTestMethod]
    [DataRow("Body", DisplayName = "bare Body tag")]
    [DataRow("s:Body", DisplayName = "s-prefixed Body tag on a soap-prefixed envelope")]
    [DataRow("SOAP-ENV:Body", DisplayName = "SOAP-ENV-prefixed Body tag on a soap-prefixed envelope")]
    public void GetXmlNodeResponseBody_WithABodyTagUnderAnyPrefix_StillFindsTheBody_Test(string bodyTag)
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.UnindentedSoap12Envelope, soapXmlBodyTag: bodyTag);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));

        Assert.AreEqual(PayloadElementName, result.Response.LocalName);
    }

    [TestMethod]
    public void GetXmlNodeResponseBody_WhenTheEnvelopeUsesAnotherPrefix_ShouldStillFindTheBody_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.AreEqual(PayloadElementName, SoapAssert.GetBodyChild(NegativeTestSupport.EnvPrefixedSoap12Envelope).Name.LocalName);

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.EnvPrefixedSoap12Envelope);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));

        Assert.AreEqual(PayloadElementName, result.Response.LocalName);
    }

    [TestMethod]
    public void GetXmlNodeResponseBody_WithSPrefixedEnvelope_ShouldUseTheFallback_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.AreEqual(PayloadElementName, SoapAssert.GetBodyChild(NegativeTestSupport.SPrefixedSoap12Envelope).Name.LocalName);

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.SPrefixedSoap12Envelope);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));

        Assert.AreEqual(PayloadElementName, result.Response.LocalName);
    }
}
