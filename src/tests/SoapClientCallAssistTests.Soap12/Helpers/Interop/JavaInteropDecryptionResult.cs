#nullable disable

using System;
using System.Text.RegularExpressions;

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal sealed class JavaInteropDecryptionResult
{

    private static readonly Regex ElementName = new(@"element=(?<name>\S+)", RegexOptions.Compiled);

    private JavaInteropDecryptionResult(string id, JavaInteropVerdict verdict, string elementLocalName)
    {
        Id = id;
        Verdict = verdict;
        ElementLocalName = elementLocalName;
    }

    internal string Id { get; }

    internal JavaInteropVerdict Verdict { get; }

    internal string ElementLocalName { get; }

    internal static JavaInteropDecryptionResult Parse(string reported)
    {
        var separator = (reported ?? string.Empty).IndexOf(" : ", StringComparison.Ordinal);

        if (separator < 0)
            return new JavaInteropDecryptionResult(reported?.Trim(), JavaInteropVerdict.Unreported, null);

        var id = reported.Substring(0, separator).Trim();
        var verdictText = reported.Substring(separator + 3).Trim();
        var element = ElementName.Match(verdictText);

        return new JavaInteropDecryptionResult(
            id,
            JavaInteropVerdict.Parse(verdictText),
            element.Success ? element.Groups["name"].Value : null);
    }

    public override string ToString()
        => $"id={JavaInteropVerdict.Or(Id)} verdict={Verdict} element={JavaInteropVerdict.Or(ElementLocalName)}";
}
