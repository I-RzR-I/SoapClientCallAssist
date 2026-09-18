using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace TestSoapServiceN45.Security
{
    public sealed class WsuSignedXml : SignedXml
    {
        public WsuSignedXml(XmlDocument document) : base(document)
        {
        }

        public override XmlElement GetIdElement(XmlDocument document, string idValue)
        {
            var byDefault = base.GetIdElement(document, idValue);
            if (byDefault != null)
                return byDefault;

            var matches = FindByWsuId(document, idValue);
            if (matches.Count == 0)
                return null;

            if (matches.Count > 1)
                throw new CryptographicException("Ambiguous wsu:Id '" + idValue + "'.");

            return matches[0];
        }

        public static List<XmlElement> FindByWsuId(XmlDocument document, string id)
        {
            var matches = new List<XmlElement>();

            if (document == null || string.IsNullOrEmpty(id))
                return matches;

            foreach (XmlNode candidate in document.GetElementsByTagName("*"))
            {
                var element = candidate as XmlElement;
                if (element == null || !element.HasAttribute("Id", WsSecurityAsmxNames.WsuNamespace))
                    continue;

                if (string.Equals(element.GetAttribute("Id", WsSecurityAsmxNames.WsuNamespace), id, StringComparison.Ordinal))
                    matches.Add(element);
            }

            return matches;
        }
    }
}
