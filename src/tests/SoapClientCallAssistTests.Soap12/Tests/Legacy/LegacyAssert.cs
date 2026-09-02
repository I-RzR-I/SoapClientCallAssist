using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers;
using System.Net;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Legacy;

internal static class LegacyAssert
{

    internal static string SendPostAndReadEnvelope(
        SoapProtocolType protocol,
        ISoapClientEndpoint client,
        XElement body,
        string context,
        string? action = null)
    {
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildPost(protocol, client, new[] { body }, correlationId, action: action);
        using var response = Soap12FunctionalSupport.Unwrap(client.SendRequest(request), $"SendRequest({context}, {protocol})");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"[{context}, {protocol}] Response envelope was: {envelope}");

        return envelope;
    }

    internal static void AssertIsValidReturnsOne(
        SoapProtocolType protocol,
        ISoapClientEndpoint client,
        XElement body,
        string context)
    {
        var envelope = SendPostAndReadEnvelope(protocol, client, body, context);
        var payload = CrossProtocolSupport.GetBodyChild(protocol, envelope);

        SoapAssert.AssertElementValue(payload, "IsValidResult", "1");
    }
}
