using CliWrap;
using CliWrap.Buffered;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    public class SmtpServiceConfiguration
    {
    }

    public class SmtpServiceRunningContext
    {
        public int Port { get; }
        public string Host => "localhost:" + Port;
        public Uri WebUIUri { get; }

        public SmtpServiceRunningContext(int port, Uri webUIUri)
        {
            Port = port;
            WebUIUri = webUIUri;
        }
    }

    public sealed class SmtpService : IAsyncDisposable
    {
        private static readonly PortLeaseManager _smtpPortLeaseManager;
        private static readonly PortLeaseManager _webUIPortLeaseManager;
        private static readonly SemaphoreSlim _restoreSemaphore = new(1, 1);

        private readonly SmtpServiceConfiguration _configuration;

        private static bool _wasRestored;

        private int _smtpPort;
        private int _webUIPort;
        private CancellationTokenSource _cancellationTokenSource;

        // Not actually unnecessary.
#pragma warning disable IDE0079 // Remove unnecessary suppression
        [SuppressMessage(
            "Performance",
            "CA1810:Initialize reference type static fields inline",
            Justification = "No GetAgentIndexOrDefault() duplication this way.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
        static SmtpService()
        {
            var agentIndexTimesHundred = TestConfigurationManager.GetAgentIndexOrDefault() * 100;
            _smtpPortLeaseManager = new PortLeaseManager(11000 + agentIndexTimesHundred, 11099 + agentIndexTimesHundred);
            _webUIPortLeaseManager = new PortLeaseManager(12000 + agentIndexTimesHundred, 12099 + agentIndexTimesHundred);
        }

        public SmtpService(SmtpServiceConfiguration configuration) => _configuration = configuration;

        public async Task<SmtpServiceRunningContext> StartAsync()
        {
            // The service depends on the smtp4dev .NET CLI tool (https://github.com/rnwood/smtp4dev) to be installed as
            // a local tool (on local tools see:
            // https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use). The local tool manifest was
            // already created with dotnet new tool-manifest and the tool installed with:
            // dotnet tool install Rnwood.Smtp4dev --version "3.1.0-*"
            var dotnetToolsConfigFilePath = Path.Combine(".config", "dotnet-tools.json");

            if (!File.Exists(dotnetToolsConfigFilePath))
            {
                throw new InvalidOperationException("No .NET CLI local tool manifest file found. Was the .config folder removed?");
            }

            var manifest = JObject.Parse(await File.ReadAllTextAsync(dotnetToolsConfigFilePath));

            var smtp4devConfig = (manifest["tools"] as JObject)?["rnwood.smtp4dev"];
            if (smtp4devConfig == null)
            {
                throw new InvalidOperationException("There was no smtp4dev configuration in the .NET CLI local tool manifest file.");
            }

            _smtpPort = _smtpPortLeaseManager.LeaseAvailableRandomPort();
            _webUIPort = _webUIPortLeaseManager.LeaseAvailableRandomPort();

            try
            {
                await _restoreSemaphore.WaitAsync();

                if (!_wasRestored)
                {
                    // Running dotnet tool restore the first time to make sure smtp4dev is installed.
                    var restoreResult = await Cli
                        .Wrap("dotnet.exe")
                        .WithArguments(a => a.Add("tool").Add("restore"))
                        .ExecuteBufferedAsync();

                    if (restoreResult.ExitCode != 0)
                    {
                        throw new InvalidOperationException(
                            $"The dotnet tool restore command failed with the following output: {restoreResult.StandardError}");
                    }

                    _wasRestored = true;
                }
            }
            finally
            {
                _restoreSemaphore.Release();
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var webUIUri = new Uri("http://localhost:" + _webUIPort);

            // Starting smtp4dev with a command like this:
            // dotnet.exe tool run smtp4dev --db "" --smtpport 11308 --urls http://localhost:12360/
            // For the possible command line arguments see:
            // https://github.com/rnwood/smtp4dev/blob/master/Rnwood.Smtp4dev/Program.cs#L132.
            await Cli
                .Wrap("dotnet.exe")
                .WithArguments(a => a
                    .Add("tool").Add("run").Add("smtp4dev")
                    // An empty db parameter means an in-memory DB.
                    .Add("--db").Add(string.Empty)
                    .Add("--smtpport").Add(_smtpPort, CultureInfo.InvariantCulture)
                    .Add("--urls").Add(webUIUri.ToString()))
                .ExecuteDotNetApplicationAsync(
                    stdErr =>
                        throw new IOException(
                            $"The smtp4dev service didn't start properly on SMTP port {_smtpPort} and web UI port " +
                                $"{_webUIPort} due to the following error: " +
                            Environment.NewLine +
                            stdErr.Text),
                    _cancellationTokenSource.Token);

            return new SmtpServiceRunningContext(_smtpPort, webUIUri);
        }

        public ValueTask DisposeAsync()
        {
            _smtpPortLeaseManager.StopLease(_smtpPort);
            _webUIPortLeaseManager.StopLease(_webUIPort);

            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            return new ValueTask(Task.CompletedTask);
        }
    }
}
