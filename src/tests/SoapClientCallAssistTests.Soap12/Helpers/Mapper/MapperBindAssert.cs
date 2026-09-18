
#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SoapClientCallAssistTests.Soap12.Helpers.Mapper;

internal static class MapperBindAssert
{
    internal const string BindCode = "ER-MAP-BND";

    internal const string ResponseCode = "ER-MAP-RSP";

    internal const string FaultCode = "ER-MAP-FLT";

    internal const string MetadataCode = "ER-MAP-MTD";

    internal const string NilNonNullableCode = "V-MAP-005";

    internal const string NoSingleBodyCode = "V-MAP-009";

    internal const string EmptyBodyCode = "V-MAP-010";

    internal const string CollectionShapeCode = "V-MAP-011";

    internal const string AnchorMismatchCode = "V-MAP-012";

    internal static void FailedWithCode(IResult result, string expectedCode)
    {
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsSuccess, expectedCode);

        var keys = Keys(result);
        Assert.IsTrue(keys.Count > 0, expectedCode);

        Assert.AreEqual(expectedCode, keys[0], $"{string.Join(", ", keys)} | {Text(result)}");
    }

    internal static void Succeeded(IResult result)
    {
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccess, $"{string.Join(", ", Keys(result))} | {Text(result)}");
    }

    internal static void MessagesDoNotContain(IResult result, params string[] forbidden)
    {
        var text = Text(result);
        foreach (var fragment in forbidden)
            Assert.IsFalse(text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0, $"{fragment} | {text}");
    }

    internal static void MessagesContain(IResult result, string expected)
    {
        var text = Text(result);
        Assert.IsTrue(text.IndexOf(expected, StringComparison.Ordinal) >= 0, $"{expected} | {text}");
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
