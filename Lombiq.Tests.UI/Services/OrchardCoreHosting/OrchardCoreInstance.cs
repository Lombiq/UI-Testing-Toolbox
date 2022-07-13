using CliWrap;
using CliWrap.Builders;
using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Helpers;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

public delegate Task BeforeAppStartHandler(string contentRootPath, ArgumentsBuilder argumentsBuilder);

public delegate Task BeforeTakeSnapshotHandler(string contentRootPath, string snapshotDirectoryPath);

public class OrchardCoreConfiguration
{
    public string AppAssemblyPath { get; set; }
    public string SnapshotDirectoryPath { get; set; }
    public BeforeAppStartHandler BeforeAppStart { get; set; }
    public BeforeTakeSnapshotHandler BeforeTakeSnapshot { get; set; }

    /// <summary>
    /// Adds a command line argument to the app during <see cref="BeforeAppStart"/> that switches AI into offline mode.
    /// This way it won't try to reach out to a remote server with telemetry and the test remains self-sufficient.
    /// </summary>
    public void EnableApplicationInsightsOfflineOperation() =>
        BeforeAppStart +=
            (_, argumentsBuilder) =>
            {
                argumentsBuilder
                    .Add("--OrchardCore:Lombiq_Hosting_Azure_ApplicationInsights:EnableOfflineOperation")
                    .Add("true");

                return Task.CompletedTask;
            };
}

/// <summary>
/// A locally executing Orchard Core application.
/// </summary>
public sealed class OrchardCoreInstance : IWebApplicationInstance
{
    // Using an HTTPS URL so it's the same as in the actual app.
    private const string UrlPrefix = "https://localhost:";

    private static readonly PortLeaseManager _portLeaseManager;
    private static readonly ConcurrentDictionary<string, string> _exeCopyMarkers = new();
    private static readonly object _exeCopyLock = new();
    private static readonly string _executableExtension = OperatingSystem.IsWindows() ? ".exe" : string.Empty;

    private readonly OrchardCoreConfiguration _configuration;
    private readonly string _contextId;
    private readonly ITestOutputHelper _testOutputHelper;
    private Command _command;
    private CancellationTokenSource _cancellationTokenSource;
    private int _port;
    private string _contentRootPath;
    private bool _isDisposed;

