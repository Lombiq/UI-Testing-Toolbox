using CliWrap;
using Lombiq.HelpfulLibraries.Cli;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

public class SmtpServiceConfiguration
{
}

public class SmtpServiceRunningContext
{
    public int Port { get; }
    public string Host => "localhost:" + Port.ToTechnicalString();
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

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        var manifest = JObject.Parse(await File.ReadAllTextAsync(dotnetToolsConfigFilePath, token));

        var smtp4devConfig = (manifest["tools"] as JObject)?["rnwood.smtp4dev"];
        if (smtp4devConfig == null)
        {
            throw new InvalidOperationException("There was no smtp4dev configuration in the .NET CLI local tool manifest file.");
        }

        _smtpPort = _smtpPortLeaseManager.LeaseAvailableRandomPort();
        _webUIPort = _webUIPortLeaseManager.LeaseAvailableRandomPort();

        var webUIPortString = _webUIPort.ToTechnicalString();
        var smtpPortString = _smtpPort.ToTechnicalString();

        var webUIUri = new Uri("http://localhost:" + webUIPortString);

        try
        {
            await _restoreSemaphore.WaitAsync(token);

            if (!_wasRestored)
            {
                // Running dotnet tool restore the first time to make sure smtp4dev is installed.
                await CliProgram.DotNet.CommandAsync(token, "tool", "restore");

                _wasRestored = true;
            }
        }
        finally
        {
            _restoreSemaphore.Release();
        }

        // Starting smtp4dev with a command like this:
        // dotnet tool run smtp4dev --db "" --smtpport 11308 --urls http://localhost:12360/
        // An empty db parameter means an in-memory DB. For all possible command line arguments see:
        // https://github.com/rnwood/smtp4dev/blob/master/Rnwood.Smtp4dev/Program.cs#L132.
        await CliProgram.DotNet.GetCommand(
            new object[] { "tool", "run", "smtp4dev", "--db", string.Empty, "--smtpport", _smtpPort, "--urls", webUIUri })
            .ExecuteDotNetApplicationAsync(
                stdErr =>
                    throw new IOException(
                        $"The smtp4dev service didn't start properly on SMTP port {_smtpPort.ToTechnicalString()} " +
                        $"and web UI port {_webUIPort.ToTechnicalString()} due to the following error: " +
                        Environment.NewLine +
                        stdErr.Text),
                token);

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
