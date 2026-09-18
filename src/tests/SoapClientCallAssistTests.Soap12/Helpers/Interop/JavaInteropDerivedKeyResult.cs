#nullable disable

using System.Text.RegularExpressions;

namespace SoapClientCallAssistTests.Soap12.Helpers.Interop;

internal sealed class JavaInteropDerivedKeyResult
{

    private static readonly Regex Shape = new(
        @"^id=(?<id>\S*) namespace=(?<ns>\S*) label=(?<label>.*?) offset=(?<offset>\d+) length=(?<length>\d+) generation=(?<generation>\S+) nonce=(?<nonce>\S*) key=(?<key>\S*)$",
        RegexOptions.Compiled);

    private JavaInteropDerivedKeyResult(string id, string ns, string label, int offset, int length, string nonceBase64, string keyBase64)
    {
        Id = id;
        Namespace = ns;
        Label = label;
        Offset = offset;
        Length = length;
        NonceBase64 = nonceBase64;
        KeyBase64 = keyBase64;
    }

    internal string Id { get; }

    internal string Namespace { get; }

    internal string Label { get; }

    internal int Offset { get; }

    internal int Length { get; }

    internal string NonceBase64 { get; }

    internal string KeyBase64 { get; }

    internal static JavaInteropDerivedKeyResult Parse(string reported)
    {
        var match = Shape.Match((reported ?? string.Empty).Trim());

        if (!match.Success)
            return new JavaInteropDerivedKeyResult(null, null, null, -1, -1, null, null);

        return new JavaInteropDerivedKeyResult(
            match.Groups["id"].Value,
            match.Groups["ns"].Value,
            match.Groups["label"].Value,
            int.Parse(match.Groups["offset"].Value),
            int.Parse(match.Groups["length"].Value),
            match.Groups["nonce"].Value,
            match.Groups["key"].Value);
    }

    public override string ToString()
        => $"id={JavaInteropVerdict.Or(Id)} label={JavaInteropVerdict.Or(Label)} offset={Offset} length={Length} " +
           $"nonce={JavaInteropVerdict.Or(NonceBase64)} key={JavaInteropVerdict.Or(KeyBase64)}";
}
