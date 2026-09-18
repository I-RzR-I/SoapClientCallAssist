#nullable disable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Transport;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace SoapClientCallAssistTests.Soap12.Tests.Transport;

[TestClass]
public sealed class TransportConfigurationTests
{

    private const string TimeoutValidationCode = "V-BEC-TMO-001";

    private static readonly Uri UnroutableEndpoint = new("https://named-http-client.invalid/Service.svc");

    private static readonly TimeSpan HandlerDelay = TimeSpan.FromMilliseconds(700);

    private static readonly TimeSpan ShorterThanHandlerDelay = TimeSpan.FromMilliseconds(100);

    private const DecompressionMethods NegotiatedDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

    [TestMethod]
    public void RegisterSoapClientsEndpoint_AfterAConsumerPrimaryHandler_SendsThroughThatHandlerWithDecompressionEnabled_Test()
    {
        var primary = new RecordingPrimaryHttpClientHandler(NegativeTestSupport.UnindentedSoap12Envelope);

        var services = new ServiceCollection();

        services
            .AddHttpClient(SoapClientEndpointExtensions.SoapHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => primary);

        services.RegisterSoapClientsEndpoint();

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>()(SoapProtocolType.SOAP_1_2);

        using var request = BuildProbeRequest(client);

        var result = client.SendRequest(request);

        Assert.IsTrue(primary.Invoked, NegativeTestSupport.Describe(result));

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));

        Assert.AreEqual(NegotiatedDecompression, primary.AutomaticDecompression & NegotiatedDecompression, $"{primary.AutomaticDecompression}");
    }

    [TestMethod]
    public void RegisterSoapClientsEndpoint_WithoutAConsumerPrimaryHandler_EnablesDecompressionOnTheDefaultHandler_Test()
    {
        var services = new ServiceCollection();
        services.RegisterSoapClientsEndpoint();

        var provider = services.BuildServiceProvider();

        var primary = PrimaryHandlerBuiltFor(provider) as HttpClientHandler;

        Assert.IsNotNull(primary);

        Assert.AreEqual(NegotiatedDecompression, primary.AutomaticDecompression & NegotiatedDecompression, $"{primary.AutomaticDecompression}");
    }

    [TestMethod]
    public void SendRequest_GoesOutOnTheNamedHttpClientTheLibraryRegisters_Test()
    {
        var probe = new RecordingHttpMessageHandler(NegativeTestSupport.UnindentedSoap12Envelope);

        var services = new ServiceCollection();
        services.RegisterSoapClientsEndpoint();

        services
            .AddHttpClient(SoapClientEndpointExtensions.SoapHttpClientName)
            .AddHttpMessageHandler(() => probe);

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>()(SoapProtocolType.SOAP_1_2);

        var built = client.BuildRequest(HttpMethod.Post, UnroutableEndpoint, NegativeTestSupport.Bodies("HelloWorld"));

        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));

        using var request = built.Response;

        var result = client.SendRequest(request);

        Assert.IsTrue(probe.Invoked, $"{SoapClientEndpointExtensions.SoapHttpClientName} | {NegativeTestSupport.Describe(result)}");

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));
    }

    [DataTestMethod]
    [DataRow(0, DisplayName = "zero timeout")]
    [DataRow(-2, DisplayName = "negative timeout just past the infinite sentinel")]
    [DataRow(-5000, DisplayName = "negative timeout")]
    public void SetClientTimeout_WithAValueThatWouldBreakEveryLaterSend_IsRefused_Test(int milliseconds)
    {
        var client = SoapClientFactoryHelper.CreateDirectSoap12Client();

        var applied = client.SetClientTimeout(TimeSpan.FromMilliseconds(milliseconds));

        Assert.IsFalse(applied.IsSuccess, $"{milliseconds}");

        Assert.AreEqual(TimeoutValidationCode, NegativeTestSupport.Messages(applied)[0].Key, NegativeTestSupport.Describe(applied));
    }

    [TestMethod]
    public void SetClientTimeout_WithAValueLongerThanHttpClientAccepts_IsRefused_Test()
    {
        var client = SoapClientFactoryHelper.CreateDirectSoap12Client();

        var applied = client.SetClientTimeout(TimeSpan.FromDays(30));

        Assert.IsFalse(applied.IsSuccess);
    }

    [TestMethod]
    public void SetClientTimeout_WhenARefusedValueIsOffered_LeavesTheWorkingTimeoutInPlace_Test()
    {
        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var refused = client.SetClientTimeout(TimeSpan.Zero);

        Assert.IsFalse(refused.IsSuccess);

        using var request = NegativeTestSupport.PostRequest(client, "HelloWorld");

        var result = client.SendRequest(request);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));
    }

    [TestMethod]
    public void SetClientTimeout_WithAUsableValue_IsAccepted_Test()
    {
        var client = SoapClientFactoryHelper.CreateDirectSoap12Client();

        var applied = client.SetClientTimeout(TimeSpan.FromSeconds(30));

        Assert.IsTrue(applied.IsSuccess, NegativeTestSupport.Describe(applied));
    }

    [TestMethod]
    public void SetClientTimeout_WithTheInfiniteSentinel_IsAccepted_Test()
    {
        var client = SoapClientFactoryHelper.CreateDirectSoap12Client();

        var applied = client.SetClientTimeout(Timeout.InfiniteTimeSpan);

        Assert.IsTrue(applied.IsSuccess, NegativeTestSupport.Describe(applied));
    }

    [TestMethod]
    public void SetClientTimeout_WithTheInfiniteSentinel_IsAppliedToTheSendRatherThanTheConfiguredDefault_Test()
    {
        var probe = new RecordingHttpMessageHandler(NegativeTestSupport.UnindentedSoap12Envelope, HandlerDelay);
        var client = ClientSendingThrough(probe);

        Assert.IsTrue(client.SetClientTimeout(ShorterThanHandlerDelay).IsSuccess);

        using (var bounded = BuildProbeRequest(client))
        {
            Assert.IsFalse(client.SendRequest(bounded).IsSuccess);
        }

        Assert.IsTrue(client.SetClientTimeout(Timeout.InfiniteTimeSpan).IsSuccess);

        using var unbounded = BuildProbeRequest(client);

        var result = client.SendRequest(unbounded);

        Assert.IsTrue(result.IsSuccess, NegativeTestSupport.Describe(result));
    }

    private static ISoapClientEndpoint ClientSendingThrough(RecordingHttpMessageHandler probe)
        => SoapClientFactoryHelper.CreateSoap12ClientSendingThrough(probe);

    private static HttpRequestMessage BuildProbeRequest(ISoapClientEndpoint client)
    {
        var built = client.BuildRequest(HttpMethod.Post, UnroutableEndpoint, NegativeTestSupport.Bodies("HelloWorld"));

        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));

        return built.Response;
    }

    private static HttpMessageHandler PrimaryHandlerBuiltFor(IServiceProvider provider)
    {
        var options = provider
            .GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>()
            .Get(SoapClientEndpointExtensions.SoapHttpClientName);

        var builder = provider.GetRequiredService<HttpMessageHandlerBuilder>();
        builder.Name = SoapClientEndpointExtensions.SoapHttpClientName;

        foreach (var action in options.HttpMessageHandlerBuilderActions)
            action(builder);

        return builder.PrimaryHandler;
    }
}
