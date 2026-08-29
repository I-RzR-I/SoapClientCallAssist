using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using RzR.ResultMessage.Abstractions.Models;
using SoapClientCallAssist.Abstractions;
using SoapTestService;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers;

internal static class NegativeTestSupport
{

    internal const string UnindentedSoap12Envelope =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\">" +
        "<soap:Body>" +
        "<HelloWorldResponse xmlns=\"http://SoapClientCallAssist.local/\">" +
        "<HelloWorldResult>Hello World</HelloWorldResult>" +
        "</HelloWorldResponse>" +
        "</soap:Body>" +
        "</soap:Envelope>";

    internal const string IndentedSoap12Envelope =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
        "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\">\n" +
        "  <soap:Body>\n" +
        "    <HelloWorldResponse xmlns=\"http://SoapClientCallAssist.local/\">\n" +
        "      <HelloWorldResult>Hello World</HelloWorldResult>\n" +
        "    </HelloWorldResponse>\n" +
        "  </soap:Body>\n" +
        "</soap:Envelope>";

    internal const string EnvPrefixedSoap12Envelope =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<env:Envelope xmlns:env=\"http://www.w3.org/2003/05/soap-envelope\">" +
        "<env:Body>" +
        "<HelloWorldResponse xmlns=\"http://SoapClientCallAssist.local/\">" +
        "<HelloWorldResult>Hello World</HelloWorldResult>" +
        "</HelloWorldResponse>" +
        "</env:Body>" +
        "</env:Envelope>";

    internal const string SPrefixedSoap12Envelope =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
        "<s:Body>" +
        "<HelloWorldResponse xmlns=\"http://SoapClientCallAssist.local/\">" +
        "<HelloWorldResult>Hello World</HelloWorldResult>" +
        "</HelloWorldResponse>" +
        "</s:Body>" +
        "</s:Envelope>";

    internal const string Soap12FaultWithoutAnyText =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\">" +
        "<soap:Body>" +
        "<soap:Fault>" +
        "<soap:Code><soap:Value /></soap:Code>" +
        "<soap:Reason><soap:Text xml:lang=\"en\" /></soap:Reason>" +
        "</soap:Fault>" +
        "</soap:Body>" +
        "</soap:Envelope>";

    internal const string Soap11FaultEnvelope =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
        "<soap:Body>" +
        "<soap:Fault>" +
        "<faultcode>soap:Client</faultcode>" +
        "<faultstring>The operation failed on purpose.</faultstring>" +
        "</soap:Fault>" +
        "</soap:Body>" +
        "</soap:Envelope>";

    internal const string MalformedEnvelopeXml =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\">" +
        "<soap:Body>" +
        "<HelloWorldResponse>" +
        "</soap:Body>" +
        "</soap:Envelope>";

    internal static XElement[] Bodies(string operation, params (string Name, string Value)[] arguments)
    {
        var element = new XElement(operation);

        foreach (var argument in arguments)
            element.Add(new XElement(argument.Name, argument.Value));

        return new[] { element };
    }

    internal static HttpRequestMessage PostRequest(
        ISoapClientEndpoint client,
        string operation,
        params (string Name, string Value)[] arguments)
    {
        var built = client.BuildRequest(HttpMethod.Post, SoapServiceFixture.ServiceUri, Bodies(operation, arguments));

        Assert.IsTrue(
            built.IsSuccess,
            $"Arranging this test requires a built request for '{operation}', but the build failed: {Describe(built)}");

        Assert.IsNotNull(built.Response, $"The build of '{operation}' reported success but produced no request.");

        return built.Response;
    }

    internal static IReadOnlyList<IMessageModel> Messages(IResult result)
    {
        Assert.IsNotNull(result, "The library returned a null result object.");
        Assert.IsNotNull(result.Messages, "The result carries no message collection, so a caller has nothing to act on.");

        return new List<IMessageModel>(result.Messages);
    }

    internal static string FirstMessageInfo(IResult result)
    {
        var messages = Messages(result);

        Assert.IsTrue(messages.Count > 0, "The result reported a failure but carried no messages.");
        Assert.IsNotNull(messages[0].Message, "The first message carries no message data.");
        Assert.IsNotNull(messages[0].Message.Info, "The first message carries no information text.");

        return messages[0].Message.Info;
    }

    internal static string Describe(IResult result)
    {
        if (result?.Messages is null)
            return "<no messages>";

        var rendered = result.Messages
            .Select(message => $"[{message.Key ?? "<null key>"}] {message.Message?.Info ?? "<null info>"}")
            .ToArray();

        return rendered.Length == 0 ? "<no messages>" : string.Join(" | ", rendered);
    }

    internal static XElement ParseRoot(string xml)
    {
        using var reader = SoapXml.CreateReader(new StringReader(xml));

        var document = XDocument.Load(reader);

        Assert.IsNotNull(document.Root, "The document has no root element.");

        return document.Root!;
    }

    internal static XElement? FindFault(string xml, XNamespace envelopeNamespace)
        => ParseRoot(xml).Descendants(envelopeNamespace + "Fault").FirstOrDefault();
}
