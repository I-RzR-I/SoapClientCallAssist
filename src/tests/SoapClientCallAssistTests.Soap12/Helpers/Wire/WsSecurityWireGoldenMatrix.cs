#nullable disable

using RzR.ResultMessage.Abstractions;
using SoapClientCallAssist.Enums;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace SoapClientCallAssistTests.Soap12.Helpers.Wire;

internal static class WsSecurityWireGoldenMatrix
{

    internal const string ExtraHeaderId = "hdr-1";

    internal const string Actor = "urn:test-actor";

    internal const string GoldenExtension = ".wire";

    internal const string RegenerationVariable = "SOAPCLIENTCALLASSIST_REGENERATE_GOLDENS";

    internal static readonly bool RegenerationRequested = Environment.GetEnvironmentVariable(RegenerationVariable) == "1";

    internal static readonly string GoldenDirectory = RegenerationRequested
        ? SourceGoldenDirectory()
        : Path.Combine(AppContext.BaseDirectory, "TestData", "WireGolden");

    private static readonly SoapProtocolType[] Protocols = { SoapProtocolType.SOAP_1_1, SoapProtocolType.SOAP_1_2 };

    internal static IReadOnlyList<WsSecurityWireGoldenCase> Cases { get; } = BuildCases();

    internal static IReadOnlyList<WsSecurityWireGoldenCase> RefusedCases { get; } = BuildRefusedCases();

    internal static string GoldenPath(WsSecurityWireGoldenCase goldenCase)
        => Path.Combine(GoldenDirectory, goldenCase.Name + GoldenExtension);

    internal static string ReadGolden(WsSecurityWireGoldenCase goldenCase)
        => File.ReadAllText(GoldenPath(goldenCase)).TrimEnd('\r', '\n');

    internal static void RegenerateIfRequested()
    {
        if (!RegenerationRequested)
            return;

        if (!Directory.Exists(GoldenDirectory))
            throw new DirectoryNotFoundException($"{RegenerationVariable}=1 needs the source tree; {GoldenDirectory} does not exist.");

        foreach (var goldenCase in Cases)
            File.WriteAllText(GoldenPath(goldenCase), WsSecurityWireNormaliser.Normalise(Wire(goldenCase)) + "\r\n", new UTF8Encoding(false));
    }

    internal static IResult<HttpRequestMessage> Build(WsSecurityWireGoldenCase goldenCase)
        => WsSecurityTestSupport.BuildWithHeaders(
            goldenCase.Protocol,
            HttpMethod.Post,
            goldenCase.Security(),
            goldenCase.ExtraHeader ? new[] { ExtraHeader() } : null);

    internal static string Wire(WsSecurityWireGoldenCase goldenCase)
        => WsSecurityTestSupport.Wire(Build(goldenCase), $"Build {goldenCase.Name}");

    private static XElement ExtraHeader()
        => new(
            WsSecurityTestSupport.Service + "Extra",
            new XAttribute(XNamespace.Xmlns + "wsu", WsSecurityTestSupport.WsuNamespace),
            new XAttribute(XNamespace.Get(WsSecurityTestSupport.WsuNamespace) + "Id", ExtraHeaderId),
            "x");

    private static List<WsSecurityWireGoldenCase> BuildCases()
    {
        var cases = new List<WsSecurityWireGoldenCase>();

        foreach (var protocol in Protocols)
        {
            var prefix = Prefix(protocol);

            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "baseline", Protocol = protocol });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "body-off", Protocol = protocol, SignBody = false });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "timestamp-off", Protocol = protocol, IncludeTimestamp = false });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "timestamp-unsigned", Protocol = protocol, SignTimestamp = false });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "mustunderstand-off", Protocol = protocol, MustUnderstand = false });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "actor", Protocol = protocol, SecurityActor = Actor });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "inclusive-c14n", Protocol = protocol, Canonicalization = SoapCanonicalizationType.InclusiveC14N });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "rsa-sha1", Protocol = protocol, SignatureAlgorithm = SoapSignatureAlgorithmType.RsaSha1 });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "digest-sha1", Protocol = protocol, DigestAlgorithm = SoapDigestAlgorithmType.Sha1 });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "extra-header", Protocol = protocol, ExtraHeader = true });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "extra-header-only", Protocol = protocol, SignBody = false, IncludeTimestamp = false, ExtraHeader = true });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "extra-header-and-timestamp", Protocol = protocol, SignBody = false, ExtraHeader = true });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "extra-header-and-body", Protocol = protocol, IncludeTimestamp = false, ExtraHeader = true });
            cases.Add(new WsSecurityWireGoldenCase
            {
                Name = prefix + "all-non-default",
                Protocol = protocol,
                MustUnderstand = false,
                SecurityActor = Actor,
                Canonicalization = SoapCanonicalizationType.InclusiveC14N,
                SignatureAlgorithm = SoapSignatureAlgorithmType.RsaSha1,
                DigestAlgorithm = SoapDigestAlgorithmType.Sha1,
                ExtraHeader = true
            });
        }

        return cases;
    }

    private static List<WsSecurityWireGoldenCase> BuildRefusedCases()
    {
        var cases = new List<WsSecurityWireGoldenCase>();

        foreach (var protocol in Protocols)
        {
            var prefix = Prefix(protocol);

            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "nothing-to-sign", Protocol = protocol, SignBody = false, IncludeTimestamp = false });
            cases.Add(new WsSecurityWireGoldenCase { Name = prefix + "timestamp-unsigned-body-off", Protocol = protocol, SignBody = false, SignTimestamp = false });
        }

        return cases;
    }

    private static string Prefix(SoapProtocolType protocol)
        => protocol == SoapProtocolType.SOAP_1_1 ? "soap11-" : "soap12-";

    private static string SourceGoldenDirectory([CallerFilePath] string thisFile = null)
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile), "..", "..", "TestData", "WireGolden"));
}
