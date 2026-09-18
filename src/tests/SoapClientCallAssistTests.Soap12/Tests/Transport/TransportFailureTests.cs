using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Transport;

[TestClass]
public sealed class TransportFailureTests
{

    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(500);

    private const string SlowOperationDelayMs = "8000";

    private static readonly TimeSpan TimeoutUpperBound = TimeSpan.FromSeconds(5);

    private const string SendFailureMessage = "An error occurred while trying to send SOAP request message.";

    private const string SendAsyncFailureMessage = "An error occurred while trying to send SOAP request message async.";

    private const string InvalidSoapMessage = "Invalid SOAP Message in response.";

    [TestMethod]
    public async Task SendRequest_WhenTheServiceAnswersWith500_ReportsFailureAndStillCarriesTheFault_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "NotFound500");

        var result = client.SendRequest(request);

        Assert.IsFalse(result.IsSuccess, NegativeTestSupport.Describe(result));

        Assert.AreEqual(NegativeTestSupport.HttpSoapFaultCode, NegativeTestSupport.Messages(result)[0].Key, NegativeTestSupport.Describe(result));

        Assert.IsNotNull(result.Response);

        using var response = result.Response;

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var fault = SoapAssert.GetBodyChild(body);

        Assert.AreEqual("Fault", fault.Name.LocalName, $"{body}");
        SoapAssert.AssertElementValue(fault, "Text", "The requested record was not found.");

        SecretLeakAssert.CarriesNoSecret(result, "SendRequest");
    }

    [TestMethod]
    public void SendRequest_WhenTheServiceOutlastsTheTimeout_FailsWithoutThrowing_Test()
    {
        var client = SoapClientFactoryHelper.CreateDirectSoap12Client();

        var timeoutApplied = client.SetClientTimeout(ShortTimeout);

        Assert.IsTrue(timeoutApplied.IsSuccess, NegativeTestSupport.Describe(timeoutApplied));

        using var request = NegativeTestSupport.PostRequest(client, "SlowOp", ("delayMs", SlowOperationDelayMs));

        var stopwatch = Stopwatch.StartNew();

        var result = client.SendRequest(request);

        stopwatch.Stop();

        Assert.IsFalse(result.IsSuccess, NegativeTestSupport.Describe(result));

        Assert.AreEqual(SendFailureMessage, NegativeTestSupport.FirstMessageInfo(result));

        Assert.IsTrue(stopwatch.Elapsed < TimeoutUpperBound, $"{ShortTimeout.TotalMilliseconds} | {SlowOperationDelayMs} | {stopwatch.Elapsed}");

        AssertCarriesNoStackFrame(result, "SendRequest");
    }

    [TestMethod]
    public async Task SendRequestAsync_WithAnAlreadyCancelledToken_FailsWithoutThrowing_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "HelloWorld");
        using var cancellation = new CancellationTokenSource();

        cancellation.Cancel();

        Assert.IsTrue(cancellation.Token.IsCancellationRequested);

        var result = await client.SendRequestAsync(request, cancellation.Token);

        Assert.IsFalse(result.IsSuccess, NegativeTestSupport.Describe(result));

        Assert.AreEqual(SendAsyncFailureMessage, NegativeTestSupport.FirstMessageInfo(result));

        AssertCarriesNoStackFrame(result, "SendRequestAsync");
    }

    private static void AssertCarriesNoStackFrame(IResult result, string what)
    {
        var swept = MessageLeakSweep.Render(result);

        Assert.IsFalse(
            swept.Contains("   at ", StringComparison.Ordinal) || swept.Contains(".cs:line", StringComparison.Ordinal),
            $"{what} | {swept}");
    }

    [TestMethod]
    public void GetXmlNodeResponseBody_WithMalformedXml_FailsGracefully_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.ThrowsException<XmlException>(() => NegativeTestSupport.ParseRoot(NegativeTestSupport.MalformedEnvelopeXml));

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.MalformedEnvelopeXml);

        Assert.IsFalse(result.IsSuccess, NegativeTestSupport.Describe(result));
        Assert.IsNull(result.Response);

        Assert.AreEqual(InvalidSoapMessage, NegativeTestSupport.FirstMessageInfo(result));

        var messages = NegativeTestSupport.Messages(result);

        Assert.IsTrue(messages.Any(message => message.MessageType.ToString() == "Exception"), NegativeTestSupport.Describe(result));
    }
}
