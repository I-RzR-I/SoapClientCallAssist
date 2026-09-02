using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Net.Http.Headers;

namespace SoapTestService;

public static class SoapEndpoints
{
    public const string ServicePath = "/ServiceSvc.svc";

    public const string ServiceAsmxPath = "/ServiceAsmx.asmx";

    public static void MapSoapService(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(
            ServicePath,
            (HttpContext context, RequestRecorder recorder)
                => HandlePostAsync(context, recorder, SoapNames.Soap12MediaType, SoapNames.Soap12));
        endpoints.MapGet(
            $"{ServicePath}/{{**rest}}",
            (HttpContext context, RequestRecorder recorder, string? rest)
                => HandleGetAsync(context, recorder, rest, SoapNames.Soap12MediaType));

        endpoints.MapPost(
            ServiceAsmxPath,
            (HttpContext context, RequestRecorder recorder)
                => HandlePostAsync(context, recorder, SoapNames.Soap11MediaType, SoapNames.Soap11));
        endpoints.MapGet(
            $"{ServiceAsmxPath}/{{**rest}}",
            (HttpContext context, RequestRecorder recorder, string? rest)
                => HandleGetAsync(context, recorder, rest, SoapNames.Soap11MediaType));
    }

    private static async Task<IResult> HandlePostAsync(
        HttpContext context,
        RequestRecorder recorder,
        string expectedMediaType,
        XNamespace envelopeNamespace)
    {
        var body = await RecordAsync(context, recorder);

        if (!IsSoapMediaType(context.Request.ContentType, expectedMediaType))
        {
            return SoapResult.Fault(
                SoapFaultCode.Sender,
                $"Content-Type must be {expectedMediaType}.",
                StatusCodes.Status415UnsupportedMediaType);
        }

        XElement? envelopeBody;

        try
        {
            envelopeBody = ReadEnvelopeBody(body, envelopeNamespace);
        }
        catch (XmlException)
        {
            return SoapResult.Fault(SoapFaultCode.Sender, "The request body is not well-formed XML.");
        }

        var operationElement = envelopeBody?.Elements().FirstOrDefault();

        if (operationElement is null)
            return SoapResult.Fault(SoapFaultCode.Sender, "The SOAP envelope carries no body element.");

        return await SoapOperations.InvokeAsync(
            operationElement.Name.LocalName,
            SoapArguments.FromElement(operationElement),
            context.RequestAborted);
    }

    private static async Task<IResult> HandleGetAsync(
        HttpContext context,
        RequestRecorder recorder,
        string? rest,
        string expectedMediaType)
    {
        await RecordAsync(context, recorder);

        if (!IsSoapMediaType(context.Request.ContentType, expectedMediaType))
        {
            return SoapResult.Fault(
                SoapFaultCode.Sender,
                $"Content-Type must be {expectedMediaType}.",
                StatusCodes.Status415UnsupportedMediaType);
        }

        var (operation, positional) = ParseRequestLine(rest);

        var named = context.Request.Query
            .Where(entry => !string.Equals(entry.Key, SoapPrefixSwitch.QueryKey, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value.FirstOrDefault() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

        return await SoapOperations.InvokeAsync(
            operation,
            SoapArguments.FromRequestLine(named, positional),
            context.RequestAborted);
    }

    private static (string Operation, IReadOnlyList<string> Positional) ParseRequestLine(string? rest)
    {
        var remainder = rest ?? string.Empty;

        if (remainder.StartsWith('{'))
        {
            var closing = remainder.LastIndexOf('}');

            if (closing >= 0)
                remainder = remainder[(closing + 1)..];
        }

        var separator = remainder.IndexOf('/', StringComparison.Ordinal);

        if (separator < 0)
            return (remainder, Array.Empty<string>());

        var positional = remainder[(separator + 1)..]
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        return (remainder[..separator], positional);
    }

    private static async Task<string> RecordAsync(HttpContext context, RequestRecorder recorder)
    {
        var request = context.Request;

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync(context.RequestAborted);

        recorder.Add(new RecordedRequest(
            request.Method,
            request.Path.Value ?? string.Empty,
            request.QueryString.Value ?? string.Empty,
            request.ContentType,

            request.Headers.ToDictionary(
                header => header.Key,
                header => (IReadOnlyList<string>)header.Value.Select(value => value ?? string.Empty).ToArray()),
            body));

        return body;
    }

    private static XElement? ReadEnvelopeBody(string body, XNamespace envelopeNamespace)
    {
        using var textReader = new StringReader(body);
        using var xmlReader = SoapXml.CreateReader(textReader);

        var root = XDocument.Load(xmlReader).Root;

        return root?.Name == envelopeNamespace + "Envelope"
            ? root.Element(envelopeNamespace + "Body")
            : null;
    }

    private static bool IsSoapMediaType(string? contentType, string expectedMediaType)
        => MediaTypeHeaderValue.TryParse(contentType, out var parsed)
           && string.Equals(parsed.MediaType.Value, expectedMediaType, StringComparison.OrdinalIgnoreCase);
}
