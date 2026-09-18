#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Security;
using SoapClientCallAssist.Security.WsSecurity;
using SoapClientCallAssistTests.Soap12.Helpers.Certificates;
using SoapClientCallAssistTests.Soap12.Helpers.Protocol;
using SoapClientCallAssistTests.Soap12.Helpers.Results;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using SoapClientCallAssistTests.Soap12.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Tests.Security.Signing;

[TestClass]
public sealed class WsSecurityConcurrentSigningTests
{

    private const int SigningCount = 50;

    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task BuildRequest_SigningFiftyEnvelopesConcurrentlyWithOneCertificate_ProducesFiftyDistinctVerifiableSignatures_Test()
    {
        using var certificate = CertificateScenarioSupport.CreateRsaCertificate("CN=Scca.Tests.Concurrency", 2048);

        var client = SoapClientFactoryHelper.CreateSoap12Client();

        var outcomes = await Task.WhenAll(
            Enumerable.Range(0, SigningCount).Select(index => Task.Run(() => Sign(client, certificate, index))));

        var failures = outcomes.Where(outcome => outcome.Wire is null).ToList();

        Assert.AreEqual(
            0,
            failures.Count, $"{SigningCount} | {string.Join(" ;; ", failures.Take(3).Select(outcome => $"#{outcome.Payload} {outcome.Failure}"))}");

        var strayed = outcomes
            .Where(outcome => Payloads(outcome).Any(payload => payload != outcome.Payload))
            .Select(outcome => outcome.Payload)
            .ToList();

        Assert.AreEqual(0, strayed.Count, string.Join(", ", strayed));

        var signatures = outcomes.Select(outcome => CertificateScenarioSupport.SignatureValue(outcome.Wire)).ToList();
        var distinct = signatures.Distinct(StringComparer.Ordinal).Count();

        Assert.AreEqual(SigningCount, distinct);

        var verified = outcomes.Count(outcome =>
            new WsSecurityMessageVerifier().Verify(outcome.Wire, certificate).IsSuccess);

        TestContext.WriteLine(
            $"concurrent signings={SigningCount} succeeded={outcomes.Length - failures.Count} "
            + $"distinct SignatureValues={distinct} verified={verified}/{SigningCount} contaminated={strayed.Count}");

        Assert.AreEqual(SigningCount, verified);
    }

    private static IEnumerable<string> Payloads(SigningOutcome outcome)
        => WsSecurityTestSupport.ParseWire(outcome.Wire)
            .GetElementsByTagName("id", SoapAssert.ServiceNs)
            .Cast<XmlElement>()
            .Select(element => element.InnerText);

    private static SigningOutcome Sign(ISoapClientEndpoint client, X509Certificate2 certificate, int index)
    {
        var payload = $"payload-{index:D2}";

        var built = client.BuildRequest(
            HttpMethod.Post,
            new BuildSoapRequestDto
            {
                Client = new HttpClientDto(WsSecurityTestSupport.Endpoint),
                Envelope = new SoapEnvelopeDto(
                    new[] { WsSecurityTestSupport.Body(payload) }, null, WsSecurityTestSupport.Action),
                Security = WsSecurityTestSupport.Security(security => security.SigningCertificate = certificate)
            });

        if (built.IsSuccess is false)
            return new SigningOutcome(payload, null, NegativeTestSupport.Describe(built));

        using var request = built.Response;

        return new SigningOutcome(payload, request.Content.ReadAsStringAsync().GetAwaiter().GetResult(), null);
    }
}
