#nullable disable

using System.Security.Cryptography.Xml;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal sealed class SymmetricTestSignedXml : SignedXml
{

    internal SymmetricTestSignedXml(XmlDocument document) : base(document)
    {
    }

    public override XmlElement GetIdElement(XmlDocument document, string idValue)
    {
        var byDefault = base.GetIdElement(document, idValue);
        if (byDefault is not null)
            return byDefault;

        foreach (var candidate in document.GetElementsByTagName("*"))
            if (candidate is XmlElement element && element.GetAttribute("Id", WsSecurityTestSupport.WsuNamespace) == idValue)
                return element;

        return null;
    }
}
