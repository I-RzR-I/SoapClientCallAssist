
#nullable disable

using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Mapper;

internal static class MapperBindEnvelope
{
    internal const string Soap12Ns = "http://www.w3.org/2003/05/soap-envelope";

    internal const string Soap11Ns = "http://schemas.xmlsoap.org/soap/envelope/";

    internal const string XsiNs = "http://www.w3.org/2001/XMLSchema-instance";

    internal static XNamespace Protocol12 => XNamespace.Get(Soap12Ns);

    internal static XNamespace Protocol11 => XNamespace.Get(Soap11Ns);

    internal static string Wrap12(string payload, string prefix = "soap")
        => Wrap(payload, Soap12Ns, prefix);

    internal static string Wrap11(string payload, string prefix = "soap")
        => Wrap(payload, Soap11Ns, prefix);

    internal static string Wrap(string payload, string envelopeNamespace, string prefix)
        => prefix is null
            ? $"<Envelope xmlns=\"{envelopeNamespace}\"><Body>{payload}</Body></Envelope>"
            : $"<{prefix}:Envelope xmlns:{prefix}=\"{envelopeNamespace}\">"
              + $"<{prefix}:Body>{payload}</{prefix}:Body>"
              + $"</{prefix}:Envelope>";

    internal static string Payload(string localName, string inner)
        => $"<{localName} xmlns=\"{MapperBindNs.Contract}\">{inner}</{localName}>";

    internal static string NestedChain(int depth, string leaf)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < depth; index++)
            builder.Append("<n>");

        builder.Append(leaf);

        for (var index = 0; index < depth; index++)
            builder.Append("</n>");

        return builder.ToString();
    }

    internal static string BillionLaughs(int levels, int fanOut)
    {
        var declarations = new StringBuilder();
        declarations.Append("<!ENTITY lol0 \"lol\">");

        for (var level = 1; level <= levels; level++)
        {
            declarations.Append($"<!ENTITY lol{level} \"");
            for (var repeat = 0; repeat < fanOut; repeat++)
                declarations.Append($"&lol{level - 1};");

            declarations.Append("\">");
        }

        return "<?xml version=\"1.0\"?>"
               + $"<!DOCTYPE Envelope [{declarations}]>"
               + $"<soap:Envelope xmlns:soap=\"{Soap12Ns}\"><soap:Body>"
               + $"<Flat xmlns=\"{MapperBindNs.Contract}\"><Name>&lol{levels};</Name></Flat>"
               + "</soap:Body></soap:Envelope>";
    }

    internal static bool CarriesFaultElement(string response, string envelopeNamespace)
        => XDocument
            .Parse(response)
            .Descendants(XName.Get("Fault", envelopeNamespace))
            .Any();

    internal static int CountBodyChildren(string response, string envelopeNamespace)
        => XDocument
            .Parse(response)
            .Descendants(XName.Get("Body", envelopeNamespace))
            .SelectMany(x => x.Elements())
            .Count();
}