    // Not actually unnecessary.
#pragma warning disable IDE0079 // Remove unnecessary suppression
    [SuppressMessage(
        "Performance",
        "CA1810:Initialize reference type static fields inline",
        Justification = "No GetAgentIndexOrDefault() duplication this way.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
    static OrchardCoreInstance()
    {
        var agentIndexTimesHundred = TestConfigurationManager.GetAgentIndexOrDefault() * 100;
        _portLeaseManager = new PortLeaseManager(9000 + agentIndexTimesHundred, 9099 + agentIndexTimesHundred);
    }

    public OrchardCoreInstance(OrchardCoreConfiguration configuration, string contextId, ITestOutputHelper testOutputHelper)
    {
        _configuration = configuration;
        _contextId = contextId;
        _testOutputHelper = testOutputHelper;
    }

    public async Task<Uri> StartUpAsync()
    {
        _port = _portLeaseManager.LeaseAvailableRandomPort();
        var url = UrlPrefix + _port.ToTechnicalString();

        _testOutputHelper.WriteLineTimestampedAndDebug("The generated URL for the Orchard Core instance is \"{0}\".", url);

        CreateContentRootFolder();

        if (!string.IsNullOrEmpty(_configuration.SnapshotDirectoryPath) && Directory.Exists(_configuration.SnapshotDirectoryPath))
        {
            FileSystem.CopyDirectory(_configuration.SnapshotDirectoryPath, _contentRootPath, overwrite: true);
        }
        else
        {
            // Copying the config files from the assembly path, i.e. the build output path so only those are included
            // that actually matter.
            OrchardCoreDirectoryHelper
                .CopyAppConfigFiles(Path.GetDirectoryName(_configuration.AppAssemblyPath), _contentRootPath);
        }

        // If you try to use the dotnet command to run a dll published for a different platform (seen this with running
        // win10-x86 dll files on an x64 Windows machine) then you'll get a "Failed to load the dll from hostpolicy.dll
        // HRESULT: 0x800700C1" error even if the exe will run without issues. So, if an exe exists, we'll run that.
        var exePath = _configuration.AppAssemblyPath.ReplaceOrdinalIgnoreCase(".dll", _executableExtension);
        var useExeToExecuteApp = File.Exists(exePath);

        // Running randomly named executables will make it harder to kill leftover processes in the event of an
        // interrupted test execution. So using a unified name pattern for such executables.
        if (useExeToExecuteApp)
        {
            exePath = _exeCopyMarkers.GetOrAdd(
                exePath,
                exePathKey =>
                {
                    // Using a lock because ConcurrentDictionary doesn't guarantee that two value factories won't run
                    // for the same key.
                    lock (_exeCopyLock)
                    {
                        var copyExePath = Path.Combine(
                            Path.GetDirectoryName(exePathKey) ?? throw new InvalidOperationException(
                                $"Unable to find the directory of \"{exePathKey}\"."),
                            "Lombiq.UITestingToolbox.AppUnderTest." + Path.GetFileName(exePathKey));

                        if (File.Exists(copyExePath) &&
                            File.GetLastWriteTimeUtc(copyExePath) < File.GetLastWriteTimeUtc(exePathKey))
                        {
                            File.Delete(copyExePath);
                        }

                        if (!File.Exists(copyExePath)) File.Copy(exePathKey, copyExePath);

                        return copyExePath;
                    }
                });
        }

        var argumentsBuilder = new ArgumentsBuilder()
            .Add("--urls").Add(url)
            .Add("--contentRoot").Add(_contentRootPath)
            .Add("--webroot").Add(Path.Combine(_contentRootPath, "wwwroot"))
            .Add("--environment").Add("Development")
            // This logging provider is a hard requirement, because we identify when the web server has started by the
            // information log with the message "Application started. Press Ctrl+C to shut down.". The MS Docs says
            // (https://docs.microsoft.com/en-us/aspnet/core/migration/50-to-60?view=aspnetcore-6.0&tabs=visual-studio#new-hosting-model)
            // that you don't need the Microsoft.Hosting.Lifetime provider so some consumers may remove it during the
            // .NET 6 migration, which would cause Lombiq.Tests.UI to wait indefinitely.
            .Add("--Logging:LogLevel:Microsoft.Hosting.Lifetime").Add("Information");

        if (!useExeToExecuteApp) argumentsBuilder = argumentsBuilder.Add(_configuration.AppAssemblyPath);

        await _configuration.BeforeAppStart
            .InvokeAsync<BeforeAppStartHandler>(handler => handler(_contentRootPath, argumentsBuilder));

        _command = Cli.Wrap(useExeToExecuteApp ? exePath : ("dotnet" + _executableExtension))
            .WithArguments(argumentsBuilder.Build());

        _testOutputHelper.WriteLineTimestampedAndDebug(
            "The Orchard Core instance will be launched with the following command: \"{0}\".", _command);

        await StartOrchardAppAsync();

        return new Uri(url);
    }

    public Task PauseAsync() => StopOrchardAppAsync();

    public Task ResumeAsync() => StartOrchardAppAsync();

    public async Task TakeSnapshotAsync(string snapshotDirectoryPath)
    {
        ArgumentNullException.ThrowIfNull(snapshotDirectoryPath);

        await PauseAsync();

        if (Directory.Exists(snapshotDirectoryPath)) Directory.Delete(snapshotDirectoryPath, recursive: true);

        Directory.CreateDirectory(snapshotDirectoryPath);

        await _configuration.BeforeTakeSnapshot
            .InvokeAsync<BeforeTakeSnapshotHandler>(handler => handler(_contentRootPath, snapshotDirectoryPath));

        FileSystem.CopyDirectory(_contentRootPath, snapshotDirectoryPath, overwrite: true);
    }

    public IEnumerable<IApplicationLog> GetLogs(CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default) cancellationToken = CancellationToken.None;

        var logFolderPath = Path.Combine(_contentRootPath, "App_Data", "logs");
        return Directory.Exists(logFolderPath)
            ? Directory
                .EnumerateFiles(logFolderPath, "*.log")
                .Select(filePath => (IApplicationLog)new ApplicationLog
                {
                    Name = Path.GetFileName(filePath),
                    FullName = Path.GetFullPath(filePath),
                    ContentLoader = () => File.ReadAllTextAsync(filePath, cancellationToken),
                })
            : Enumerable.Empty<IApplicationLog>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        await StopOrchardAppAsync();

        _portLeaseManager.StopLease(_port);

        DirectoryHelper.SafelyDeleteDirectoryIfExists(_contentRootPath, 60);
    }

    private void CreateContentRootFolder()
    {
        _contentRootPath = DirectoryPaths.GetTempSubDirectoryPath(_contextId, "App");
        Directory.CreateDirectory(_contentRootPath);
        _testOutputHelper.WriteLineTimestampedAndDebug("Content root path was created: {0}", _contentRootPath);
    }

    private async Task StartOrchardAppAsync()
    {
        _testOutputHelper.WriteLineTimestampedAndDebug("Attempting to start the Orchard Core instance.");

        if (_command == null)
        {
            throw new InvalidOperationException("The app needs to be started up first.");
        }

        if (_cancellationTokenSource != null) return;

        _cancellationTokenSource = new CancellationTokenSource();

        await _command.ExecuteDotNetApplicationAsync(
            stdErr =>
            {
                var dotnet = "dotnet" + _executableExtension;
                var note = stdErr.Text.ContainsOrdinalIgnoreCase("Failed to bind to address")
                    ? " This can happen when there are leftover " + dotnet +
                      " processes after an aborted test run or some other app is listening on the same port too."
                    : string.Empty;

                throw new IOException(
                    StringHelper.Concatenate(
                        $"Starting the Orchard Core application via {dotnet} failed with the following output:",
                        $"{Environment.NewLine}{stdErr.Text}{note}"));
            },
            _cancellationTokenSource.Token);

        _testOutputHelper.WriteLineTimestampedAndDebug("The Orchard Core instance was started.");
    }

    private Task StopOrchardAppAsync()
    {
        if (_cancellationTokenSource == null) return Task.CompletedTask;

        _testOutputHelper.WriteLineTimestampedAndDebug("Attempting to stop the Orchard Core instance.");

        // Cancellation is the only way to stop the process, won't be able to send a Ctrl+C, see:
        // https://github.com/Tyrrrz/CliWrap/issues/47
        if (!_cancellationTokenSource.IsCancellationRequested) _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;

        _testOutputHelper.WriteLineTimestampedAndDebug("The Orchard Core instance was stopped.");

        return Task.CompletedTask;
    }

    private class ApplicationLog : IApplicationLog
    {
        public string Name { get; init; }
        public string FullName { get; init; }
        public Func<Task<string>> ContentLoader { get; init; }

        public Task<string> GetContentAsync() => ContentLoader();

        public void Remove()
        {
            if (File.Exists(FullName)) File.Delete(FullName);
        }
    }
}
