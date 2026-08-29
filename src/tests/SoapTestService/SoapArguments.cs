using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml.Linq;

namespace SoapTestService;

public sealed class SoapArguments
{
    private readonly XElement? _element;
    private readonly IReadOnlyDictionary<string, string> _named;
    private readonly IReadOnlyList<string> _positional;

    private SoapArguments(
        XElement? element,
        IReadOnlyDictionary<string, string> named,
        IReadOnlyList<string> positional)
    {
        _element = element;
        _named = named;
        _positional = positional;
    }

    public static SoapArguments FromElement(XElement operationElement)
    {
        ArgumentNullException.ThrowIfNull(operationElement);

        return new SoapArguments(operationElement, ReadOnlyDictionary<string, string>.Empty, Array.Empty<string>());
    }

    public static SoapArguments FromRequestLine(IReadOnlyDictionary<string, string> named,
        IReadOnlyList<string> positional)
    {
        ArgumentNullException.ThrowIfNull(named);
        ArgumentNullException.ThrowIfNull(positional);

        return new SoapArguments(null, named, positional);
    }

    public XElement? GetElement(string name) => SoapElement.Find(_element, name);

    public string? GetString(string name, int position)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfNegative(position);

        if (_element is not null)
            return GetElement(name)?.Value;

        if (_named.TryGetValue(name, out var value))
            return value;

        return position < _positional.Count ? _positional[position] : null;
    }

    public bool TryGetInt32(string name, int position, int fallback, out int value)
    {
        var raw = GetString(name, position);

        if (string.IsNullOrWhiteSpace(raw))
        {
            value = fallback;

            return true;
        }

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
