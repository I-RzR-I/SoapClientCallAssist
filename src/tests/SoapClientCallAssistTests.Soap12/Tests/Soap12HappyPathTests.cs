using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class Soap12HappyPathTests
{

    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(30);

    [TestMethod]
    public async Task SendRequest_HelloWorld_ReturnsTheGreetingInASoap12Envelope_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildPost(
            client,
            Soap12FunctionalSupport.HelloWorldBody(),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(client.SendRequest(request), "SendRequest(HelloWorld)");
        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        var payload = SoapAssert.GetBodyChild(envelope);

        Assert.AreEqual(
            (Soap12FunctionalSupport.Service + "HelloWorldResponse").ToString(),
            payload.Name.ToString(),
            "The service must answer with the conventional response element.");
        SoapAssert.AssertElementValue(payload, "HelloWorldResult", "Hello World");
    }

    [TestMethod]
    public async Task SendRequestAsync_HelloWorld_ReturnsTheGreetingInASoap12Envelope_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildPost(
            client,
            Soap12FunctionalSupport.HelloWorldBody(),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync(HelloWorld)");
        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        var payload = SoapAssert.GetBodyChild(envelope);

        Assert.AreEqual(
            (Soap12FunctionalSupport.Service + "HelloWorldResponse").ToString(),
            payload.Name.ToString(),
            "The service must answer with the conventional response element.");
        SoapAssert.AssertElementValue(payload, "HelloWorldResult", "Hello World");
    }

    [TestMethod]
    public async Task SendRequest_EchoValue_ReturnsBothArgumentsJoined_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildPost(
            client,
            Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(client.SendRequest(request), "SendRequest(EchoValue)");
        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        var payload = SoapAssert.GetBodyChild(envelope);
        SoapAssert.AssertElementValue(payload, "EchoValueResult", "abc:7");
    }

    [TestMethod]
    public async Task SendRequestAsync_EchoValue_ReturnsBothArgumentsJoined_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildPost(
            client,
            Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync(EchoValue)");
        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        var payload = SoapAssert.GetBodyChild(envelope);
        SoapAssert.AssertElementValue(payload, "EchoValueResult", "abc:7");
    }

    [TestMethod]
    public async Task SendRequest_AddRecordWithDetail_RoundTripsTheNestedGraphAtDepth_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildPost(
            client,
            Soap12FunctionalSupport.AddRecordWithDetailBody(),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(
            client.SendRequest(request),
            "SendRequest(AddRecordWithDetail)");
        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        var payload = SoapAssert.GetBodyChild(envelope);

        Soap12FunctionalSupport.AssertChildValue(
            payload,
            Soap12FunctionalSupport.Service + "AddRecordWithDetailResult",
            "true");

        var received = Soap12FunctionalSupport.RequireChild(payload, Soap12FunctionalSupport.Service + "ReceivedProduct");

        Soap12FunctionalSupport.AssertChildValue(received, Soap12FunctionalSupport.Service + "Id", "77");
        Soap12FunctionalSupport.AssertChildValue(received, Soap12FunctionalSupport.Service + "Code", "P-77");
        Soap12FunctionalSupport.AssertChildValue(received, Soap12FunctionalSupport.Service + "Name", "Product 77");
        Soap12FunctionalSupport.AssertChildValue(received, Soap12FunctionalSupport.Service + "IsActive", "true");

        var detail = Soap12FunctionalSupport.RequireChild(received, Soap12FunctionalSupport.Service + "Detail");

        Soap12FunctionalSupport.AssertChildValue(detail, Soap12FunctionalSupport.Service + "PartnerId", "177");
        Soap12FunctionalSupport.AssertChildValue(detail, Soap12FunctionalSupport.Service + "ManufacturerId", "277");
        Soap12FunctionalSupport.AssertChildValue(detail, Soap12FunctionalSupport.Service + "SupplierId", "377");
    }

    [TestMethod]
    public async Task SendRequestAsync_GetProduct_ExtractedBodyCarriesTheNestedDetail_Test()
    {
        const int productId = 42;

        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = Soap12FunctionalSupport.BuildPost(
            client,
            Soap12FunctionalSupport.GetProductBody(productId),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            "SendRequestAsync(GetProduct)");
        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        var node = Soap12FunctionalSupport.Unwrap(
            client.GetXmlNodeResponseBody(envelope),
            "GetXmlNodeResponseBody(GetProduct response)");

        var extracted = Soap12FunctionalSupport.ParseRoot(node.OuterXml);

        Assert.AreEqual(
            (Soap12FunctionalSupport.Service + "GetProductResponse").ToString(),
            extracted.Name.ToString(),
            "Extraction must yield the operation response element itself.");

        var result = Soap12FunctionalSupport.RequireChild(extracted, Soap12FunctionalSupport.Service + "GetProductResult");
        var detail = Soap12FunctionalSupport.RequireChild(result, Soap12FunctionalSupport.Service + "Detail");

        Soap12FunctionalSupport.AssertChildValue(result, Soap12FunctionalSupport.Service + "Id", productId.ToString());
        Soap12FunctionalSupport.AssertChildValue(result, Soap12FunctionalSupport.Service + "Code", $"P-{productId}");

        Soap12FunctionalSupport.AssertChildValue(
            detail,
            Soap12FunctionalSupport.Service + "PartnerId",
            (productId + 100).ToString());
        Soap12FunctionalSupport.AssertChildValue(
            detail,
            Soap12FunctionalSupport.Service + "ManufacturerId",
            (productId + 200).ToString());
        Soap12FunctionalSupport.AssertChildValue(
            detail,
            Soap12FunctionalSupport.Service + "SupplierId",
            (productId + 300).ToString());
    }

    [TestMethod]
    public async Task SendRequestAsync_WithLiveCancellationToken_CompletesAndReturnsThePayload_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var cancellation = new CancellationTokenSource(CallTimeout);

        using var request = Soap12FunctionalSupport.BuildPost(
            client,
            Soap12FunctionalSupport.EchoValueBody(Soap12FunctionalSupport.Service, "cancellable", "1"),
            correlationId);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request, cancellation.Token),
            "SendRequestAsync(EchoValue, live token)");
        var envelope = await response.Content.ReadAsStringAsync();

        Soap12FunctionalSupport.AssertRequestWasSoap12OnTheWire(correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Response envelope was: {envelope}");

        var payload = SoapAssert.GetBodyChild(envelope);
        SoapAssert.AssertElementValue(payload, "EchoValueResult", "cancellable:1");

        Assert.IsFalse(
            cancellation.IsCancellationRequested,
            "The call completed only because the deadline had already elapsed, which makes the payload assertion meaningless.");
    }
}
