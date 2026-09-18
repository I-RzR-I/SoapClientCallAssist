#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssist.Abstractions;
using SoapClientCallAssistTests.Wcf.Service.Common;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Wcf.Helpers;

internal static class ProbeReaders
{
    private static readonly XNamespace Identity = WcfProbeContract.Namespace;

    internal static XElement ReadPayload(ISoapClientEndpoint client, string responseBody)
    {
        var node = WcfCallSupport.Unwrap(client.GetXNodeResponseBody(responseBody), "GetXNodeResponseBody");

        return (node as XDocument)?.Root
               ?? node as XElement
               ?? throw new AssertFailedException($"The response payload is a {node.GetType().Name}, not an element.");
    }

    internal static WcfCallerIdentity ReadWhoAmI(ISoapClientEndpoint client, string responseBody, XNamespace service)
    {
        var result = ReadPayload(client, responseBody).Element(service + "WhoAmIResult")
            ?? throw new AssertFailedException("The WhoAmI response carries no WhoAmIResult.");

        return new WcfCallerIdentity
        {
            AuthenticationType = result.Element(Identity + "AuthenticationType")?.Value ?? string.Empty,
            Name = result.Element(Identity + "Name")?.Value ?? string.Empty
        };
    }

    internal static string ReadEcho(ISoapClientEndpoint client, string responseBody, XNamespace service)
        => ReadPayload(client, responseBody).Element(service + "EchoResult")?.Value
           ?? throw new AssertFailedException("The Echo response carries no EchoResult.");
}
