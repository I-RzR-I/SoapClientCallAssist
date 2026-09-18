#nullable disable

using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Helpers;

internal static class ProbeBodies
{

    internal static XElement WhoAmI(XNamespace service) => new(service + "WhoAmI");

    internal static XElement Echo(XNamespace service, string value) => new(service + "Echo", new XElement(service + "value", value));
}
