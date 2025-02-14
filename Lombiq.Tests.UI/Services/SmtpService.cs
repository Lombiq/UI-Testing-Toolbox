using CliWrap;
using Lombiq.HelpfulLibraries.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

public class SmtpServiceConfiguration
{
    public SmtpServiceRunningContext Context { get; set; }
}

public class SmtpServiceRunningContext
{
    public int Port { get; }
    public int ImapPort { get; set; }
    public string Host => "localhost";
    public Uri WebUIUri { get; }

    public SmtpServiceRunningContext(int port, int imapPort, Uri webUIUri)
    {
        Port = port;
        ImapPort = imapPort;
        WebUIUri = webUIUri;
    }
}

public sealed class SmtpService : IAsyncDisposable
{
    private static readonly PortLeaseManager _smtpPortLeaseManager;
    private static readonly PortLeaseManager _webUIPortLeaseManager;
    private static readonly PortLeaseManager _imapPortLeaseManager;
    private static readonly SemaphoreSlim _restoreSemaphore = new(1, 1);

    private readonly SmtpServiceConfiguration _configuration;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private static bool _wasRestored;

    private int _smtpPort;
    private int _webUIPort;
    private int _imapPort;
    private bool _isDisposed;

    static SmtpService()
    {
        var agentIndexTimesHundred = TestConfigurationManager.GetAgentIndexOrDefault() * 100;
        _smtpPortLeaseManager = new PortLeaseManager(11000 + agentIndexTimesHundred, 11099 + agentIndexTimesHundred);
        _webUIPortLeaseManager = new PortLeaseManager(12000 + agentIndexTimesHundred, 12099 + agentIndexTimesHundred);
        _imapPortLeaseManager = new PortLeaseManager(16000 + agentIndexTimesHundred, 16099 + agentIndexTimesHundred);
    }

    public SmtpService(SmtpServiceConfiguration configuration) => _configuration = configuration;

    public async Task<SmtpServiceRunningContext> StartAsync()
    {
        // The service depends on the smtp4dev .NET CLI tool (https://github.com/rnwood/smtp4dev) to be installed as a
        // local tool (on local tools see: https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use).
        // The local tool manifest was already created with dotnet new tool-manifest and the tool installed with:
        // dotnet tool install Rnwood.Smtp4dev --version "<version>"
        var dotnetToolsConfigFilePath = Path.Combine(".config", "dotnet-tools.json");

        if (!File.Exists(dotnetToolsConfigFilePath))
        {
            throw new InvalidOperationException("No .NET CLI local tool manifest file found. Was the .config folder removed?");
        }

        var token = _cancellationTokenSource.Token;

        var manifest = JsonNode.Parse(await File.ReadAllTextAsync(dotnetToolsConfigFilePath, token));

        // Verify that an smtp4dev configuration is in place.
        if (manifest?["tools"]?["rnwood.smtp4dev"] == null)
        {
            throw new InvalidOperationException("There was no smtp4dev configuration in the .NET CLI local tool manifest file.");
        }

        _smtpPort = await _smtpPortLeaseManager.LeaseAvailableRandomPortAsync(token);
        _webUIPort = await _webUIPortLeaseManager.LeaseAvailableRandomPortAsync(token);
        _imapPort = await _imapPortLeaseManager.LeaseAvailableRandomPortAsync();

        var webUIPortString = _webUIPort.ToTechnicalString();
        var smtpPortString = _smtpPort.ToTechnicalString();

        var webUIUri = new Uri("http://localhost:" + webUIPortString);

        try
        {
            await _restoreSemaphore.WaitAsync(token);

            if (!_wasRestored)
            {
                // Running dotnet tool restore the first time to make sure smtp4dev is installed.
                await CliProgram.DotNet.ExecuteAsync(token, "tool", "restore");

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
        await CliProgram.DotNet
            .GetCommand("tool", "run", "smtp4dev", "--db", string.Empty, "--smtpport", _smtpPort, "--imapport", _imapPort, "--urls", webUIUri)
            .WithEnvironmentVariables(new Dictionary<string, string>
            {
                ["ServerOptions__DisableMessageSanitisation"] = "true",
            })
            .ExecuteUntilOutputAsync(
                "Now listening on:",
                stdErr =>
                    throw new IOException(
                        $"The smtp4dev service didn't start properly on SMTP port {smtpPortString} and web UI port " +
                        $"{webUIPortString} due to the following error:{Environment.NewLine}{stdErr.Text}"),
                token);

        return new SmtpServiceRunningContext(_smtpPort, _imapPort, webUIUri);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        // This is a clean-up method, no need to forward a CancellationToken.
        await _smtpPortLeaseManager.StopLeaseAsync(_smtpPort, CancellationToken.None);
        await _webUIPortLeaseManager.StopLeaseAsync(_webUIPort, CancellationToken.None);

        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();
    }
}
