using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests;

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
            "SendRequestAsync(SOAP 1.1, HelloWorld)");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "A well-formed SOAP 1.1 call must be accepted by the service.");

        var recorded = CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(Protocol, correlationId);

        Assert.IsTrue(
            MediaTypeHeaderValue.TryParse(recorded.ContentType, out var parsed),
            $"The request reached the service with an unparseable Content-Type: [{recorded.ContentType ?? "<null>"}].");

        var mediaType = parsed!.MediaType;

        Assert.AreNotEqual(
            SoapAssert.Soap12MediaType,
            mediaType,
            $"The SOAP 1.1 client must never send {SoapAssert.Soap12MediaType} on the wire. Recorded Content-Type was: [{recorded.ContentType}].");

        Assert.AreEqual(
            SoapAssert.Soap11MediaType,
            mediaType,
            $"The SOAP 1.1 client must send {SoapAssert.Soap11MediaType} on the wire. Recorded Content-Type was: [{recorded.ContentType}].");
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
            "SendRequest(SOAP 1.1, HelloWorld)");

        var rawEnvelope = await response.Content.ReadAsStringAsync();

        SoapAssert.AssertIsSoap11Envelope(rawEnvelope);

        Assert.IsFalse(
            rawEnvelope.Contains(SoapAssert.Soap12Ns, StringComparison.Ordinal),
            $"The raw response text must never mention the SOAP 1.2 envelope namespace. Raw response was: {rawEnvelope}");

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
            "BuildRequest(POST, SOAP 1.1, EchoValue, action)");

        Assert.IsNotNull(request.Content, "The built request carries no content.");

        SoapAssert.AssertContentTypeIsSoap11(request);

        var contentType = request.Content.Headers.ContentType!;

        Assert.IsFalse(
            contentType.Parameters.Any(
                parameter => string.Equals(parameter.Name, "ActionParameter", StringComparison.OrdinalIgnoreCase)),
            "ActionParameter is a SOAP 1.2-only content-type parameter, gated in BaseEndpointClient.BuildSoapRequestMessage " +
            $"on SoapProtocolType.SOAP_1_2; a SOAP 1.1 request carrying it would be a defect. Full header was: [{contentType}].");

        Assert.IsFalse(
            contentType.Parameters.Any(
                parameter => string.Equals(parameter.Name, "action", StringComparison.OrdinalIgnoreCase)),
            $"SOAP 1.1 does not carry any content-type action parameter. Full header was: [{contentType}].");

        CollectionAssert.AreEqual(
            new[] { action },
            request.Content.Headers.GetValues("SOAPAction").ToArray(),
            "The SOAPAction content header must still carry the action verbatim under SOAP 1.1.");
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

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync(SOAP 1.1, ThrowFault)");

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode, "A SOAP 1.1 fault is returned with HTTP 500.");

        var body = await response.Content.ReadAsStringAsync();

        SoapAssert.AssertIsSoap11Envelope(body);

        var fault = SoapAssert.GetSoap11BodyChild(body);

        Assert.AreEqual("Fault", fault.Name.LocalName, $"The body child must be a fault. Body was: {body}");
        Assert.AreEqual(SoapAssert.Soap11Ns, fault.Name.NamespaceName, "The fault must be in the SOAP 1.1 envelope namespace.");

        Assert.IsTrue(
            fault.Elements().Any(element => element.Name.LocalName == "faultcode"),
            $"A SOAP 1.1 fault must carry an unqualified faultcode element, not the 1.2 Code/Value shape. Body was: {body}");
        Assert.IsTrue(
            fault.Elements().Any(element => element.Name.LocalName == "faultstring"),
            $"A SOAP 1.1 fault must carry an unqualified faultstring element, not the 1.2 Reason/Text shape. Body was: {body}");

        var faultCode = fault.Elements().First(element => element.Name.LocalName == "faultcode").Value;
        var faultString = fault.Elements().First(element => element.Name.LocalName == "faultstring").Value;

        Assert.AreEqual("soap:Client", faultCode, $"Unexpected fault code. Body was: {body}");
        Assert.AreEqual("The operation failed on purpose.", faultString, $"Unexpected fault string. Body was: {body}");

        var check = client.CheckBodyForFaultCode(body);

        Assert.IsFalse(check.IsSuccess, $"A populated SOAP 1.1 fault must be classified as a failure. Body was: {body}");

        StringAssert.Contains(
            NegativeTestSupport.FirstMessageInfo(check),
            "The operation failed on purpose.",
            "The SOAP 1.1 client must surface the fault string to the caller.");
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

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync(SOAP 1.1 request against the SOAP 1.2 route)");

        Assert.AreEqual(
            HttpStatusCode.UnsupportedMediaType,
            response.StatusCode,
            "The SOAP 1.2 route demands application/soap+xml; a genuine text/xml SOAP 1.1 request must be rejected with 415. " +
            "If this ever returns 200, the SOAP 1.1 client is silently speaking SOAP 1.2 on the wire.");
    }
}
