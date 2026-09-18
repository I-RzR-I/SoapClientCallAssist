using System;
using System.Globalization;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Helpers;

internal static class UsernameTokenHeader
{

    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    private static readonly TimeSpan TimestampTimeToLive = TimeSpan.FromMinutes(5);

    internal static XElement Build(XNamespace soapNamespace, string userName, string password)
    {
        var created = DateTime.UtcNow;

        return new XElement(
            WsSecurityNames.Wsse + "Security",
            new XAttribute(XNamespace.Xmlns + "wsse", WsSecurityNames.Wsse),
            new XAttribute(XNamespace.Xmlns + "wsu", WsSecurityNames.Wsu),
            new XAttribute(soapNamespace + "mustUnderstand", "1"),
            new XElement(
                WsSecurityNames.Wsu + "Timestamp",
                new XAttribute(WsSecurityNames.Wsu + "Id", "ts-" + Guid.NewGuid().ToString("N")),
                new XElement(WsSecurityNames.Wsu + "Created", Format(created)),
                new XElement(WsSecurityNames.Wsu + "Expires", Format(created + TimestampTimeToLive))),
            new XElement(
                WsSecurityNames.Wsse + "UsernameToken",
                new XAttribute(WsSecurityNames.Wsu + "Id", "ut-" + Guid.NewGuid().ToString("N")),
                new XElement(WsSecurityNames.Wsse + "Username", userName),
                new XElement(
                    WsSecurityNames.Wsse + "Password",
                    new XAttribute("Type", WsSecurityNames.PasswordTextType),
                    password)));
    }

    private static string Format(DateTime utc) => utc.ToString(TimestampFormat, CultureInfo.InvariantCulture);
}
