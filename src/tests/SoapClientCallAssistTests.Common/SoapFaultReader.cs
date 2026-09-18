using System;
using System.Linq;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Common;

public static class SoapFaultReader
{
    private static readonly XNamespace Soap11 = "http://schemas.xmlsoap.org/soap/envelope/";

    private static readonly XNamespace Soap12 = "http://www.w3.org/2003/05/soap-envelope";

    public static SoapFaultInfo Read(string envelopeText)
    {
        var envelope = XDocument.Parse(envelopeText).Root
            ?? throw new InvalidOperationException("The response carried no XML root element.");

        if (envelope.Name.Namespace == Soap11)
            return ReadSoap11(RequireFault(envelope, Soap11));

        if (envelope.Name.Namespace == Soap12)
            return ReadSoap12(RequireFault(envelope, Soap12));

        throw new InvalidOperationException($"The response root element {envelope.Name} is not a SOAP envelope.");
    }

    private static XElement RequireFault(XElement envelope, XNamespace soap)
        => envelope.Element(soap + "Body")?.Element(soap + "Fault")
           ?? throw new InvalidOperationException("The response Body carries no Fault element.");

    private static SoapFaultInfo ReadSoap11(XElement fault)
    {
        var code = fault.Element("faultcode")
            ?? throw new InvalidOperationException("The SOAP 1.1 Fault carries no faultcode.");

        return Resolve(code, fault.Element("faultstring")?.Value ?? string.Empty);
    }

    private static SoapFaultInfo ReadSoap12(XElement fault)
    {
        var code = fault.Element(Soap12 + "Code")
            ?? throw new InvalidOperationException("The SOAP 1.2 Fault carries no Code.");

        var value = code.Element(Soap12 + "Value")
            ?? throw new InvalidOperationException("The SOAP 1.2 Fault Code carries no Value.");

        for (var subcode = code.Element(Soap12 + "Subcode"); subcode != null; subcode = subcode.Element(Soap12 + "Subcode"))
            value = subcode.Element(Soap12 + "Value") ?? value;

        var reason = fault.Element(Soap12 + "Reason")?.Elements(Soap12 + "Text").FirstOrDefault()?.Value ?? string.Empty;

        return Resolve(value, reason);
    }

    private static SoapFaultInfo Resolve(XElement qualifiedValue, string reason)
    {
        var text = qualifiedValue.Value.Trim();
        var separator = text.IndexOf(':');

        if (separator < 0)
            return new SoapFaultInfo(string.Empty, text, reason);

        var prefix = text.Substring(0, separator);
        var localName = text.Substring(separator + 1);
        var ns = qualifiedValue.GetNamespaceOfPrefix(prefix)
            ?? throw new InvalidOperationException($"The fault code prefix '{prefix}' is not bound to a namespace.");

        return new SoapFaultInfo(ns.NamespaceName, localName, reason);
    }
}