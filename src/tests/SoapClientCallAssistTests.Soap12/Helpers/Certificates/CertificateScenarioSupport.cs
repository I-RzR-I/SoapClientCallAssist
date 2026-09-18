#nullable disable

using RzR.ResultMessage.Abstractions;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SoapClientCallAssistTests.Soap12.Helpers.Certificates;

internal static class CertificateScenarioSupport
{

    internal const string PfxPassword = "Pfx-Pwd-For-Tests!9f2c";

    internal const int Rsa2048SignatureValueLength = 344;

    internal const int Rsa4096SignatureValueLength = 684;

    internal static X509Certificate2 CreateRsaCertificate(string subject, int keySize)
        => TestCertificates.SelfSignedRsa(subject, keySize);

    internal static X509Certificate2 CreateExpiredRsaCertificate(string subject)
        => TestCertificates.SelfSignedRsa(subject, 2048, DateTimeOffset.UtcNow.AddDays(-400), DateTimeOffset.UtcNow.AddDays(-35));

    internal static X509Certificate2 CreateEcdsaCertificate(string subject)
        => TestCertificates.Ecdsa(subject);

    internal static string WritePfx(X509Certificate2 certificate, string password)
    {
        var path = Path.Combine(Path.GetTempPath(), $"scca-cert-scenario-{Guid.NewGuid():N}.pfx");

        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));

        return path;
    }

    internal static X509Certificate2 LoadPfx(string path, string password, X509KeyStorageFlags flags)
        => X509CertificateLoader.LoadPkcs12FromFile(path, password, flags);

    internal static void Delete(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            File.Delete(path);
    }

    internal static string SignatureValue(string wire)
        => WireText(wire, "//ds:Signature/ds:SignatureValue", "ds:SignatureValue");

    internal static string BinarySecurityToken(string wire)
        => WireText(wire, "//wsse:BinarySecurityToken", "wsse:BinarySecurityToken");

    internal static string FullMessageText(IResult result)
    {
        if (result?.Messages is null)
            return "<no messages>";

        var text = new StringBuilder();

        foreach (var message in result.Messages)
        {
            text.Append('[').Append(message.Key ?? "<null key>").Append("] ");
            text.Append(message.Message?.Info ?? "<null info>");

            if (message.Message?.Details is null)
                continue;

            foreach (var detail in message.Message.Details)
                text.Append(" || ").Append(detail);
        }

        return text.Length == 0 ? "<no messages>" : text.ToString();
    }

    private static string WireText(string wire, string xpath, string what)
    {
        var document = WsSecurityTestSupport.ParseWire(wire);

        return WsSecurityTestSupport
            .RequireNode(document, xpath, WsSecurityTestSupport.Namespaces(document), what)
            .InnerText;
    }
}
