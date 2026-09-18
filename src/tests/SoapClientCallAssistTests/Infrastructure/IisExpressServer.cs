#region U S I N G

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace SoapClientCallAssistTests.Infrastructure
{
    public sealed class IisExpressServer : IDisposable
    {
        private static readonly TimeSpan ReadinessTimeout = TimeSpan.FromSeconds(30);

        private static readonly TimeSpan ReadinessPollInterval = TimeSpan.FromMilliseconds(250);

        private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(10);

        private static readonly TimeSpan PortReleaseTimeout = TimeSpan.FromSeconds(10);

        private const int CapturedOutputLimit = 8000;

        private const int BodySampleLimit = 8192;

        private const int BodySummaryLimit = 200;

        private readonly IReadOnlyList<SoapServiceProbe> _probes;

        private readonly IisExpressHostingProfile _profile;

        private readonly int _port;

        private readonly HttpClient _probeClient;

        private readonly StringBuilder _capturedOutput = new StringBuilder();

        private readonly object _outputLock = new object();

        private Process _process;

        private EventHandler _processExitHandler;

        private ConsoleCancelEventHandler _cancelKeyHandler;

        private int _processId;

        private bool _startedByFixture;

        public IisExpressServer(
            IisExpressHostingProfile profile,
            IReadOnlyList<SoapServiceProbe> probes,
            RemoteCertificateValidationCallback serverCertificateValidation)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (probes == null)
                throw new ArgumentNullException(nameof(probes));

            if (serverCertificateValidation == null)
                throw new ArgumentNullException(nameof(serverCertificateValidation));

            if (probes.Count == 0)
                throw new ArgumentException("At least one readiness probe is required.", nameof(probes));

            if (!probes.Any(probe => probe.VerifiesIdentity))
                throw new ArgumentException(
                    "At least one probe must carry an expected body marker, otherwise nothing tells this " +
                    "service apart from any other listener on the same port.", nameof(probes));

            _profile = profile;
            _probes = probes;
            _port = profile.HttpPort;
            _probeClient = new HttpClient(new SocketsHttpHandler
            {
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = serverCertificateValidation
                }
            })
            {
                Timeout = ProbeTimeout
            };
        }

        public async Task EnsureRunningAsync(Action<string> log)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));

            if (await IsRespondingAsync().ConfigureAwait(false))
            {
                log("Adopted the instance already answering " + DescribeProbes() +
                    " as this service; it was not started here and will be left running.");

                return;
            }

            var executablePath = IisExpressPathResolver.ResolveExecutable();
            var configPath = IisExpressPathResolver.ResolveApplicationHostConfig(_profile, executablePath, log);

            IisExpressPathResolver.EnsureServiceAssemblyBuilt();

            var contentRoot = IisExpressPathResolver.ResolveSiteContentRoot();
            var site = IisExpressSiteResolver.Resolve(configPath, _port, contentRoot, log);

            log("Starting " + IisExpressPathResolver.Quote(executablePath) + " for site " + site +
                " from " + IisExpressPathResolver.Quote(configPath) + ".");

            Start(executablePath, configPath, site);

            try
            {
                await WaitForReadinessAsync(log, site).ConfigureAwait(false);
            }
            catch
            {
                StopProcessTree();

                throw;
            }
        }

        public async Task ShutdownAsync(Action<string> log)
        {
            UnregisterHostShutdownHooks();

            if (!_startedByFixture)
            {
                log("Nothing to stop: the service was already running when the run started.");

                return;
            }

            if (_process == null)
            {
                log("The IIS Express process " + _processId +
                    " this run started was already stopped when startup failed.");

                return;
            }

            var processId = _processId;

            StopProcessTree();

            log("Stopped the IIS Express process " + processId + " that this run started.");

            await WaitForPortReleaseAsync(log).ConfigureAwait(false);
        }

        public void Dispose()
        {
            UnregisterHostShutdownHooks();
            StopProcessTree();
            _probeClient.Dispose();
        }

        private void Start(string executablePath, string configPath, IisExpressSite site)
        {
            var startInfo = new ProcessStartInfo(executablePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory
            };

            startInfo.ArgumentList.Add("/config:" + configPath);
            startInfo.ArgumentList.Add("/siteid:" + site.Id);

            var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (sender, args) => CaptureOutput(args.Data);
            process.ErrorDataReceived += (sender, args) => CaptureOutput(args.Data);

            try
            {
                process.Start();
            }
            catch (Exception exception)
            {
                process.Dispose();

                throw new InvalidOperationException(
                    "Failed to launch " + IisExpressPathResolver.Quote(executablePath) + " for site " + site +
                    ": " + exception.Message, exception);
            }

            _process = process;
            _processId = process.Id;
            _startedByFixture = true;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            RegisterHostShutdownHooks();
        }

        private async Task WaitForReadinessAsync(Action<string> log, IisExpressSite site)
        {
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                var process = _process;
                if (process == null || process.HasExited)
                    throw new InvalidOperationException(
                        "IIS Express exited with code " + (process?.ExitCode.ToString() ?? "unknown") +
                        " after " + FormatElapsed(stopwatch) + " while starting site " + site + "." + Diagnostics());

                if (await IsRespondingAsync().ConfigureAwait(false))
                {
                    log("Service answered " + DescribeProbes() + " after " + FormatElapsed(stopwatch) +
                        " (process " + _processId + ").");

                    return;
                }

                if (stopwatch.Elapsed >= ReadinessTimeout)
                    throw new InvalidOperationException(
                        "IIS Express did not answer " + DescribeProbes() + " with HTTP 200 within " +
                        FormatElapsed(stopwatch) + " (limit " + ReadinessTimeout.TotalSeconds.ToString("F0") +
                        "s) while hosting site " + site + "." + Diagnostics());

                await Task.Delay(ReadinessPollInterval).ConfigureAwait(false);
            }
        }

        private async Task<bool> IsRespondingAsync()
        {
            foreach (var probe in _probes)
            {
                if (!await IsRespondingAsync(probe).ConfigureAwait(false))
                    return false;
            }

            return true;
        }

        private async Task<bool> IsRespondingAsync(SoapServiceProbe probe)
        {
            try
            {
                using (var response = await _probeClient
                    .GetAsync(probe.Uri, HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(false))
                {
                    if (response.StatusCode != probe.ExpectedStatusCode)
                        return false;

                    if (!probe.VerifiesIdentity)
                        return true;

                    var body = await ReadBodySampleAsync(response).ConfigureAwait(false);
                    if (body.Contains(probe.ExpectedBodyMarker, StringComparison.Ordinal))
                        return true;

                    throw BuildForeignResponderException(probe, response, body);
                }
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static async Task<string> ReadBodySampleAsync(HttpResponseMessage response)
        {
            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                var buffer = new byte[BodySampleLimit];
                var total = 0;

                while (total < buffer.Length)
                {
                    var read = await stream
                        .ReadAsync(buffer, total, buffer.Length - total)
                        .ConfigureAwait(false);

                    if (read == 0)
                        break;

                    total += read;
                }

                return Encoding.UTF8.GetString(buffer, 0, total);
            }
        }

        private InvalidOperationException BuildForeignResponderException(
            SoapServiceProbe probe,
            HttpResponseMessage response,
            string body)
        {
            return new InvalidOperationException(
                "Something is already answering " + probe + ", but it is not this repository's SOAP test " +
                "service: the response did not contain " +
                IisExpressPathResolver.Quote(probe.ExpectedBodyMarker) + "." + Environment.NewLine +
                "What answered: HTTP " + (int)response.StatusCode + ", Server " +
                IisExpressPathResolver.Quote(DescribeHeader(response, "Server")) + ", Content-Type " +
                IisExpressPathResolver.Quote(DescribeContentType(response)) + ", body starts " +
                IisExpressPathResolver.Quote(Summarize(body)) + "." + Environment.NewLine +
                "Adopting it would run every test in this assembly against a service nobody in this " +
                "checkout built: a stale iisexpress.exe left behind by a hard-killed run or an earlier " +
                "branch, another clone of this repository, or any catch-all listener will answer here. " +
                "Stop whatever holds port " + _port + " and run again.");
        }

        private string DescribeProbes()
        {
            return string.Join(", ", _probes);
        }

        private void StopProcessTree()
        {
            var process = Interlocked.Exchange(ref _process, null);
            if (process == null)
                return;

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    process.WaitForExit((int)ExitTimeout.TotalMilliseconds);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        private async Task WaitForPortReleaseAsync(Action<string> log)
        {
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                if (!IsPortListening())
                {
                    log("Ports " + _port + " and " + _profile.HttpsPort + " released after " +
                        FormatElapsed(stopwatch) + ".");

                    return;
                }

                if (stopwatch.Elapsed >= PortReleaseTimeout)
                {
                    log("Warning: port " + _port + " or " + _profile.HttpsPort + " is still listening " +
                        FormatElapsed(stopwatch) + " after the IIS Express process was stopped.");

                    return;
                }

                await Task.Delay(ReadinessPollInterval).ConfigureAwait(false);
            }
        }

        private bool IsPortListening()
        {
            try
            {
                return IPGlobalProperties.GetIPGlobalProperties()
                    .GetActiveTcpListeners()
                    .Any(endpoint => endpoint.Port == _port || endpoint.Port == _profile.HttpsPort);
            }
            catch (NetworkInformationException)
            {
                return false;
            }
        }

        private string Diagnostics()
        {
            return " " + DescribePortState() + ReadCapturedOutput();
        }

        private string DescribePortState()
        {
            return IsPortListening()
                ? "Port " + _port + " is currently held by another listener."
                : "Nothing is listening on port " + _port + ".";
        }

        private void CaptureOutput(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            lock (_outputLock)
            {
                if (_capturedOutput.Length >= CapturedOutputLimit)
                    return;

                _capturedOutput.Append(line).Append(Environment.NewLine);
            }
        }

        private string ReadCapturedOutput()
        {
            string output;

            lock (_outputLock)
                output = _capturedOutput.ToString();

            return string.IsNullOrWhiteSpace(output)
                ? string.Empty
                : Environment.NewLine + "IIS Express output:" + Environment.NewLine + output.TrimEnd();
        }

        private void RegisterHostShutdownHooks()
        {
            _processExitHandler = (sender, args) => StopProcessTree();
            _cancelKeyHandler = (sender, args) => StopProcessTree();

            AppDomain.CurrentDomain.ProcessExit += _processExitHandler;
            Console.CancelKeyPress += _cancelKeyHandler;
        }

        private void UnregisterHostShutdownHooks()
        {
            if (_processExitHandler != null)
            {
                AppDomain.CurrentDomain.ProcessExit -= _processExitHandler;
                _processExitHandler = null;
            }

            if (_cancelKeyHandler != null)
            {
                Console.CancelKeyPress -= _cancelKeyHandler;
                _cancelKeyHandler = null;
            }
        }

        private static string DescribeHeader(HttpResponseMessage response, string name)
        {
            return response.Headers.TryGetValues(name, out var values)
                ? string.Join(" ", values)
                : "(absent)";
        }

        private static string DescribeContentType(HttpResponseMessage response)
        {
            return response.Content?.Headers.ContentType?.ToString() ?? "(absent)";
        }

        private static string Summarize(string body)
        {
            var collapsed = string.Join(
                " ",
                body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Trim();

            return collapsed.Length <= BodySummaryLimit
                ? collapsed
                : collapsed.Substring(0, BodySummaryLimit) + "...";
        }

        private static string FormatElapsed(Stopwatch stopwatch)
        {
            return stopwatch.Elapsed.TotalSeconds.ToString("F1") + "s";
        }
    }
}
