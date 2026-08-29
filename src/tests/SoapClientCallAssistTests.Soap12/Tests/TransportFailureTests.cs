using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests;

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
    public async Task SendRequest_WhenTheServiceAnswersWith500_ReportsSuccessAndLeavesTheStatusUnchecked()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "NotFound500");

        var result = client.SendRequest(request);

        Assert.IsTrue(
            result.IsSuccess,
            $"Pinning current behaviour: an HTTP 500 is reported as a successful send. Got: {NegativeTestSupport.Describe(result)}");

        Assert.IsNotNull(result.Response, "A successful send must carry the response it received.");

        using var response = result.Response;

        Assert.AreEqual(
            HttpStatusCode.InternalServerError,
            response.StatusCode,
            "The service was asked for an error, so the pin only means something if the status really is 500.");

        var body = await response.Content.ReadAsStringAsync();
        var fault = SoapAssert.GetBodyChild(body);

        Assert.AreEqual("Fault", fault.Name.LocalName, $"The 500 must carry a SOAP fault. Body was: {body}");
        SoapAssert.AssertElementValue(fault, "Text", "The requested record was not found.");
    }

    [TestMethod]
    [Ignore("DEFECT-STATUS-IGNORED: unpins when fixed")]
    public void SendRequest_WhenTheServiceAnswersWith500_ShouldReportFailure()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "NotFound500");

        var result = client.SendRequest(request);

        Assert.IsFalse(result.IsSuccess, "A server error status must not be reported to the caller as a successful send.");
    }

    [TestMethod]
    public void SendRequest_WhenTheServiceOutlastsTheTimeout_FailsWithoutThrowing()
    {
        var client = SoapClientFactoryHelper.CreateDirectSoap12Client();

        var timeoutApplied = client.SetClientTimeout(ShortTimeout);

        Assert.IsTrue(timeoutApplied.IsSuccess, $"The timeout must be accepted. Got: {NegativeTestSupport.Describe(timeoutApplied)}");

        using var request = NegativeTestSupport.PostRequest(client, "SlowOp", ("delayMs", SlowOperationDelayMs));

        var stopwatch = Stopwatch.StartNew();

        var result = client.SendRequest(request);

        stopwatch.Stop();

        Assert.IsFalse(result.IsSuccess, $"A timed-out call must be a failure. Got: {NegativeTestSupport.Describe(result)}");

        Assert.AreEqual(
            SendFailureMessage,
            NegativeTestSupport.FirstMessageInfo(result),
            "A timeout must be reported as a send failure.");

        Assert.IsTrue(
            stopwatch.Elapsed < TimeoutUpperBound,
            $"The call was configured to give up after {ShortTimeout.TotalMilliseconds} ms against a " +
            $"{SlowOperationDelayMs} ms delay, but it took {stopwatch.Elapsed}. The failure was therefore " +
            "probably not the timeout.");
    }

    [TestMethod]
    public async Task SendRequestAsync_WithAnAlreadyCancelledToken_FailsWithoutThrowing()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();
        using var request = NegativeTestSupport.PostRequest(client, "HelloWorld");
        using var cancellation = new CancellationTokenSource();

        cancellation.Cancel();

        Assert.IsTrue(cancellation.Token.IsCancellationRequested, "The arrangement requires an already cancelled token.");

        var result = await client.SendRequestAsync(request, cancellation.Token);

        Assert.IsFalse(
            result.IsSuccess,
            $"A cancelled call must be a failure even though HelloWorld would otherwise succeed. Got: {NegativeTestSupport.Describe(result)}");

        Assert.AreEqual(
            SendAsyncFailureMessage,
            NegativeTestSupport.FirstMessageInfo(result),
            "Cancellation must be reported through the asynchronous send failure path.");
    }

    [TestMethod]
    public void GetXmlNodeResponseBody_WithMalformedXml_FailsGracefully()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        Assert.ThrowsException<XmlException>(
            () => NegativeTestSupport.ParseRoot(NegativeTestSupport.MalformedEnvelopeXml),
            "The malformed fixture must actually fail to parse.");

        var result = client.GetXmlNodeResponseBody(NegativeTestSupport.MalformedEnvelopeXml);

        Assert.IsFalse(result.IsSuccess, $"Malformed XML must not be reported as a usable body. Got: {NegativeTestSupport.Describe(result)}");
        Assert.IsNull(result.Response, "A failed extraction must not hand back a node.");

        Assert.AreEqual(
            InvalidSoapMessage,
            NegativeTestSupport.FirstMessageInfo(result),
            "The caller must be told the response was not a valid SOAP message.");

        var messages = NegativeTestSupport.Messages(result);

        Assert.IsTrue(
            messages.Any(message => message.MessageType.ToString() == "Exception"),
            $"The parser failure must be attached so the cause is diagnosable. Got: {NegativeTestSupport.Describe(result)}");
    }
}
