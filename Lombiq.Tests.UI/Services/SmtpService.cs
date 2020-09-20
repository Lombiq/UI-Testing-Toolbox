using CliWrap;
using CliWrap.Buffered;
using CliWrap.Builders;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    [SuppressMessage(
        "StyleCop.CSharp.MaintainabilityRules",
        "SA1402:File may only contain a single type",
        Justification = "Here for future use.")]
    public class SmtpServiceConfiguration
    {
    }


    public class SmtpServiceRunningContext
    {
        public int Port { get; }
        public Uri WebUIUri { get; }


        public SmtpServiceRunningContext(int port, Uri webUIUri)
        {
            Port = port;
            WebUIUri = webUIUri;
        }
    }


    public class SmtpService : IAsyncDisposable
    {
        private static readonly PortLeaseManager _smtpPortLeaseManager;
        private static readonly PortLeaseManager _webUIPortLeaseManager;
        private static readonly SemaphoreSlim _restoreSemaphore = new SemaphoreSlim(1, 1);
        private static bool _wasRestored;

        private int _smtpPort;
        private int _webUIPort;
        private CancellationTokenSource _cancellationTokenSource;


        [SuppressMessage(
            "Minor Code Smell",
            "S3963:\"static\" fields should be initialized inline",
            Justification = "No GetAgentIndexOrDefault() duplication this way.")]
        static SmtpService()
        {
            var agentIndexTimesHundred = TestConfigurationManager.GetAgentIndexOrDefault() * 100;
            _smtpPortLeaseManager = new PortLeaseManager(11000 + agentIndexTimesHundred, 11099 + agentIndexTimesHundred);
            _webUIPortLeaseManager = new PortLeaseManager(12000 + agentIndexTimesHundred, 12099 + agentIndexTimesHundred);
        }

        [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Here for future use.")]
        public async Task<SmtpServiceRunningContext> StartAsync(SmtpServiceConfiguration configuration = null)
        {
            // The service depends on the smtp4dev .NET CLI tool (https://github.com/rnwood/smtp4dev) to be installed as
            // a local tool (on local tools see: https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use).
            // The local tool manifest was already created with
            // dotnet new tool-manifest
            // and the tool installed with
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
            // dotnet tool run smtp4dev --db="" --smtpport 26 --urls http://localhost:1234
            // For the possible command line arguments see https://github.com/rnwood/smtp4dev/blob/master/Rnwood.Smtp4dev/Program.cs#L95
            // Although e.g. "urls" is not there.
            await Cli
                .Wrap("dotnet.exe")
                .WithArguments(a =>
                    AddArguments(
                        a,
                        "tool",
                        "run",
                        "smtp4dev",
                        // For the db parameter the equal sign is needed.
                        "--db=",
                        string.Empty,
                        "--smtpport",
                        _smtpPort,
                        "--urls",
                        webUIUri.ToString()))
                .ExecuteDotNetApplicationAsync(
                    stdErr =>
                        throw new IOException(
                            $"The smtp4dev service didn't start properly on SMTP port {_smtpPort} and web UI port {_webUIPort} due to the following error: " +
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

        private static void AddArguments(ArgumentsBuilder builder, params object[] arguments)
        {
            foreach (var argument in arguments)
            {
                builder = argument is IFormattable formattable
                    ? builder.Add(formattable, CultureInfo.InvariantCulture)
                    : builder.Add(argument.ToString());
            }
        }
    }
}
