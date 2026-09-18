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

namespace SoapClientCallAssistTests.Soap12.Helpers.Results;

internal static class NegativeTestSupport
{

    internal const string HttpRedirectCode = "ER-BEC-HTTP-3XX";

    internal const string HttpUnauthorizedCode = "ER-BEC-HTTP-401";

    internal const string HttpForbiddenCode = "ER-BEC-HTTP-403";

    internal const string HttpClientErrorCode = "ER-BEC-HTTP-4XX";

    internal const string HttpSoapFaultCode = "ER-BEC-HTTP-FLT";

    internal const string HttpServerErrorCode = "ER-BEC-HTTP-5XX";

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

    internal const string Soap12HeaderFaultEnvelope =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<soap:Envelope xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\">" +
        "<soap:Header>" +
        "<soap:Fault>" +
        "<soap:Code><soap:Value>soap:Server</soap:Value></soap:Code>" +
        "<soap:Reason><soap:Text xml:lang=\"en\">Account suspended</soap:Text></soap:Reason>" +
        "</soap:Fault>" +
        "</soap:Header>" +
        "<soap:Body>" +
        "<HelloWorldResponse xmlns=\"http://SoapClientCallAssist.local/\">" +
        "<HelloWorldResult>Hello World</HelloWorldResult>" +
        "</HelloWorldResponse>" +
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

        Assert.IsTrue(built.IsSuccess, $"{operation} | {Describe(built)}");

        Assert.IsNotNull(built.Response, operation);

        return built.Response;
    }

    internal static IReadOnlyList<IMessageModel> Messages(IResult result)
    {
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Messages);

        return new List<IMessageModel>(result.Messages);
    }

    internal static string FirstMessageInfo(IResult result)
    {
        var messages = Messages(result);

        Assert.IsTrue(messages.Count > 0);
        Assert.IsNotNull(messages[0].Message);
        Assert.IsNotNull(messages[0].Message.Info);

        return messages[0].Message.Info;
    }

    internal static string Describe(IResult result) => MessageLeakSweep.Render(result);

    internal static XElement ParseRoot(string xml)
    {
        using var reader = SoapXml.CreateReader(new StringReader(xml));

        var document = XDocument.Load(reader);

        Assert.IsNotNull(document.Root);

        return document.Root!;
    }

    internal static XElement? FindFault(string xml, XNamespace envelopeNamespace)
        => ParseRoot(xml).Descendants(envelopeNamespace + "Fault").FirstOrDefault();
}
