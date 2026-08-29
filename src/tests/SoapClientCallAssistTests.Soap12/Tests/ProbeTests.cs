using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using RzR.ResultMessage.Abstractions.Models;
using SoapClientCallAssist;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssist.Dto.Public;
using SoapClientCallAssist.Enums;
using SoapTestService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Tests;

[TestClass]
public sealed class ProbeTests
{
    private const string ProbeNamespace = "http://SoapClientCallAssist.local/";

    private static readonly XNamespace Ns = ProbeNamespace;

    private static readonly Uri ValidUri = new("http://127.0.0.1:9999/ServiceSvc.svc");

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public void ProbeA_GetUriForms()
    {
        var client = ResolveSoap12Client();

        RecordGetUri(client, "A1", EchoBody(XNamespace.None), buildGetRequestAsSlashUrl: false);
        RecordGetUri(client, "A2", EchoBody(XNamespace.None), buildGetRequestAsSlashUrl: true);
        RecordGetUri(client, "A3", EchoBody(Ns), buildGetRequestAsSlashUrl: false);
        RecordGetUri(client, "A4", EchoBody(Ns), buildGetRequestAsSlashUrl: true);
    }

    [TestMethod]
    public void ProbeB_FailureResultShape()
    {
        var client = ResolveSoap12Client();

        Log("PROBE B1 - BuildRequest(PUT, validUri, bodies) - expected V_BEC_VR_002");
        LogResultShape("B1", client.BuildRequest(HttpMethod.Put, ValidUri, EchoBody(XNamespace.None)));

        Log("PROBE B2 - BuildRequest(POST, null uri, bodies) - expected V_BEC_VR_003");
        LogResultShape("B2", client.BuildRequest(HttpMethod.Post, null, EchoBody(XNamespace.None)));

        Log("PROBE B3 - BuildRequest(POST, (BuildSoapRequestDto)null) - expected ER-S12-BSR");
        LogResultShape("B3", client.BuildRequest(HttpMethod.Post, null!));

        Log("PROBE B4 - BuildRequest(GET, validUri, empty bodies)");
        LogResultShape("B4", client.BuildRequest(HttpMethod.Get, ValidUri, Array.Empty<XElement>()));
    }

    [TestMethod]
    public async Task ProbeC_NestedBodySurvival()
    {
        var client = ResolveSoap12Client();

        var partiallyNamespaced = new[]
        {
            new XElement(Ns + "AddRecordWithDetail",
                new XElement(Ns + "product",
                    new XElement("Id", "1"),
                    new XElement("Detail",
                        new XElement("PartnerId", "9"))))
        };

        await RecordEnvelope(client, "C1", partiallyNamespaced);

        var fullyNamespaced = new[]
        {
            new XElement(Ns + "AddRecordWithDetail",
                new XElement(Ns + "product",
                    new XElement(Ns + "Id", "1"),
                    new XElement(Ns + "Detail",
                        new XElement(Ns + "PartnerId", "9"))))
        };

        await RecordEnvelope(client, "C2", fullyNamespaced);
    }

    [TestMethod]
    public async Task ProbeD_Soap12Markers()
    {
        var client = ResolveSoap12Client();
        const string action = ProbeNamespace + "EchoValue";

        var request = Unwrap("D", client.BuildRequest(HttpMethod.Post, ValidUri, EchoBody(XNamespace.None), action: action));
        if (request is null)
            return;

        var contentType = request.Content?.Headers.ContentType;

        Log($"D.ContentType.ToString() = [{contentType}]");
        Log($"D.ContentType.MediaType = [{Text(contentType?.MediaType)}]");
        Log($"D.ContentType.CharSet = [{Text(contentType?.CharSet)}]");
        Log($"D.ContentType.Parameters.Count = {Count(contentType?.Parameters.Count)}");

        if (contentType is not null)
            foreach (var parameter in contentType.Parameters)
                Log($"D.ContentType.Parameter [{parameter.Name}] = [{parameter.Value}]");

        Log($"D.RequestHeaders.Contains(SOAPAction) = {request.Headers.Contains("SOAPAction")}");
        Log($"D.ContentHeaders.Contains(SOAPAction) = {request.Content?.Headers.Contains("SOAPAction")}");
        Log($"D.RequestHeaders.Contains(Action) = {request.Headers.Contains("Action")}");
        Log($"D.ContentHeaders.Contains(Action) = {request.Content?.Headers.Contains("Action")}");

        foreach (var header in request.Headers)
            Log($"D.RequestHeader [{header.Key}] = [{string.Join("|", header.Value)}]");

        if (request.Content is not null)
            foreach (var header in request.Content.Headers)
                Log($"D.ContentHeader [{header.Key}] = [{string.Join("|", header.Value)}]");

        var envelope = await ReadContent(request);
        Log($"D.Envelope = {envelope}");

        using var reader = SoapXml.CreateReader(new StringReader(envelope));
        var document = XDocument.Load(reader);

        Log($"D.EnvelopeRoot.LocalName = [{Text(document.Root?.Name.LocalName)}]");
        Log($"D.EnvelopeRoot.NamespaceName = [{Text(document.Root?.Name.NamespaceName)}]");
    }

