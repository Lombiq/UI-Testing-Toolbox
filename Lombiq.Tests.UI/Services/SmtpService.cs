using CliWrap;
using Lombiq.HelpfulLibraries.Cli;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
    private static readonly PortLeaseManager _pop3PortLeaseManager;
    private static readonly SemaphoreSlim _restoreSemaphore = new(1, 1);

    private readonly SmtpServiceConfiguration _configuration;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private static bool _wasRestored;

    private int _smtpPort;
    private int _webUIPort;
    private int _imapPort;
    private int _pop3Port;
    private bool _isDisposed;

    static SmtpService()
    {
        var agentIndexTimesHundred = TestConfigurationManager.GetAgentIndexOrDefault() * 100;
        _smtpPortLeaseManager = new PortLeaseManager(11000 + agentIndexTimesHundred, 11099 + agentIndexTimesHundred);
        _webUIPortLeaseManager = new PortLeaseManager(12000 + agentIndexTimesHundred, 12099 + agentIndexTimesHundred);
        _imapPortLeaseManager = new PortLeaseManager(16000 + agentIndexTimesHundred, 16099 + agentIndexTimesHundred);
        _pop3PortLeaseManager = new PortLeaseManager(17000 + agentIndexTimesHundred, 17099 + agentIndexTimesHundred);
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
        _imapPort = await _imapPortLeaseManager.LeaseAvailableRandomPortAsync(token);
        _pop3Port = await _pop3PortLeaseManager.LeaseAvailableRandomPortAsync(token);

        var webUIPortString = _webUIPort.ToTechnicalString();
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

        // The "splash screen" lines smtp4dev outputs to stderr on startup:
        // https://github.com/rnwood/smtp4dev/issues/1996.
        string[] splashScreenStdErrLineStarts = [
            "┌────────────.",
            "|\\          / \\",
            "| \\        /   \\",
            "|  smtp4dev    /",
            "|             /",
            "└────────────'",
            " > For help use argument --help"
            ];

        // Starting smtp4dev with a command similar to this (with more parameters, see below):
        // dotnet tool run smtp4dev --db "" --smtpport 11308 --urls http://localhost:12360/
        // An empty db parameter means an in-memory DB. For all possible command line arguments see:
        // https://github.com/rnwood/smtp4dev/blob/master/Rnwood.Smtp4dev/CommandLineParser.cs.
        //
        // We use PipeTarget.ToDelegate + a TaskCompletionSource instead of ExecuteUntilOutputAsync because returning
        // early from ExecuteUntilOutputAsync's await foreach disposes CliWrap's stdout pipe, breaking smtp4dev's
        // stdout and causing the process to crash or hang before its SMTP/IMAP listeners ever start.
        var startedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var stdOutPipe = PipeTarget.ToDelegate((string line) =>
        {
            if (line.Contains("Now listening on:", StringComparison.OrdinalIgnoreCase))
            {
                startedTcs.TrySetResult(true);
            }
        });

        var stdErrPipe = PipeTarget.ToDelegate((string line) =>
        {
            if (splashScreenStdErrLineStarts.Any(stdErrLineStart => line.StartsWithOrdinal(stdErrLineStart)) ||
                string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            startedTcs.TrySetException(new IOException(
                $"The smtp4dev service didn't start properly on SMTP port {_smtpPort.ToTechnicalString()}, " +
                $"web UI port {webUIPortString}, IMAP port {_imapPort.ToTechnicalString()}, and POP3 port " +
                $"{_pop3Port.ToTechnicalString()} due to the following error:{Environment.NewLine}{line}"));
        });

        // Fire-and-forget: smtp4dev runs until the CancellationToken is canceled in DisposeAsync. Not awaiting keeps
        // the stdout pipe alive so smtp4dev's SMTP/IMAP listeners can start after the HTTP host signals readiness.
        _ = CliProgram.DotNet
            .GetCommand(
                "tool",
                "run",
                "smtp4dev",
                "--db",
                string.Empty,
                "--smtpport",
                _smtpPort,
                "--imapport",
                _imapPort,
                // We don't need POP3 but it can't be disabled: https://github.com/rnwood/smtp4dev/issues/1997.
                "--pop3port",
                _pop3Port,
                "--urls",
                webUIUri)
            .WithEnvironmentVariables(new Dictionary<string, string>
            {
                ["ServerOptions__DisableMessageSanitisation"] = "true",
            })
            .WithStandardOutputPipe(stdOutPipe)
            .WithStandardErrorPipe(stdErrPipe)
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(token);

        await startedTcs.Task.WaitAsync(TimeSpan.FromSeconds(30), token);

        // smtp4dev's HTTP host signals readiness with "Now listening on:", but its SMTP and IMAP listeners may bind
        // their ports asynchronously afterwards. We use MailKit clients to probe these ports so that the probe itself
        // performs a proper protocol handshake (not just a bare TCP connect) and doesn't leave smtp4dev in a bad state.
        await WaitForSmtpPortAsync(_smtpPort, token);
        await WaitForImapPortAsync(_imapPort, token);

        return new SmtpServiceRunningContext(_smtpPort, _imapPort, webUIUri);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        // Cancel the token first to signal smtp4dev to exit, then wait briefly to ensure the process has released
        // its ports before we mark them as available for the next test's smtp4dev instance.
        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();

        await Task.Delay(500, CancellationToken.None);

        // This is a clean-up method, no need to forward a CancellationToken.
        await _smtpPortLeaseManager.StopLeaseAsync(_smtpPort, CancellationToken.None);
        await _webUIPortLeaseManager.StopLeaseAsync(_webUIPort, CancellationToken.None);
    }

    private static async Task WaitForSmtpPortAsync(int port, CancellationToken cancellationToken)
    {
        const int maxAttempts = 120;
        const int delayMilliseconds = 250;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync("localhost", port, useSsl: false, cancellationToken);
                await client.DisconnectAsync(quit: true, cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is SocketException or SmtpCommandException or SmtpProtocolException or IOException)
            {
                if (attempt == maxAttempts - 1)
                {
                    throw new TimeoutException(
                        $"The smtp4dev SMTP port {port.ToTechnicalString()} did not become available within the expected time.");
                }

                await Task.Delay(delayMilliseconds, cancellationToken);
            }
        }
    }

    private static async Task WaitForImapPortAsync(int port, CancellationToken cancellationToken)
    {
        const int maxAttempts = 120;
        const int delayMilliseconds = 250;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var client = new ImapClient();
                await client.ConnectAsync("localhost", port, useSsl: false, cancellationToken);
                await client.DisconnectAsync(quit: true, cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is SocketException or ImapCommandException or ImapProtocolException or IOException)
            {
                if (attempt == maxAttempts - 1)
                {
                    throw new TimeoutException(
                        $"The smtp4dev IMAP port {port.ToTechnicalString()} did not become available within the expected time.");
                }

                await Task.Delay(delayMilliseconds, cancellationToken);
            }
        }
    }
}
