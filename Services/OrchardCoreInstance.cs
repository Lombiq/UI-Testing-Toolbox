using CliWrap;
using CliWrap.Builders;
using CliWrap.EventStream;
using Lombiq.Tests.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;

namespace Lombiq.Tests.UI.Services
{
    public delegate void BeforeAppStartHandler(string contentRootPath, ArgumentsBuilder argumentsBuilder);

    public class OrchardCoreConfiguration
    {
        public string AppAssemblyPath { get; set; }
        public string SnapshotDirectoryPath { get; set; }
        public BeforeAppStartHandler BeforeAppStart { get; set; }
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


        [SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Code is much nicer this way.")]
        static OrchardCoreInstance()
        {
            var agentIndexTimesHundred = TestConfigurationManager.GetAgentIndexOrDefault() * 100;
            _portLeaseManager = new PortLeaseManager(6000 + agentIndexTimesHundred, 6099 + agentIndexTimesHundred);
        }

        public OrchardCoreInstance(OrchardCoreConfiguration configuration, ITestOutputHelper testOutputHelper)
        {
            _configuration = configuration;
            _testOutputHelper = testOutputHelper;
        }


        public async Task<Uri> StartUp()
        {
            _port = _portLeaseManager.LeaseAvailableRandomPort();
            var url = UrlPrefix + _port;

            _testOutputHelper.WriteLine("Generated URL for Orchard Core instance is \"{0}\".", url);

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

            _command = Cli.Wrap("dotnet.exe")
                .WithArguments(argumentsBuilder =>
                {
                    var builder = argumentsBuilder
                        .Add(_configuration.AppAssemblyPath)
                        .Add("--urls").Add(url)
                        .Add("--contentRoot").Add(_contentRootPath)
                        .Add("--webroot=").Add(Path.Combine(_contentRootPath, "wwwroot"))
                        .Add("--environment").Add("Development");

                    if (_configuration.BeforeAppStart != null)
                    {
                        _configuration.BeforeAppStart.Invoke(_contentRootPath, builder);
                    }
                });

            await StartOrchardApp();

            return new Uri(url);
        }

        public Task Pause() => StopOrchardApp();

        public Task Resume() => StartOrchardApp();

        public async Task TakeSnapshot(string snapshotDirectoryPath)
        {
            await Pause();

            if (Directory.Exists(snapshotDirectoryPath)) Directory.Delete(snapshotDirectoryPath, true);

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
                        ContentLoader = () => File.ReadAllTextAsync(filePath)
                    }) :
                Enumerable.Empty<IApplicationLog>();
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed) return;

            _isDisposed = true;

            await StopOrchardApp();

            _portLeaseManager.StopLease(_port);

            DirectoryHelper.SafelyDeleteDirectoryIfExists(_contentRootPath);
        }


        private void CreateContentRootFolder()
        {
            _contentRootPath = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString());
            Directory.CreateDirectory(_contentRootPath);
        }

        private async Task StartOrchardApp()
        {
            if (_command == null)
            {
                throw new InvalidOperationException("The app needs to be started up first.");
            }

            if (_cancellationTokenSource != null) return;

            _cancellationTokenSource = new CancellationTokenSource();

            var enumerator = _command
                .ListenAsync(_cancellationTokenSource.Token).GetAsyncEnumerator();

            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    var commandEvent = enumerator.Current;

                    switch (commandEvent)
                    {
                        case StandardOutputCommandEvent stdOut:
                            if (stdOut.Text.Contains("Application started. Press Ctrl+C to shut down.")) return;
                            break;
                        case StandardErrorCommandEvent stdErr:
                            throw new IOException(
                                "Starting the Orchard Core application via dotnet.exe failed with the following output:" +
                                Environment.NewLine +
                                stdErr.Text +
                                (stdErr.Text.Contains("Failed to bind to address", StringComparison.OrdinalIgnoreCase) ?
                                    " This can happen when there are leftover dotnet.exe processes after an aborted test run or some other app is listening on the same port too." :
                                    string.Empty));
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        private Task StopOrchardApp()
        {
            if (_cancellationTokenSource == null) return Task.CompletedTask;

            // Cancellation is the only way to stop the process, won't be able to send a Ctrl+C, see:
            // https://github.com/Tyrrrz/CliWrap/issues/47
            if (!_cancellationTokenSource.IsCancellationRequested) _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;

            return Task.CompletedTask;
        }


        private class ApplicationLog : IApplicationLog
        {
            public string Name { get; set; }
            public Func<Task<string>> ContentLoader { get; set; }

            public Task<string> GetContent() => ContentLoader();
        }
    }
}
