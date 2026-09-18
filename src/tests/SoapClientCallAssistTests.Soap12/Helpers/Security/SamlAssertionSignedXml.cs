#nullable disable

using System.Security.Cryptography.Xml;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Helpers.Security;

internal sealed class SamlAssertionSignedXml : SignedXml
{

    internal SamlAssertionSignedXml(XmlDocument document) : base(document)
    {
    }

    public override XmlElement GetIdElement(XmlDocument document, string idValue)
    {
        var byDefault = base.GetIdElement(document, idValue);

        if (byDefault is not null)
            return byDefault;

        foreach (XmlNode node in document.GetElementsByTagName("*"))
        {
            if (node is not XmlElement element)
                continue;

            if (element.GetAttribute("Id", WsSecurityTestSupport.WsuNamespace) == idValue)
                return element;

            if (element.LocalName == "Assertion"
                && element.NamespaceURI == SamlTestSupport.Saml11Namespace
                && element.GetAttribute("AssertionID") == idValue)
                return element;
        }

        return null;
    }
}
