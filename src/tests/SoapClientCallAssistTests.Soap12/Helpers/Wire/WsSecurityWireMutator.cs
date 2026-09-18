#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace SoapClientCallAssistTests.Soap12.Helpers.Wire;

internal static class WsSecurityWireMutator
{

    private const string ReferenceUriMarker = "<Reference URI=\"";

    private const string SignedInfoEnd = "</SignedInfo>";

    private const string SignatureStart = "<Signature ";

    private const string SignatureEnd = "</Signature>";

    private const string KeyInfoStart = "<KeyInfo>";

    internal static string ReplaceOnce(string wire, string find, string replacement)
    {
        var index = wire.IndexOf(find, StringComparison.Ordinal);

        Assert.IsTrue(index >= 0, $"{find} | {wire}");

        return wire.Remove(index, find.Length).Insert(index, replacement);
    }

    internal static string ReplaceAll(string wire, string find, string replacement)
    {
        Assert.IsTrue(wire.Contains(find, StringComparison.Ordinal), $"{find} | {wire}");

        return wire.Replace(find, replacement);
    }

    internal static string WithFirstReferenceUri(string wire, string uri)
    {
        var start = wire.IndexOf(ReferenceUriMarker, StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, wire);

        var valueStart = start + ReferenceUriMarker.Length;
        var valueEnd = wire.IndexOf('"', valueStart);

        Assert.IsTrue(valueEnd > valueStart - 1);

        return wire[..valueStart] + uri + wire[valueEnd..];
    }

    internal static string WithTransformAlgorithm(string wire, string algorithm)
        => ReplaceAll(
            wire,
            $"<Transform Algorithm=\"{WsSecurityTestSupport.ExclusiveC14N}\" />",
            $"<Transform Algorithm=\"{algorithm}\" />");

    internal static string WithCanonicalizationAlgorithm(string wire, string algorithm)
        => ReplaceOnce(
            wire,
            $"<CanonicalizationMethod Algorithm=\"{WsSecurityTestSupport.ExclusiveC14N}\" />",
            $"<CanonicalizationMethod Algorithm=\"{algorithm}\" />");

    internal static string WithSignatureMethod(string wire, string algorithm)
        => ReplaceOnce(
            wire,
            $"<SignatureMethod Algorithm=\"{WsSecurityTestSupport.RsaSha256Signature}\" />",
            $"<SignatureMethod Algorithm=\"{algorithm}\" />");

    internal static string WithDigestMethod(string wire, string algorithm)
        => ReplaceAll(
            wire,
            $"<DigestMethod Algorithm=\"{WsSecurityTestSupport.Sha256Digest}\" />",
            $"<DigestMethod Algorithm=\"{algorithm}\" />");

    internal static string WithHmacOutputLength(string wire)
        => ReplaceOnce(
            wire,
            $"<SignatureMethod Algorithm=\"{WsSecurityTestSupport.RsaSha256Signature}\" />",
            $"<SignatureMethod Algorithm=\"{WsSecurityTestSupport.RsaSha256Signature}\">"
            + "<HMACOutputLength>80</HMACOutputLength></SignatureMethod>");

    internal static string WithRetrievalMethodInKeyInfo(string wire)
        => ReplaceOnce(wire, KeyInfoStart, KeyInfoStart + "<RetrievalMethod URI=\"#planted\" />");

    internal static string WithoutReferences(string wire)
    {
        var start = wire.IndexOf(ReferenceUriMarker, StringComparison.Ordinal);
        var end = wire.IndexOf(SignedInfoEnd, StringComparison.Ordinal);

        Assert.IsTrue(start >= 0 && end > start, wire);

        return wire[..start] + wire[end..];
    }

    internal static string WithDuplicatedSignature(string wire)
    {
        var signature = SignatureElement(wire);
        var end = wire.IndexOf(SignatureEnd, StringComparison.Ordinal) + SignatureEnd.Length;

        return wire[..end] + signature + wire[end..];
    }

    internal static string WithoutSignature(string wire)
    {
        var start = wire.IndexOf(SignatureStart, StringComparison.Ordinal);
        var end = wire.IndexOf(SignatureEnd, StringComparison.Ordinal) + SignatureEnd.Length;

        Assert.IsTrue(start >= 0 && end > start, wire);

        return wire[..start] + wire[end..];
    }

