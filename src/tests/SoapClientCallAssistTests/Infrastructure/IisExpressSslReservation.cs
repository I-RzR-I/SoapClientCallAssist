#region U S I N G

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

#endregion

namespace SoapClientCallAssistTests.Infrastructure
{
    public static class IisExpressSslReservation
    {
        public const string IisExpressApplicationId = "{214124cd-d05b-4309-9af9-9caa44b2b74a}";

        public const int ReservedRangeStart = 44300;

        public const int ReservedRangeEnd = 44399;

        private const string NetshExecutable = "netsh.exe";

        private const string AnyAddress = "0.0.0.0";

        private static readonly TimeSpan NetshTimeout = TimeSpan.FromSeconds(15);

        private static readonly Regex CertificateHashLine = new Regex(
            @"^\s*[^:\r\n]*:\s*(?<hash>[0-9A-Fa-f]{40})\s*$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);

        public static string ReadServerCertificateThumbprint(int port)
        {
            var ipPort = AnyAddress + ":" + port.ToString(CultureInfo.InvariantCulture);
            var output = RunNetsh("http show sslcert ipport=" + ipPort);

            var match = CertificateHashLine.Match(output);
            if (match.Success)
                return match.Groups["hash"].Value.ToUpperInvariant();

            throw new TestServiceUnavailableException(
                "No http.sys certificate reservation exists for " + ipPort + ", so IIS Express cannot serve TLS " +
                "on port " + port + " and the transport tests have nothing to pin." + Environment.NewLine +
                "The IIS Express installer normally reserves ports " + ReservedRangeStart + "-" + ReservedRangeEnd +
                " with its development certificate. Recreate the reservation once, from an elevated prompt, with " +
                "the thumbprint of the IIS Express development certificate (Cert:\\LocalMachine\\My, CN=localhost):" +
                Environment.NewLine +
                "  netsh http add sslcert ipport=" + ipPort + " certhash=<thumbprint> appid=" + IisExpressApplicationId +
                Environment.NewLine + "netsh reported:" + Environment.NewLine + output.Trim());
        }

        private static string RunNetsh(string arguments)
        {
            var startInfo = new ProcessStartInfo(NetshExecutable, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        throw new TestServiceUnavailableException(
                            "netsh could not be started, so the http.sys certificate reservation cannot be read.");

                    var standardOutput = process.StandardOutput.ReadToEndAsync();
                    var standardError = process.StandardError.ReadToEndAsync();

                    if (!process.WaitForExit((int)NetshTimeout.TotalMilliseconds))
                    {
                        process.Kill();

                        throw new TestServiceUnavailableException(
                            "netsh did not finish reading the http.sys certificate reservation within " +
                            NetshTimeout.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture) + "s.");
                    }

                    return standardOutput.GetAwaiter().GetResult() + Environment.NewLine + standardError.GetAwaiter().GetResult();
                }
            }
            catch (Exception exception) when (
                exception is System.ComponentModel.Win32Exception ||
                exception is InvalidOperationException)
            {
                throw new TestServiceUnavailableException(
                    "netsh could not be run to read the http.sys certificate reservation: " + exception.Message);
            }
        }
    }
}
