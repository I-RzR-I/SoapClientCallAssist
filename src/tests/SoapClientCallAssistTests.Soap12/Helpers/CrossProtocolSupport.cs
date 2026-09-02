using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using SoapTestService;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers;

internal static class CrossProtocolSupport
{
    internal static ISoapClientEndpoint CreateClient(SoapProtocolType protocol)
        => protocol == SoapProtocolType.SOAP_1_2
            ? SoapClientFactoryHelper.CreateSoap12Client()
            : SoapClientFactoryHelper.CreateSoap11Client();

    internal static ISoapClientEndpoint CreateDirectClient(SoapProtocolType protocol)
        => protocol == SoapProtocolType.SOAP_1_2
            ? SoapClientFactoryHelper.CreateDirectSoap12Client()
            : SoapClientFactoryHelper.CreateDirectSoap11Client();

    internal static Uri EndpointFor(SoapProtocolType protocol)
        => protocol == SoapProtocolType.SOAP_1_2 ? SoapServiceFixture.ServiceUri : SoapServiceFixture.Service11Uri;

    internal static string ExpectedMediaType(SoapProtocolType protocol)
        => protocol == SoapProtocolType.SOAP_1_2 ? SoapAssert.Soap12MediaType : SoapAssert.Soap11MediaType;

    internal static string ExpectedEnvelopeNamespace(SoapProtocolType protocol)
        => protocol == SoapProtocolType.SOAP_1_2 ? SoapAssert.Soap12Ns : SoapAssert.Soap11Ns;

    internal static XElement GetBodyChild(SoapProtocolType protocol, string xml)
        => SoapAssert.GetBodyChild(xml, ExpectedEnvelopeNamespace(protocol));

    internal static HttpRequestMessage BuildPost(
        SoapProtocolType protocol,
        ISoapClientEndpoint client,
        IEnumerable<XElement> bodies,
        string correlationId,
        Uri? endpoint = null,
        string? action = null)
        => Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Post,
                endpoint ?? EndpointFor(protocol),
                bodies,
                action: action,
                httpClientHeaders: Soap12FunctionalSupport.CorrelationHeaders(correlationId)),
            $"BuildRequest(POST, {protocol})");

    internal static HttpRequestMessage BuildGet(
        SoapProtocolType protocol,
        ISoapClientEndpoint client,
        IEnumerable<XElement> bodies,
        string correlationId,
        bool buildGetRequestAsSlashUrl,
        string? action = null)
        => Soap12FunctionalSupport.Unwrap(
            client.BuildRequest(
                HttpMethod.Get,
                EndpointFor(protocol),
                bodies,
                action: action,
                httpClientHeaders: Soap12FunctionalSupport.CorrelationHeaders(correlationId),
                buildGetRequestAsSlashUrl: buildGetRequestAsSlashUrl),
            $"BuildRequest(GET, {protocol})");

    internal static RecordedRequest AssertRequestWasOnTheWireForProtocol(SoapProtocolType protocol, string correlationId)
    {
        var recorded = Soap12FunctionalSupport.FindRecorded(correlationId);

        Assert.IsTrue(
            MediaTypeHeaderValue.TryParse(recorded.ContentType, out var parsed),
            $"The request reached the service with an unparseable Content-Type: [{recorded.ContentType ?? "<null>"}].");

        Assert.AreEqual(
            ExpectedMediaType(protocol),
            parsed!.MediaType,
            $"A {protocol} request must reach the service with the {protocol} media type. Full header was: [{recorded.ContentType}].");

        return recorded;
    }
}
