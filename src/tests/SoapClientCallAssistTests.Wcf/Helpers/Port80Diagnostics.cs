using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SoapClientCallAssistTests.Wcf.Helpers;

internal static class Port80Diagnostics
{

    private static readonly Regex ListeningOnPort80 = new(
        @"^\s*TCP\s+\S+:80\s+\S+\s+LISTENING\s+(?<pid>\d+)\s*$",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly TimeSpan NetstatTimeout = TimeSpan.FromSeconds(10);

    internal static string DescribeHolder()
    {
        try
        {
            var netstat = new ProcessStartInfo("netstat", "-ano")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(netstat);

            if (process is null)
                return "netstat could not be started";

            var output = process.StandardOutput.ReadToEnd();

            if (!process.WaitForExit((int)NetstatTimeout.TotalMilliseconds))
                return "netstat did not finish in time";

            var match = ListeningOnPort80.Match(output);

            if (!match.Success)
                return "no TCP listener on port 80 was reported by netstat";

            var pid = int.Parse(match.Groups["pid"].Value);

            return $"pid {pid} ({ProcessName(pid)}) is listening on TCP port 80";
        }
        catch (Exception ex)
        {
            return $"port 80 holder could not be determined ({ex.GetType().Name}: {ex.Message})";
        }
    }

    private static string ProcessName(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);

            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return "process already gone";
        }
    }
}
