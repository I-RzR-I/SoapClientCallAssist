#nullable disable

using System;

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal sealed class JavaInteropReferenceResult
{

    internal JavaInteropReferenceResult(string uri) => Uri = uri;

    internal string Uri { get; }

    internal string DigestMethod { get; set; }

    internal JavaInteropVerdict DigestValid { get; set; } = JavaInteropVerdict.Unreported;

    internal string ExpectedDigest { get; set; }

    internal string CalculatedDigest { get; set; }

    internal string Dereferenced { get; set; }

    internal bool Resolved =>
        Dereferenced is not null && !Dereferenced.StartsWith("NULL", StringComparison.Ordinal);

    public override string ToString()
        => $"URI={Uri} digest={DigestValid} " +
           $"digestMethod={JavaInteropVerdict.Or(DigestMethod)} " +
           $"dereferenced={JavaInteropVerdict.Or(Dereferenced)} " +
           $"expected={JavaInteropVerdict.Or(ExpectedDigest)} calculated={JavaInteropVerdict.Or(CalculatedDigest)}";
}
