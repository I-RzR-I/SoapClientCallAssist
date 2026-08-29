using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class Soap12GetRequestTests
{

    [TestMethod]
    public async Task BuildAndSendGet_QueryForm_AddressesTheOperationAndEchoesBothArguments()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildGet(
            client,
            Soap12FunctionalSupport.EchoValueBody(XNamespace.None),
            correlationId,
            buildGetRequestAsSlashUrl: false);

        var expected = $"{SoapServiceFixture.ServiceUri}/EchoValue?value=abc&count=7";

        Assert.IsNotNull(request.RequestUri, "The GET build produced no request URI.");
        Assert.AreEqual(expected, request.RequestUri.ToString(), "RequestUri.ToString() (unescaped form).");
        Assert.AreEqual(expected, request.RequestUri.AbsoluteUri, "RequestUri.AbsoluteUri (escaped form).");

        SoapAssert.AssertContentTypeIsSoap12(request);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync(GET EchoValue, query form)");
        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        SoapAssert.AssertElementValue(SoapAssert.GetBodyChild(envelope), "EchoValueResult", "abc:7");
    }

    [TestMethod]
    public async Task BuildAndSendGet_SlashForm_AppendsValuesAsPathSegmentsInOrder()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildGet(
            client,
            Soap12FunctionalSupport.EchoValueBody(XNamespace.None),
            correlationId,
            buildGetRequestAsSlashUrl: true);

        var expected = $"{SoapServiceFixture.ServiceUri}/EchoValue/abc/7";

        Assert.IsNotNull(request.RequestUri, "The GET build produced no request URI.");
        Assert.AreEqual(expected, request.RequestUri.ToString(), "RequestUri.ToString() (unescaped form).");
        Assert.AreEqual(expected, request.RequestUri.AbsoluteUri, "RequestUri.AbsoluteUri (escaped form).");

        SoapAssert.AssertContentTypeIsSoap12(request);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync(GET EchoValue, slash form)");
        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        SoapAssert.AssertElementValue(SoapAssert.GetBodyChild(envelope), "EchoValueResult", "abc:7");
    }

    [TestMethod]
    public void BuildGet_WithNamespacedBody_LeaksTheNamespaceIntoThePathSegment()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildGet(
            client,
            Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
            correlationId,
            buildGetRequestAsSlashUrl: false);

        Assert.IsNotNull(request.RequestUri, "The GET build produced no request URI.");

        var expectedUnescaped = $"{SoapServiceFixture.ServiceUri}/{{{SoapAssert.ServiceNs}}}EchoValue?value=abc&count=7";
        var expectedEscaped = $"{SoapServiceFixture.ServiceUri}/%7B{SoapAssert.ServiceNs}%7DEchoValue?value=abc&count=7";

        Assert.AreEqual(
            expectedUnescaped,
            request.RequestUri.ToString(),
            "RequestUri.ToString() (unescaped form) renders the braces literally.");

        Assert.AreEqual(
            expectedEscaped,
            request.RequestUri.AbsoluteUri,
            "RequestUri.AbsoluteUri (escaped form) percent-encodes the braces as %7B and %7D.");

        SoapAssert.AssertContentTypeIsSoap12(request);
    }

    [TestMethod]
    [Ignore("DEFECT-SOAP12-GET-QNAME: unpins when fixed")]
    public void BuildGet_WithNamespacedBody_ShouldUseOnlyTheLocalNameInThePathSegment()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildGet(
            client,
            Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
            correlationId,
            buildGetRequestAsSlashUrl: false);

        var expected = $"{SoapServiceFixture.ServiceUri}/EchoValue?value=abc&count=7";

        Assert.IsNotNull(request.RequestUri, "The GET build produced no request URI.");
        Assert.AreEqual(expected, request.RequestUri.ToString(), "RequestUri.ToString() (unescaped form).");
        Assert.AreEqual(expected, request.RequestUri.AbsoluteUri, "RequestUri.AbsoluteUri (escaped form).");
    }

    [TestMethod]
    public async Task SendGet_StillShipsASoap12EnvelopeThatHasNoBodyElement()
    {
        const string action = SoapAssert.ServiceNs + "EchoValue";

        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildGet(
            client,
            Soap12FunctionalSupport.EchoValueBody(XNamespace.None),
            correlationId,
            buildGetRequestAsSlashUrl: false,
            action: action);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync(GET EchoValue, with action)");

        var recorded = Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);

        Assert.AreEqual("GET", recorded.Method, "The recorded call must be the GET under test.");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(recorded.Body),
            "A GET issued by this client is expected to carry an envelope body; nothing arrived.");

        var root = Soap12FunctionalSupport.AssertEnvelopeNamespace(recorded.Body, SoapAssert.Soap12Ns);

        Assert.IsNull(
            root.Element(Soap12FunctionalSupport.Soap12 + "Body"),
            $"A GET envelope must have no Body element; the arguments travel in the request line. Envelope was: {recorded.Body}");

        var header = Soap12FunctionalSupport.RequireChild(root, Soap12FunctionalSupport.Soap12 + "Header");

        Soap12FunctionalSupport.AssertChildValue(header, "Action", action);
    }
}
