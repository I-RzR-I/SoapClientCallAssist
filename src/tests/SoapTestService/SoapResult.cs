using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SoapTestService;

public sealed class SoapResult : IResult
{
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

        var isSoap11 = httpContext.Request.Path.StartsWithSegments(SoapEndpoints.ServiceAsmxPath);
        var envelopeNamespace = isSoap11 ? SoapNames.Soap11 : SoapNames.Soap12;
        var mediaType = isSoap11 ? SoapNames.Soap11MediaType : SoapNames.Soap12MediaType;

        var prefix = SoapPrefixSwitch.Resolve(httpContext.Request.Query[SoapPrefixSwitch.QueryKey].ToString());

        var content = _fault is { } fault
            ? BuildFault(fault.Code, fault.Reason, prefix, envelopeNamespace, isSoap11)
            : _payload;

        var envelope = new XElement(
            envelopeNamespace + "Envelope",
            new XAttribute(XNamespace.Xmlns + prefix, envelopeNamespace.NamespaceName),
            new XElement(envelopeNamespace + "Body", content));

        var body = Serialize(envelope);
        var response = httpContext.Response;

        response.StatusCode = _statusCode;
        response.ContentType = $"{mediaType}; charset=utf-8";
        response.ContentLength = body.Length;
        response.Headers.XContentTypeOptions = NoSniff;

        await response.Body.WriteAsync(body, httpContext.RequestAborted);
    }

    private static XElement BuildFault(
        SoapFaultCode code,
        string reason,
        string prefix,
        XNamespace envelopeNamespace,
        bool isSoap11)
        => isSoap11
            ? new XElement(
                envelopeNamespace + "Fault",
                new XElement("faultcode", $"{prefix}:{Soap11FaultCode(code)}"),
                new XElement("faultstring", reason))
            : new XElement(
                envelopeNamespace + "Fault",
                new XElement(
                    envelopeNamespace + "Code",
                    new XElement(envelopeNamespace + "Value", $"{prefix}:{code}")),
                new XElement(
                    envelopeNamespace + "Reason",
                    new XElement(
                        envelopeNamespace + "Text",
                        new XAttribute(XNamespace.Xml + "lang", "en"),
                        reason)));

    private static byte[] Serialize(XElement envelope)
    {
        using var buffer = new MemoryStream(RenderBufferBytes);

        using (var writer = XmlWriter.Create(buffer, WriterSettings))
            envelope.WriteTo(writer);

        return buffer.ToArray();
    }

    private static string Soap11FaultCode(SoapFaultCode code)
        => code switch
        {
            SoapFaultCode.Sender => "Client",
            SoapFaultCode.Receiver => "Server",
            _ => code.ToString()
        };
}
