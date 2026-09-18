using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

[TestClass]
public sealed class Soap11ProtocolMarkersTests
{
    private const SoapProtocolType Protocol = SoapProtocolType.SOAP_1_1;

    [TestMethod]
    public async Task SendRequest_HelloWorld_SendsTextXmlOnTheWireNotApplicationSoapXml_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            Protocol,
            client,
            Soap12FunctionalSupport.HelloWorldBody(),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var recorded = CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(Protocol, correlationId);

        Assert.IsTrue(MediaTypeHeaderValue.TryParse(recorded.ContentType, out var parsed), $"{recorded.ContentType ?? "<null>"}");

        var mediaType = parsed!.MediaType;

        Assert.AreNotEqual(SoapAssert.Soap12MediaType, mediaType, $"{recorded.ContentType}");

        Assert.AreEqual(SoapAssert.Soap11MediaType, mediaType, $"{recorded.ContentType}");
    }

    [TestMethod]
    public async Task SendRequest_HelloWorld_RawResponseEnvelopeIsInTheXmlSoapOrgNamespaceBeforeAnyReaderRunsOnIt_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            Protocol,
            client,
            Soap12FunctionalSupport.HelloWorldBody(),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(
            client.SendRequest(request),
            "SendRequest");

        var rawEnvelope = await response.Content.ReadAsStringAsync();

        SoapAssert.AssertIsSoap11Envelope(rawEnvelope);

        Assert.IsFalse(rawEnvelope.Contains(SoapAssert.Soap12Ns, StringComparison.Ordinal), $"{rawEnvelope}");

        var payload = SoapAssert.GetSoap11BodyChild(rawEnvelope);
        SoapAssert.AssertElementValue(payload, "HelloWorldResult", "Hello World");
    }

    [TestMethod]
    public void BuildRequest_WithAction_OmitsTheSoap12OnlyActionParameterContentTypeParameter_Test()
    {
        const string action = SoapAssert.ServiceNs + "EchoValue";
        var client = CrossProtocolSupport.CreateClient(Protocol);

        using var request = Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                SoapServiceFixture.Service11Uri,
                Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
                action: action),
            "BuildRequest");

        Assert.IsNotNull(request.Content);

        SoapAssert.AssertContentTypeIsSoap11(request);

        var contentType = request.Content.Headers.ContentType!;

        Assert.IsFalse(
            contentType.Parameters.Any(
                parameter => string.Equals(parameter.Name, "ActionParameter", StringComparison.OrdinalIgnoreCase)),
            $"{contentType}");

        Assert.IsFalse(
            contentType.Parameters.Any(
                parameter => string.Equals(parameter.Name, "action", StringComparison.OrdinalIgnoreCase)),
            $"{contentType}");

        CollectionAssert.AreEqual(new[] { action }, request.Content.Headers.GetValues("SOAPAction").ToArray());
    }

    [TestMethod]
    public async Task CheckBodyForFaultCode_WithLiveSoap11Fault_FailsAndTheRawShapeIsFaultcodeFaultstring_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            Protocol,
            client,
            NegativeTestSupport.Bodies("ThrowFault"),
            correlationId);

        using var response = Soap12FunctionalSupport.UnwrapRejected(
            await client.SendRequestAsync(request),
            NegativeTestSupport.HttpSoapFaultCode,
            "SendRequestAsync");

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        SoapAssert.AssertIsSoap11Envelope(body);

        var fault = SoapAssert.GetSoap11BodyChild(body);

        Assert.AreEqual("Fault", fault.Name.LocalName, $"{body}");
        Assert.AreEqual(SoapAssert.Soap11Ns, fault.Name.NamespaceName);

        Assert.IsTrue(fault.Elements().Any(element => element.Name.LocalName == "faultcode"), $"{body}");
        Assert.IsTrue(fault.Elements().Any(element => element.Name.LocalName == "faultstring"), $"{body}");

        var faultCode = fault.Elements().First(element => element.Name.LocalName == "faultcode").Value;
        var faultString = fault.Elements().First(element => element.Name.LocalName == "faultstring").Value;

        Assert.AreEqual("soap:Client", faultCode, $"{body}");
        Assert.AreEqual("The operation failed on purpose.", faultString, $"{body}");

        var check = client.CheckBodyForFaultCode(body);

        Assert.IsFalse(check.IsSuccess, $"{body}");

        StringAssert.Contains(NegativeTestSupport.FirstMessageInfo(check), "The operation failed on purpose.");
    }

    [TestMethod]
    public async Task SendRequest_WhenASoap11RequestTargetsTheSoap12Route_TheServiceRejectsWithUnsupportedMediaType_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(
            Protocol,
            client,
            Soap12FunctionalSupport.HelloWorldBody(),
            correlationId,
            endpoint: SoapServiceFixture.ServiceUri);

        using var response = Soap12FunctionalSupport.UnwrapRejected(
            await client.SendRequestAsync(request),
            NegativeTestSupport.HttpClientErrorCode,
            "SendRequestAsync");

        Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }
}
