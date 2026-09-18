#nullable disable

using SoapClientCallAssistTests.Soap12.Enums;
using System;

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal sealed class JavaInteropVerdict
{

    internal const string UnreportedText = "<unreported>";

    private const string ErrorLabel = "ERROR";

    private const string InvalidLabel = "INVALID";

    private const string ValidLabel = "VALID";

    internal static readonly JavaInteropVerdict Unreported =
        new(JavaInteropVerdictKind.Unreported, null);

    private JavaInteropVerdict(JavaInteropVerdictKind kind, string errorDetail)
    {
        Kind = kind;
        ErrorDetail = errorDetail;
    }

    internal JavaInteropVerdictKind Kind { get; }

    internal string ErrorDetail { get; }

    internal bool IsValid => Kind == JavaInteropVerdictKind.Valid;

    internal bool IsInvalid => Kind == JavaInteropVerdictKind.Invalid;

    internal bool IsError => Kind == JavaInteropVerdictKind.Error;

    internal bool IsUnreported => Kind == JavaInteropVerdictKind.Unreported;

    internal static JavaInteropVerdict Parse(string reported)
    {
        if (reported is null)
            return Unreported;

        var trimmed = reported.Trim();

        if (trimmed.StartsWith(ErrorLabel, StringComparison.Ordinal))
            return new JavaInteropVerdict(JavaInteropVerdictKind.Error, ExceptionText(trimmed));

        if (trimmed.StartsWith(InvalidLabel, StringComparison.Ordinal))
            return new JavaInteropVerdict(JavaInteropVerdictKind.Invalid, null);

        if (trimmed.StartsWith(ValidLabel, StringComparison.Ordinal))
            return new JavaInteropVerdict(JavaInteropVerdictKind.Valid, null);

        return Unreported;
    }

    internal static string Or(string value) => value ?? UnreportedText;

    public override string ToString()
        => Kind switch
        {
            JavaInteropVerdictKind.Valid => ValidLabel,
            JavaInteropVerdictKind.Invalid => InvalidLabel,
            JavaInteropVerdictKind.Error => $"{ErrorLabel}({Or(ErrorDetail)})",
            _ => UnreportedText
        };

    private static string ExceptionText(string reported)
    {
        var open = reported.IndexOf('(');
        var close = reported.LastIndexOf(')');

        return open >= 0 && close > open
            ? reported.Substring(open + 1, close - open - 1).Trim()
            : reported.Substring(ErrorLabel.Length).Trim();
    }
}
