#nullable disable

using RzR.ResultMessage.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Results;

internal static class MessageLeakSweep
{

    internal const string NoMessages = "<no messages>";

    internal const string SecureStringMarker = "<SecureString instance: a secret object handed to the caller>";

    internal const string LazyValueSkipped = "<Lazy<T>.Value not forced>";

    internal const string TaskResultSkipped = "<Task<T>.Result not read>";

    private const int MaxDepth = 8;

    private const string NullText = "<null>";

    internal static IReadOnlyList<string> Values(IResult result)
    {
        var sink = new List<string>();

        if (result?.Messages is null)
            return sink;

        var index = 0;

        foreach (var message in result.Messages)
            Walk($"[{index++}]", message, sink, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));

        return sink;
    }

    internal static string Render(IResult result)
    {
        var values = Values(result);

        return values.Count == 0 ? NoMessages : string.Join(" ", values);
    }

    private static void Walk(string path, object value, List<string> sink, int depth, HashSet<object> seen)
    {
        if (value is null)
        {
            sink.Add($"{path}={NullText}");

            return;
        }

        if (value is string text)
        {
            sink.Add($"{path}='{text}'");

            return;
        }

        var type = value.GetType();

        if (type.IsPrimitive || type.IsEnum || value is decimal or DateTime or DateTimeOffset or TimeSpan or Guid)
        {
            sink.Add($"{path}={Convert.ToString(value, CultureInfo.InvariantCulture)}");

            return;
        }

        if (TryRenderLeaf(path, value, out var leaf))
        {
            sink.Add(leaf);

            return;
        }

        if (value is Exception exception)
            sink.Add($"{path}.ToString()='{exception}'");

        if (value is XNode xmlLinqNode)
            sink.Add($"{path}.ToString()='{xmlLinqNode.ToString(SaveOptions.DisableFormatting)}'");

        if (depth >= MaxDepth)
        {
            sink.Add($"{path}=<depth limit reached, {type.Name} not swept>");

            return;
        }

        if (value is IEnumerable items)
        {
            var index = 0;

            foreach (var item in items)
                Walk($"{path}[{index++}]", item, sink, depth + 1, seen);

            if (index == 0)
                sink.Add($"{path}=<empty>");

            return;
        }

        if (!seen.Add(value))
        {
            sink.Add($"{path}=<already swept>");

            return;
        }

        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var swept = 0;

        foreach (var property in properties)
        {
            if (property.GetMethod is null || property.GetIndexParameters().Length > 0)
                continue;

            swept++;

            if (TrySkipSideEffectGetter(property, out var skipped))
            {
                sink.Add($"{path}.{property.Name}={skipped}");

                continue;
            }

            object member;

            try
            {
                member = property.GetValue(value);
            }
            catch (TargetInvocationException getterFailure)
            {
                sink.Add($"{path}.{property.Name}=<getter threw {getterFailure.InnerException?.GetType().Name}>");

                continue;
            }
            catch (NotSupportedException)
            {
                sink.Add($"{path}.{property.Name}=<getter not invokable by reflection>");

                continue;
            }

            Walk($"{path}.{property.Name}", member, sink, depth + 1, seen);
        }

        if (swept == 0)
            sink.Add($"{path}={value}");
    }

    private static bool TryRenderLeaf(string path, object value, out string rendered)
    {
        switch (value)
        {
            case byte[] bytes:
                rendered = $"{path}={ByteRenderings.Render(bytes)}";
                return true;

            case ArraySegment<byte> segment:
                rendered = $"{path}={ByteRenderings.Render(segment.ToArray())}";
                return true;

            case Memory<byte> memory:
                rendered = $"{path}={ByteRenderings.Render(memory.ToArray())}";
                return true;

            case ReadOnlyMemory<byte> readOnlyMemory:
                rendered = $"{path}={ByteRenderings.Render(readOnlyMemory.ToArray())}";
                return true;

            case MemoryStream memoryStream:
                rendered = $"{path}.ToArray()={ByteRenderings.Render(memoryStream.ToArray())}";
                return true;

            case StringBuilder builder:
                rendered = $"{path}.ToString()='{builder}'";
                return true;

            case SecureString:
                rendered = $"{path}={SecureStringMarker}";
                return true;

            case XmlNode xmlNode:
                rendered = $"{path}.OuterXml='{xmlNode.OuterXml}'";
                return true;

            default:
                rendered = null;
                return false;
        }
    }

    private static bool TrySkipSideEffectGetter(PropertyInfo property, out string marker)
    {
        var declaring = property.DeclaringType;

        if (declaring is { IsGenericType: true })
        {
            var definition = declaring.GetGenericTypeDefinition();

            if (definition == typeof(Lazy<>) && property.Name == nameof(Lazy<object>.Value))
            {
                marker = LazyValueSkipped;
                return true;
            }

            if ((definition == typeof(Task<>) || definition == typeof(ValueTask<>)) && property.Name == nameof(Task<object>.Result))
            {
                marker = TaskResultSkipped;
                return true;
            }
        }

        marker = null;
        return false;
    }
}
