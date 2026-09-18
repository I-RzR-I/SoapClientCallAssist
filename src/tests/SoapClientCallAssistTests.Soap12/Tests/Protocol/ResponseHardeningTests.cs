using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

[TestClass]
public sealed class ResponseHardeningTests
{
    private const string PayloadElementName = "HelloWorldResponse";

    private const string CommentMarker = "ZZQ-ATTACKER-COMMENT-5e1c";

    private const string InstructionMarker = "ZZQ-ATTACKER-PI-9a7f";

    private const string DtdMarker = "ZZQ-DTD-MARKER-2b6d";

    private const string DepthCode = "ER-XML-DEPTH";

    private const string InvalidSoapCode = "ER-BEC-GRB-03";

    private const string FaultCheckErrorCode = "ER-BEC-CBFFC";

    private const int MaxXmlDepth = 64;

    private const int EnvelopeAndBodyDepth = 2;

    private static string Envelope(string bodyContent)
        => "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
           + "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\">"
           + $"<soap:Body>{bodyContent}</soap:Body>"
           + "</soap:Envelope>";

    private static string Payload(string inner = "")
        => $"<{PayloadElementName} xmlns=\"http://SoapClientCallAssist.local/\">{inner}<HelloWorldResult>Hello World</HelloWorldResult></{PayloadElementName}>";

    private static string Comment() => $"<!--{CommentMarker}-->";

    private static string Instruction() => $"<?attacker {InstructionMarker}?>";

    internal static string NestedEnvelope(int totalElementDepth)
    {
        var chain = new StringBuilder();
        var chainLength = totalElementDepth - EnvelopeAndBodyDepth;

        for (var index = 0; index < chainLength; index++)
            chain.Append("<n>");

        for (var index = 0; index < chainLength; index++)
            chain.Append("</n>");

        return Envelope(chain.ToString());
    }

    private static string Key(IResult result) => NegativeTestSupport.Messages(result)[0].Key;

    [DataTestMethod]
    [DataRow(true, DisplayName = "comment before the payload, at Body level")]
    [DataRow(false, DisplayName = "processing instruction before the payload, at Body level")]
    public void GetXmlNodeResponseBody_WithUnsignedNodeInFrontOfThePayload_ReturnsThePayloadElement_Test(bool comment)
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var injected = comment ? Comment() : Instruction();
        var marker = comment ? CommentMarker : InstructionMarker;
        var wire = Envelope(injected + Payload());

        Assert.IsTrue(wire.Contains(marker));

        var result = client.GetXmlNodeResponseBody(wire);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));
        Assert.AreEqual(XmlNodeType.Element, result.Response.NodeType);
        Assert.AreEqual(PayloadElementName, result.Response.LocalName);
        Assert.IsFalse(result.Response.OuterXml.Contains(marker), $"{result.Response.OuterXml}");
    }

    [DataTestMethod]
    [DataRow(true, DisplayName = "comment nested inside the payload")]
    [DataRow(false, DisplayName = "processing instruction nested inside the payload")]
    public void GetXmlNodeResponseBody_WithUnsignedNodeOneLevelDeeper_DropsItFromThePayload_Test(bool comment)
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var injected = comment ? Comment() : Instruction();
        var marker = comment ? CommentMarker : InstructionMarker;
        var wire = Envelope(Payload(injected));

        Assert.IsTrue(wire.Contains(marker));

        var result = client.GetXmlNodeResponseBody(wire);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));
        Assert.AreEqual(PayloadElementName, result.Response.LocalName);

        Assert.IsFalse(result.Response.InnerXml.Contains(marker), $"{result.Response.InnerXml}");

        Assert.AreEqual("Hello World", result.Response.InnerText);
    }

    [TestMethod]
    public void GetXNodeResponseBody_WithUnsignedNodesAtBothDepths_ReturnsThePayloadDocumentWithoutThem_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var wire = Envelope(Comment() + Instruction() + Payload(Comment() + Instruction()));

        var result = client.GetXNodeResponseBody(wire);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));

        var document = result.Response as XDocument;

        Assert.IsNotNull(document, $"{result.Response?.GetType().Name}");
        Assert.AreEqual(PayloadElementName, document!.Root!.Name.LocalName);
        Assert.IsFalse(document.DescendantNodes().OfType<XComment>().Any());
        Assert.IsFalse(document.DescendantNodes().OfType<XProcessingInstruction>().Any());
    }

    [TestMethod]
    public void GetXmlNodeResponseBody_WithADtd_IsRejectedWithoutEchoingIt_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var wire =
            "<?xml version=\"1.0\"?>"
            + $"<!DOCTYPE Envelope [<!ENTITY marker \"{DtdMarker}\">]>"
            + "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\">"
            + $"<soap:Body>{Payload()}</soap:Body></soap:Envelope>";

        var result = client.GetXmlNodeResponseBody(wire);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(InvalidSoapCode, Key(result), NegativeTestSupport.Describe(result));
        Assert.IsFalse(NegativeTestSupport.Describe(result).Contains(DtdMarker));
    }

    [TestMethod]
    public void CheckBodyForFaultCode_WithADtd_IsRejectedRatherThanReadAsNoFault_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var wire =
            "<?xml version=\"1.0\"?>"
            + $"<!DOCTYPE Envelope [<!ENTITY marker \"{DtdMarker}\">]>"
            + "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\">"
            + $"<soap:Body>{Payload()}</soap:Body></soap:Envelope>";

        var result = client.CheckBodyForFaultCode(wire);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(FaultCheckErrorCode, Key(result), NegativeTestSupport.Describe(result));
        Assert.IsFalse(NegativeTestSupport.Describe(result).Contains(DtdMarker));
    }

    [TestMethod]
    public void GetXmlNodeResponseBody_NestingJustInsideTheDepthCap_IsAccepted_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.GetXmlNodeResponseBody(NestedEnvelope(MaxXmlDepth));

        Assert.IsTrue(result.IsSuccess, $"{MaxXmlDepth} | {NegativeTestSupport.Describe(result)}");
        Assert.AreEqual("n", result.Response.LocalName);
    }

    [TestMethod]
    public void GetXmlNodeResponseBody_NestingOnePastTheDepthCap_FailsWithTheDepthCode_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.GetXmlNodeResponseBody(NestedEnvelope(MaxXmlDepth + 1));

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(DepthCode, Key(result), NegativeTestSupport.Describe(result));
    }

    [TestMethod]
    public void GetXNodeResponseBody_NestingOnePastTheDepthCap_FailsWithTheDepthCode_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.GetXNodeResponseBody(NestedEnvelope(MaxXmlDepth + 1));

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(DepthCode, Key(result), NegativeTestSupport.Describe(result));
    }

    [TestMethod]
    public void CheckBodyForFaultCode_NestingOnePastTheDepthCap_FailsWithTheDepthCode_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.CheckBodyForFaultCode(NestedEnvelope(MaxXmlDepth + 1));

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(DepthCode, Key(result), NegativeTestSupport.Describe(result));
    }

    [TestMethod]
    public void CheckBodyForFaultCode_NestingJustInsideTheDepthCap_IsAccepted_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var result = client.CheckBodyForFaultCode(NestedEnvelope(MaxXmlDepth));

        Assert.IsTrue(result.IsSuccess, $"{MaxXmlDepth} | {NegativeTestSupport.Describe(result)}");
    }
}
