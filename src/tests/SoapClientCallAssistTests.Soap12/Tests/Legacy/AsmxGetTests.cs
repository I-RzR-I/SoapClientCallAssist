using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests.Legacy;

[TestClass]
public sealed class AsmxGetTests
{

    private const SoapProtocolType Protocol = SoapProtocolType.SOAP_1_1;

    [TestMethod]
    public void AsmxGet_IsValid_NameInBodies_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildGet(
            Protocol,
            client,
            new[] { LegacyBodyBuilders.IsValidNoNamespaceRoot() },
            correlationId,
            buildGetRequestAsSlashUrl: false);

        using var response = Soap12FunctionalSupport.Unwrap(
            client.SendRequest(request),
            $"SendRequest {Protocol}");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(Protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

        var payload = CrossProtocolSupport.GetBodyChild(Protocol, envelope);
        SoapAssert.AssertElementValue(payload, "IsValidResult", "1");
    }

    [TestMethod]
    public void AsmxGet_IsValid_NameInBodiesV2_UsingRequestDtoOverload_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);

        var built = client.BuildRequest(
            HttpMethod.Get,
            new BuildSoapRequestDto(
                new HttpClientDto(CrossProtocolSupport.EndpointFor(Protocol)),
                new SoapEnvelopeDto(new[] { LegacyBodyBuilders.IsValidNoNamespaceRoot() })));

        Assert.IsTrue(built.IsSuccess, NegativeTestSupport.Describe(built));
        Assert.IsNotNull(built.Response);

        using var request = built.Response;
        using var response = Soap12FunctionalSupport.Unwrap(
            client.SendRequest(request),
            $"SendRequest {Protocol}");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

        var payload = CrossProtocolSupport.GetBodyChild(Protocol, envelope);
        SoapAssert.AssertElementValue(payload, "IsValidResult", "1");
    }

    [TestMethod]
    public async Task AsmxGet_IsValid_NameInBodies_Async_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildGet(
            Protocol,
            client,
            new[] { LegacyBodyBuilders.IsValidNoNamespaceRoot() },
            correlationId,
            buildGetRequestAsSlashUrl: false);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync {Protocol}");
        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(Protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

        var payload = CrossProtocolSupport.GetBodyChild(Protocol, envelope);
        SoapAssert.AssertElementValue(payload, "IsValidResult", "1");
    }

    [TestMethod]
    public void AsmxGet_IsValid_NameInBodiesAndIdWithNs_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildGet(
            Protocol,
            client,
            new[] { LegacyBodyBuilders.IsValidQualifiedChildrenNoNamespaceRoot() },
            correlationId,
            buildGetRequestAsSlashUrl: false);

        using var response = Soap12FunctionalSupport.Unwrap(
            client.SendRequest(request),
            $"SendRequest {Protocol}");
        var envelope = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(Protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

        var payload = CrossProtocolSupport.GetBodyChild(Protocol, envelope);
        SoapAssert.AssertElementValue(payload, "IsValidResult", "1");
    }

    [TestMethod]
    public async Task AsmxGet_IsValid_NameInBodiesAndIdWithNs_Async_ReturnsOne_Test()
    {
        var client = CrossProtocolSupport.CreateClient(Protocol);
        var correlationId = Soap12FunctionalSupport.NewCorrelationId();

        using var request = CrossProtocolSupport.BuildGet(
            Protocol,
            client,
            new[] { LegacyBodyBuilders.IsValidQualifiedChildrenNoNamespaceRoot() },
            correlationId,
            buildGetRequestAsSlashUrl: false);

        using var response = Soap12FunctionalSupport.Unwrap(
            await client.SendRequestAsync(request),
            $"SendRequestAsync {Protocol}");
        var envelope = await response.Content.ReadAsStringAsync();

        CrossProtocolSupport.AssertRequestWasOnTheWireForProtocol(Protocol, correlationId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{envelope}");

        var payload = CrossProtocolSupport.GetBodyChild(Protocol, envelope);
        SoapAssert.AssertElementValue(payload, "IsValidResult", "1");
    }
}
