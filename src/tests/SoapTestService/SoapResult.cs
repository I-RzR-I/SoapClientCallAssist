using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SoapTestService;

public sealed class SoapResult : IResult
{
    private const string SoapContentType = "application/soap+xml; charset=utf-8";
    private const string NoSniff = "nosniff";

    private const int RenderBufferBytes = 1024;

    private static readonly XmlWriterSettings WriterSettings = new()
    {
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        Indent = false,

        OmitXmlDeclaration = false
    };

    private readonly XElement? _payload;
    private readonly (SoapFaultCode Code, string Reason)? _fault;
    private readonly int _statusCode;

    private SoapResult(XElement? payload, (SoapFaultCode Code, string Reason)? fault, int statusCode)
    {
        _payload = payload;
        _fault = fault;
        _statusCode = statusCode;
    }

    public static SoapResult Payload(XElement payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return new SoapResult(payload, null, StatusCodes.Status200OK);
    }

    public static SoapResult Fault(
        SoapFaultCode code,
        string reason,
        int statusCode = StatusCodes.Status500InternalServerError)
    {
        ArgumentNullException.ThrowIfNull(reason);

        return new SoapResult(null, (code, reason), statusCode);
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var prefix = SoapPrefixSwitch.Resolve(httpContext.Request.Query[SoapPrefixSwitch.QueryKey].ToString());

        var content = _fault is { } fault ? BuildFault(fault.Code, fault.Reason, prefix) : _payload;

        var envelope = new XElement(
            SoapNames.Soap12 + "Envelope",
            new XAttribute(XNamespace.Xmlns + prefix, SoapNames.Soap12.NamespaceName),
            new XElement(SoapNames.Soap12 + "Body", content));

        var body = Serialize(envelope);
        var response = httpContext.Response;

        response.StatusCode = _statusCode;
        response.ContentType = SoapContentType;
        response.ContentLength = body.Length;
        response.Headers.XContentTypeOptions = NoSniff;

        await response.Body.WriteAsync(body, httpContext.RequestAborted);
    }

    private static XElement BuildFault(SoapFaultCode code, string reason, string prefix)
        => new(
            SoapNames.Soap12 + "Fault",
            new XElement(
                SoapNames.Soap12 + "Code",
                new XElement(SoapNames.Soap12 + "Value", $"{prefix}:{code}")),
            new XElement(
                SoapNames.Soap12 + "Reason",
                new XElement(
                    SoapNames.Soap12 + "Text",
                    new XAttribute(XNamespace.Xml + "lang", "en"),
                    reason)));

    private static byte[] Serialize(XElement envelope)
    {
        using var buffer = new MemoryStream(RenderBufferBytes);

        using (var writer = XmlWriter.Create(buffer, WriterSettings))
            envelope.WriteTo(writer);

        return buffer.ToArray();
    }
}
