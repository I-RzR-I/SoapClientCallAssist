#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace SoapClientCallAssistTests.Soap12.Helpers.Wire;

internal static class WsSecurityWireGoldenIntegrity
{

    private const string SelfClosingWithoutSpace = "\"/>";

    private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };

    internal static void AssertWellFormed(string goldenPath)
    {
        Assert.IsTrue(File.Exists(goldenPath), Explain("the file is missing", goldenPath));

        var bytes = File.ReadAllBytes(goldenPath);

        Assert.IsFalse(bytes.Length >= Utf8Bom.Length && bytes.AsSpan(0, Utf8Bom.Length).SequenceEqual(Utf8Bom), Explain("it starts with a UTF-8 BOM", goldenPath));

        var text = Encoding.UTF8.GetString(bytes);

        Assert.IsTrue(text.EndsWith("\r\n", StringComparison.Ordinal), Explain("it does not end with a single CRLF", goldenPath));

        Assert.IsTrue(text.IndexOfAny(new[] { '\r', '\n' }) == text.Length - 2, Explain("it spans more than one line", goldenPath));

        Assert.IsFalse(text.Contains('\t'), Explain("it contains tabs", goldenPath));

        Assert.IsFalse(text.Contains(SelfClosingWithoutSpace, StringComparison.Ordinal), Explain("its empty elements were rewritten as \"/> instead of the signer's \" />", goldenPath));
    }

    private static string Explain(string problem, string goldenPath)
        => $"{Path.GetFileName(goldenPath)} was reformatted by an editor or XML formatter: {problem}. "
           + "Wire goldens pin the exact envelope the signer emits (child order, namespace placement, self-closing form) "
           + "as one line with a single trailing CRLF, no BOM, no tabs, so a pretty-printer changes what they pin. "
           + $"Do not hand-edit them; regenerate every golden from the current signer by running WsSecurityWireGoldenTests with {WsSecurityWireGoldenMatrix.RegenerationVariable}=1. | {goldenPath}";
}