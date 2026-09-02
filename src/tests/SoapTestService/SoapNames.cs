using System.Xml.Linq;

namespace SoapTestService;

public static class SoapNames
{
    public static readonly XNamespace Soap11 = "http://schemas.xmlsoap.org/soap/envelope/";

    public static readonly XNamespace Soap12 = "http://www.w3.org/2003/05/soap-envelope";

    public static readonly XNamespace Service = "http://SoapClientCallAssist.local/";

    public const string Soap11MediaType = "text/xml";

    public const string Soap12MediaType = "application/soap+xml";
}
