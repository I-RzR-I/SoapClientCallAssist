#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RzR.ResultMessage.Abstractions;
using SoapClientCallAssistTests.Soap12.Helpers.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace SoapClientCallAssistTests.Soap12.Helpers.Results;

internal static class SecretLeakAssert
{

    internal static void CarriesNoSecret(IResult result, string what, params ForbiddenSecret[] extraForbidden)
    {
        var swept = MessageLeakSweep.Render(result);

        foreach (var forbidden in Forbidden().Concat(extraForbidden.SelectMany(secret => secret.Renderings())))
        {
            Assert.IsFalse(
                swept.Contains(forbidden.Value, StringComparison.OrdinalIgnoreCase),
                $"{what} | {forbidden.Name} | {forbidden.Value} | {swept}");
        }
    }

    internal static void CarriesSecret(IResult result, string secret, string what)
    {
        var swept = MessageLeakSweep.Render(result);

        Assert.IsTrue(swept.Contains(secret, StringComparison.Ordinal), $"{what} | {swept}");
    }

    private static IEnumerable<(string Name, string Value)> Forbidden()
    {
        foreach (var certificate in Certificates())
        {
            yield return ("the signing certificate subject", certificate.Subject);
            yield return ("the signing certificate thumbprint", certificate.Thumbprint);
            yield return ("the signing certificate serial number", certificate.SerialNumber);
            yield return ("the signing certificate DER bytes", Convert.ToBase64String(certificate.RawData)[..40]);
            yield return ("the signing certificate public key", certificate.GetPublicKeyString()[..40]);

            foreach (var rendering in ForbiddenSecret.OfBytes("the signing certificate DER bytes", certificate.RawData).Renderings())
                yield return rendering;
        }

        yield return ("a SecureString instance", MessageLeakSweep.SecureStringMarker);
        yield return ("a BCL cryptography type name", "System.Security.Cryptography");
        yield return ("a stack frame", ".cs:line");
        yield return ("a stack frame", "   at ");
        yield return ("an exception type name", "Exception:");
        yield return ("the test binary directory", AppContext.BaseDirectory);
        yield return ("the host temporary directory", Path.GetTempPath());
    }

    private static IEnumerable<X509Certificate2> Certificates()
        => new[]
        {
            WsSecurityTestSupport.SigningCertificate,
            WsSecurityTestSupport.PublicOnlyCertificate,
            WsSecurityTestSupport.OtherCertificate
        }.Where(certificate => certificate is not null);
}
