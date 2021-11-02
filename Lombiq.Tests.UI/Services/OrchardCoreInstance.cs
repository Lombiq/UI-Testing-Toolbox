using CliWrap;
using CliWrap.Builders;
using Lombiq.Tests.UI.Helpers;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services
{
    public delegate Task BeforeAppStartHandler(string contentRootPath, ArgumentsBuilder argumentsBuilder);

    public delegate Task BeforeTakeSnapshotHandler(string contentRootPath, string snapshotDirectoryPath);

    public class OrchardCoreConfiguration
    {
        public string AppAssemblyPath { get; set; }
        public string SnapshotDirectoryPath { get; set; }
        public BeforeAppStartHandler BeforeAppStart { get; set; }
        public BeforeTakeSnapshotHandler BeforeTakeSnapshot { get; set; }
    }

    /// <summary>
    /// A locally executing Orchard Core application.
    /// </summary>
    public sealed class OrchardCoreInstance : IWebApplicationInstance
    {
        // Using an HTTPS URL so it's the same as in the actual app.
        private const string UrlPrefix = "https://localhost:";

        private static readonly PortLeaseManager _portLeaseManager;

        private readonly OrchardCoreConfiguration _configuration;
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

        public OrchardCoreInstance(OrchardCoreConfiguration configuration, ITestOutputHelper testOutputHelper)
        {
            _configuration = configuration;
            _testOutputHelper = testOutputHelper;
        }

        public async Task<Uri> StartUpAsync()
        {
            _port = _portLeaseManager.LeaseAvailableRandomPort();
            var url = UrlPrefix + _port;

            _testOutputHelper.WriteLineTimestampedAndDebug("The generated URL for the Orchard Core instance is \"{0}\".", url);

            CreateContentRootFolder();

            if (!string.IsNullOrEmpty(_configuration.SnapshotDirectoryPath) && Directory.Exists(_configuration.SnapshotDirectoryPath))
            {
                FileSystem.CopyDirectory(_configuration.SnapshotDirectoryPath, _contentRootPath, true);
            }
            else
            {
                // Copying the config files from the assembly path, i.e. the build output path so only those are
                // included that actually matter.
                OrchardCoreDirectoryHelper
                    .CopyAppConfigFiles(Path.GetDirectoryName(_configuration.AppAssemblyPath), _contentRootPath);
            }

            // If you try to use dotnet.exe to run a DLL published for a different platform (seen this with running
            // win10-x86 DLLs on an x64 Windows machine) then you'll get a "Failed to load the dll from hostpolicy.dll
            // HRESULT: 0x800700C1" error even if the exe will run without issues. So, if an exe exists, we'll run that.
            var exePath = _configuration.AppAssemblyPath.ReplaceOrdinalIgnoreCase(".dll", ".exe");
            var useExeToExecuteApp = File.Exists(exePath);

            _command = Cli.Wrap(useExeToExecuteApp ? exePath : "dotnet.exe")
                .WithArguments(argumentsBuilder =>
                {
                    var builder = argumentsBuilder
                        .Add("--urls").Add(url)
                        .Add("--contentRoot").Add(_contentRootPath)
                        .Add("--webroot=").Add(Path.Combine(_contentRootPath, "wwwroot"))
                        .Add("--environment").Add("Development");

                    if (!useExeToExecuteApp) builder = builder.Add(_configuration.AppAssemblyPath);

                    // There is no other option here than to wait for the invoked Tasks.
#pragma warning disable AsyncFixer02 // Long-running or blocking operations inside an async method
                    _configuration.BeforeAppStart?.Invoke(_contentRootPath, builder)?.Wait();
#pragma warning restore AsyncFixer02 // Long-running or blocking operations inside an async method
                });

            _testOutputHelper.WriteLineTimestampedAndDebug(
                "The Orchard Core instance will be launched with the following command: \"{0}\".", _command);

            await StartOrchardAppAsync();

            return new Uri(url);
        }

        public Task PauseAsync() => StopOrchardAppAsync();

        public Task ResumeAsync() => StartOrchardAppAsync();

        public async Task TakeSnapshotAsync(string snapshotDirectoryPath)
        {
            await PauseAsync();

            if (Directory.Exists(snapshotDirectoryPath)) Directory.Delete(snapshotDirectoryPath, true);

            Directory.CreateDirectory(snapshotDirectoryPath);

            if (_configuration.BeforeTakeSnapshot != null)
            {
                await _configuration.BeforeTakeSnapshot.Invoke(_contentRootPath, snapshotDirectoryPath);
            }

            FileSystem.CopyDirectory(_contentRootPath, snapshotDirectoryPath, true);
        }

        public IEnumerable<IApplicationLog> GetLogs()
        {
            var logFolderPath = Path.Combine(_contentRootPath, "App_Data", "logs");
            return Directory.Exists(logFolderPath) ?
                Directory
                    .EnumerateFiles(logFolderPath, "*.log")
                    .Select(filePath => (IApplicationLog)new ApplicationLog
                    {
                        Name = Path.GetFileName(filePath),
                        ContentLoader = () => File.ReadAllTextAsync(filePath),
                    }) :
                Enumerable.Empty<IApplicationLog>();
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
            _contentRootPath = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString());
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
                    throw new IOException(
                        "Starting the Orchard Core application via dotnet.exe failed with the following output:" +
                        Environment.NewLine +
                        stdErr.Text +
                        (stdErr.Text.Contains("Failed to bind to address", StringComparison.OrdinalIgnoreCase)
                            ? " This can happen when there are leftover dotnet.exe processes after an aborted test run " +
                                "or some other app is listening on the same port too."
                            : string.Empty)),
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
            public string Name { get; set; }
            public Func<Task<string>> ContentLoader { get; set; }

            public Task<string> GetContentAsync() => ContentLoader();
        }
    }
}
