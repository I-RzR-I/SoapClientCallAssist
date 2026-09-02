
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers;

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

internal static class MapperBindAssert
{
    internal const string BindCode = "ER-MAP-BND";

    internal const string ResponseCode = "ER-MAP-RSP";

    internal const string FaultCode = "ER-MAP-FLT";

    internal const string MetadataCode = "ER-MAP-MTD";

    internal const string NilNonNullableCode = "V-MAP-005";

    internal const string NoSingleBodyCode = "V-MAP-009";

    internal const string EmptyBodyCode = "V-MAP-010";

    internal static void FailedWithCode(IResult result, string expectedCode)
    {
        Assert.IsNotNull(result, "The mapper returned no result at all.");
        Assert.IsFalse(
            result.IsSuccess,
            $"Expected a failure carrying '{expectedCode}', but the call succeeded.");

        var keys = Keys(result);
        Assert.IsTrue(
            keys.Count > 0,
            $"Expected a failure carrying '{expectedCode}', but the result holds no message.");

        Assert.AreEqual(
            expectedCode,
            keys[0],
            $"Expected '{expectedCode}' as the first message code. Codes: [{string.Join(", ", keys)}]. "
            + $"Message text: {Text(result)}");
    }

    internal static void Succeeded(IResult result)
    {
        Assert.IsNotNull(result, "The mapper returned no result at all.");
        Assert.IsTrue(
            result.IsSuccess,
            $"Expected success. Codes: [{string.Join(", ", Keys(result))}]. Message text: {Text(result)}");
    }

    internal static void MessagesDoNotContain(IResult result, params string[] forbidden)
    {
        var text = Text(result);
        foreach (var fragment in forbidden)
            Assert.IsFalse(
                text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0,
                $"The failure echoed '{fragment}'. Full message text: {text}");
    }

    internal static void MessagesContain(IResult result, string expected)
    {
        var text = Text(result);
        Assert.IsTrue(
            text.IndexOf(expected, StringComparison.Ordinal) >= 0,
            $"Expected the failure to mention '{expected}'. Full message text: {text}");
    }

    internal static IReadOnlyList<string> Keys(IResult result)
        => result?.Messages == null
            ? new List<string>()
            : result.Messages.Select(x => x?.Key ?? "(null key)").ToList();

    internal static string Text(IResult result)
    {
        if (result?.Messages == null)
            return "(no messages)";

        var sink = new StringBuilder();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);

        foreach (var message in result.Messages)
            Collect(message, sink, seen, 0);

        return sink.ToString();
    }

    private static void Collect(object node, StringBuilder sink, HashSet<object> seen, int depth)
    {
        if (node is null || depth > 4)
            return;

        if (node is string text)
        {
            sink.Append(text).Append('\n');

            return;
        }

        if (node is IEnumerable sequence)
        {
            foreach (var item in sequence)
                Collect(item, sink, seen, depth + 1);

            return;
        }

        if (!seen.Add(node))
            return;

        sink.Append(node.ToString()).Append('\n');

        foreach (var property in node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetGetMethod(false) is null)
                continue;

            object value;
            try
            {
                value = property.GetValue(node);
            }
            catch
            {
                continue;
            }

            Collect(value, sink, seen, depth + 1);
        }
    }
}
