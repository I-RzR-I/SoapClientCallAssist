using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using System.Net;
using System.Threading.Tasks;

namespace SoapClientCallAssistTests.Soap12.Tests.Legacy;

[TestClass]
public sealed class SvcGetTests
{

    private const SoapProtocolType Protocol = SoapProtocolType.SOAP_1_2;

    [TestMethod]
    public async Task SvcGet_IsValid_NameInBodies_ReturnsOne_Test()
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
}