    private static string SignatureElement(string wire)
    {
        var start = wire.IndexOf(SignatureStart, StringComparison.Ordinal);
        var end = wire.IndexOf(SignatureEnd, StringComparison.Ordinal) + SignatureEnd.Length;

        Assert.IsTrue(start >= 0 && end > start, wire);

        return wire[start..end];
    }

    internal static string WithAppendedDecoyBody(string wire)
        => ReplaceOnce(
            wire,
            "</soap:Envelope>",
            "<soap:Body><IsValid xmlns=\"" + WsSecurityTestSupport.Service
            + "\"><id>attacker-controlled</id></IsValid></soap:Body></soap:Envelope>");

    internal static string WithTamperedBody(string wire)
        => ReplaceOnce(wire, "<id>s1</id>", "<id>s2</id>");

    internal static string WithHeaderChild(string wire, string element)
        => ReplaceOnce(wire, "</soap:Header>", element + "</soap:Header>");

    internal static string WithDecoyCarryingPlainId(string wire, string id)
        => WithHeaderChild(wire, $"<Decoy Id=\"{id}\"><id>attacker-controlled</id></Decoy>");

    internal static string WithTimestampWindow(string wire, DateTimeOffset created, DateTimeOffset expires)
    {
        var document = WsSecurityTestSupport.ParseWire(wire);

        RewriteTimestamp(document, created, expires);

        return document.OuterXml;
    }

    internal static string ResignedWithTimestampWindow(
        string wire, DateTimeOffset created, DateTimeOffset expires, X509Certificate2 certificate)
    {
        var document = WsSecurityTestSupport.ParseWire(wire);
        var namespaces = WsSecurityTestSupport.Namespaces(document);

        RewriteTimestamp(document, created, expires);

        var signedInfo = WsSecurityTestSupport.RequireNode(
            document, "//ds:Signature/ds:SignedInfo", namespaces, "SignedInfo");

        foreach (XmlElement reference in signedInfo.SelectNodes("ds:Reference", namespaces))
            RewriteDigest(document, namespaces, reference);

        var signatureValue = WsSecurityTestSupport.RequireNode(
            document, "//ds:Signature/ds:SignatureValue", namespaces, "SignatureValue");

        using var key = certificate.GetRSAPrivateKey();

        Assert.IsNotNull(key);

        signatureValue.InnerText = Convert.ToBase64String(
            key.SignData(
                WsSecurityTestSupport.ExclusiveC14NBytes(signedInfo),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1));

        return document.OuterXml;
    }

    private static void RewriteTimestamp(XmlDocument document, DateTimeOffset created, DateTimeOffset expires)
    {
        var namespaces = WsSecurityTestSupport.Namespaces(document);
        var timestamp = WsSecurityTestSupport.RequireNode(
            document, "//wsu:Timestamp", namespaces, "wsu:Timestamp");

        WsSecurityTestSupport.RequireNode(timestamp, "wsu:Created", namespaces, "wsu:Created")
            .InnerText = WsSecurityTestSupport.TimestampInstant(created);

        WsSecurityTestSupport.RequireNode(timestamp, "wsu:Expires", namespaces, "wsu:Expires")
            .InnerText = WsSecurityTestSupport.TimestampInstant(expires);
    }

    private static void RewriteDigest(XmlDocument document, XmlNamespaceManager namespaces, XmlElement reference)
    {
        var uri = reference.GetAttribute("URI");

        Assert.IsTrue(uri.StartsWith("#", StringComparison.Ordinal), $"{uri}");

        var id = uri[1..];
        var covered = document.SelectSingleNode(
            $"//*[@*[local-name()='Id' and namespace-uri()='{WsSecurityTestSupport.WsuNamespace}']='{id}']",
            namespaces) as XmlElement;

        Assert.IsNotNull(covered, $"{id} | {uri}");

        WsSecurityTestSupport.RequireNode(reference, "ds:DigestValue", namespaces, $"{uri}")
            .InnerText = WsSecurityTestSupport.IndependentExclusiveC14NDigest(covered);
    }

    internal static string WithDuplicateWsuId(string wire, string id)
        => WithHeaderChild(
            wire,
            $"<Decoy xmlns:wsu=\"{WsSecurityTestSupport.WsuNamespace}\" wsu:Id=\"{id}\"><id>attacker-controlled</id></Decoy>");
}
