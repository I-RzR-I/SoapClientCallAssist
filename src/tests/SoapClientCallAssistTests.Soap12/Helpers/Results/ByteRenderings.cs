#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Results;

internal static class ByteRenderings
{

    internal static IEnumerable<(string Form, string Text)> Of(byte[] bytes)
    {
        yield return ("dec", string.Join(" ", bytes.Select(value => value.ToString(CultureInfo.InvariantCulture))));
        yield return ("hex", Convert.ToHexString(bytes).ToLowerInvariant());
        yield return ("HEX", Convert.ToHexString(bytes));
        yield return ("base64", Convert.ToBase64String(bytes));
        yield return ("bitconverter", BitConverter.ToString(bytes));
    }

    internal static string Render(byte[] bytes)
        => $"<byte[{bytes.Length.ToString(CultureInfo.InvariantCulture)}]> "
            + string.Join(" ", Of(bytes).Select(rendering => $"{rendering.Form}={rendering.Text}"));
}
