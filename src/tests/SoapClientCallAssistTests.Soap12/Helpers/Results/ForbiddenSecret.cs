#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace SoapClientCallAssistTests.Soap12.Helpers.Results;

internal sealed class ForbiddenSecret
{

    private readonly string _name;

    private readonly byte[] _bytes;

    private readonly string _text;

    private ForbiddenSecret(string name, byte[] bytes, string text)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A forbidden secret needs a name so a leak report can say what leaked.", nameof(name));

        if (bytes is null || bytes.Length == 0)
            throw new ArgumentException("An empty forbidden value would match every sweep.", nameof(bytes));

        _name = name;
        _bytes = bytes;
        _text = text;
    }

    internal static ForbiddenSecret OfBytes(string name, byte[] bytes) => new(name, bytes, null);

    internal static ForbiddenSecret OfText(string name, string text)
        => new(name, string.IsNullOrEmpty(text) ? null : Encoding.UTF8.GetBytes(text), text);

    internal IEnumerable<(string Name, string Value)> Renderings()
    {
        if (_text is not null)
            yield return (_name, _text);

        foreach (var (form, text) in ByteRenderings.Of(_bytes))
            yield return ($"{_name} as {form}", text);
    }
}