    private static ISoapClientEndpoint ResolveSoap12Client()
    {
        var services = new ServiceCollection();
        services.RegisterSoapClientsEndpoint();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<Func<SoapProtocolType, ISoapClientEndpoint>>();

        return factory(SoapProtocolType.SOAP_1_2);
    }

    private static XElement[] EchoBody(XNamespace ns)
        => new[]
        {
            new XElement(ns + "EchoValue",
                new XElement("value", "abc"),
                new XElement("count", "7"))
        };

    private static async Task<string> ReadContent(HttpRequestMessage request)
        => request.Content is null
            ? "<no content>"
            : await request.Content.ReadAsStringAsync();

    private static string Text(string? value) => value ?? "<null>";

    private static string Count(int? count) => count?.ToString() ?? "<null collection>";

    private void Log(string line) => TestContext.WriteLine(line);

    private HttpRequestMessage? Unwrap(string label, IResult<HttpRequestMessage> result)
    {
        Log($"{label}.IsSuccess = {result.IsSuccess}");

        if (result.IsSuccess && result.Response is not null)
            return result.Response;

        LogResultShape(label, result);

        return null;
    }

    private void LogResultShape(string label, IResult result)
    {
        Log($"{label}.IsSuccess = {result.IsSuccess}");
        Log($"{label}.IsFailure = {result.IsFailure}");
        Log($"{label}.Messages.Count = {Count(result.Messages?.Count)}");

        var messages = result.Messages is null
            ? new List<IMessageModel>()
            : new List<IMessageModel>(result.Messages);

        for (var index = 0; index < messages.Count; index++)
        {
            var message = messages[index];

            Log($"{label}.Messages[{index}].Key = [{Text(message.Key)}]");
            Log($"{label}.Messages[{index}].MessageType = {message.MessageType}");
            Log($"{label}.Messages[{index}].Message.Info = [{Text(message.Message?.Info)}]");

            var details = message.Message?.Details;
            Log($"{label}.Messages[{index}].Message.Details.Count = {Count(details?.Count)}");

            for (var detailIndex = 0; detailIndex < (details?.Count ?? 0); detailIndex++)
                Log($"{label}.Messages[{index}].Message.Details[{detailIndex}] = [{details![detailIndex]}]");
        }

        Log($"{label}.GetFirstMessage() = [{Text(result.GetFirstMessage())}]");

        var firstWithDetails = result.GetFirstMessageWithDetails();
        Log($"{label}.GetFirstMessageWithDetails().Info = [{Text(firstWithDetails?.Info)}]");
        Log($"{label}.GetFirstMessageWithDetails().Details.Count = {Count(firstWithDetails?.Details?.Count)}");
    }

    private void RecordGetUri(ISoapClientEndpoint client, string label, XElement[] bodies, bool buildGetRequestAsSlashUrl)
    {
        var request = Unwrap(label, client.BuildRequest(
            HttpMethod.Get,
            ValidUri,
            bodies,
            buildGetRequestAsSlashUrl: buildGetRequestAsSlashUrl));

        if (request is null)
            return;

        Log($"{label}.RequestUri.ToString() = {request.RequestUri}");
        Log($"{label}.RequestUri.AbsoluteUri = {request.RequestUri?.AbsoluteUri}");
    }

    private async Task RecordEnvelope(ISoapClientEndpoint client, string label, XElement[] bodies)
    {
        var request = Unwrap(label, client.BuildRequest(HttpMethod.Post, ValidUri, bodies));

        if (request is null)
            return;

        Log($"{label}.Envelope = {await ReadContent(request)}");
    }
}
