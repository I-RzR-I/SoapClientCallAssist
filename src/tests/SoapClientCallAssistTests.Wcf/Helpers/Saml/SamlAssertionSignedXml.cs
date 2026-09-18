#nullable disable

using System.Security.Cryptography.Xml;
using System.Xml;

namespace SoapClientCallAssistTests.Wcf.Helpers.Saml;

internal sealed class SamlAssertionSignedXml : SignedXml
{

    internal SamlAssertionSignedXml(XmlDocument document) : base(document)
    {
    }

    public override XmlElement GetIdElement(XmlDocument document, string idValue)
    {
        var byDefault = base.GetIdElement(document, idValue);

        if (byDefault != null)
            return byDefault;

        foreach (XmlNode node in document.GetElementsByTagName("*"))
        {
            if (node is XmlElement element
                && element.LocalName == "Assertion"
                && element.NamespaceURI == SamlCallSupport.Saml11Namespace
                && element.GetAttribute("AssertionID") == idValue)
                return element;
        }

        return null;
    }
}
