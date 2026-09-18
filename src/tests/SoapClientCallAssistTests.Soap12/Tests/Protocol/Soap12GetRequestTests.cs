using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests.Protocol;

[TestClass]
public sealed class Soap12GetRequestTests
{

    [TestMethod]
    public async Task BuildAndSendGet_QueryForm_AddressesTheOperationAndEchoesBothArguments_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildGet(
            client,
            Soap12FunctionalSupport.EchoValueBody(XNamespace.None),
            correlationId,
            buildGetRequestAsSlashUrl: false);

        var expected = $"{SoapServiceFixture.ServiceUri}/EchoValue?value=abc&count=7";

        Assert.IsNotNull(request.RequestUri);
        Assert.AreEqual(expected, request.RequestUri.ToString());
        Assert.AreEqual(expected, request.RequestUri.AbsoluteUri);

        SoapAssert.AssertContentTypeIsSoap12(request);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync");
        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

        SoapAssert.AssertElementValue(SoapAssert.GetBodyChild(envelope), "EchoValueResult", "abc:7");
    }

    [TestMethod]
    public async Task BuildAndSendGet_SlashForm_AppendsValuesAsPathSegmentsInOrder_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildGet(
            client,
            Soap12FunctionalSupport.EchoValueBody(XNamespace.None),
            correlationId,
            buildGetRequestAsSlashUrl: true);

        var expected = $"{SoapServiceFixture.ServiceUri}/EchoValue/abc/7";

        Assert.IsNotNull(request.RequestUri);
        Assert.AreEqual(expected, request.RequestUri.ToString());
        Assert.AreEqual(expected, request.RequestUri.AbsoluteUri);

        SoapAssert.AssertContentTypeIsSoap12(request);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync");
        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

        SoapAssert.AssertElementValue(SoapAssert.GetBodyChild(envelope), "EchoValueResult", "abc:7");
    }

    [TestMethod]
    public void BuildGet_WithNamespacedBody_LeaksTheNamespaceIntoThePathSegment_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildGet(
            client,
            Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
            correlationId,
            buildGetRequestAsSlashUrl: false);

        Assert.IsNotNull(request.RequestUri);

        var expectedUnescaped = $"{SoapServiceFixture.ServiceUri}/{{{SoapAssert.ServiceNs}}}EchoValue?value=abc&count=7";
        var expectedEscaped = $"{SoapServiceFixture.ServiceUri}/%7B{SoapAssert.ServiceNs}%7DEchoValue?value=abc&count=7";

        Assert.AreEqual(expectedUnescaped, request.RequestUri.ToString());

        Assert.AreEqual(expectedEscaped, request.RequestUri.AbsoluteUri);

        SoapAssert.AssertContentTypeIsSoap12(request);
    }

    [TestMethod]
    [Ignore("DEFECT-SOAP12-GET-QNAME: unpins when fixed")]
    public void BuildGet_WithNamespacedBody_ShouldUseOnlyTheLocalNameInThePathSegment_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildGet(
            client,
            Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
            correlationId,
            buildGetRequestAsSlashUrl: false);

        var expected = $"{SoapServiceFixture.ServiceUri}/EchoValue?value=abc&count=7";

        Assert.IsNotNull(request.RequestUri);
        Assert.AreEqual(expected, request.RequestUri.ToString());
        Assert.AreEqual(expected, request.RequestUri.AbsoluteUri);
    }

    [TestMethod]
    public async Task SendGet_StillShipsASoap12EnvelopeThatHasNoBodyElement_Test()
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
            "SendRequestAsync");

        var recorded = Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);

        Assert.AreEqual("GET", recorded.Method);
        Assert.IsFalse(string.IsNullOrWhiteSpace(recorded.Body));

        var root = Soap12FunctionalSupport.AssertEnvelopeNamespace(recorded.Body, SoapAssert.Soap12Ns);

        Assert.IsNull(root.Element(Soap12FunctionalSupport.Soap12 + "Body"), $"{recorded.Body}");

        var header = Soap12FunctionalSupport.RequireChild(root, Soap12FunctionalSupport.Soap12 + "Header");

        Soap12FunctionalSupport.AssertChildValue(header, "Action", action);
    }
}
