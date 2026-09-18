using System.Xml.Linq;

namespace SoapTestService;

public static class SoapElement
{
    public static XElement? Child(XElement? parent, string localName)
    {
        ArgumentException.ThrowIfNullOrEmpty(localName);

        return parent?.Elements().FirstOrDefault(element => Matches(element, localName));
    }

    public static XElement? Find(XElement? parent, string localName)
        => Child(parent, localName)
           ?? parent?.Descendants().FirstOrDefault(element => Matches(element, localName));

    public static string Text(XElement? parent, string localName)
        => Child(parent, localName)?.Value ?? string.Empty;

    private static bool Matches(XElement element, string localName)
        => string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase);
}
