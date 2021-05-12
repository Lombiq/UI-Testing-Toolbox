using CliWrap.Builders;
using Lombiq.HelpfulLibraries.Libraries.Utilities;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium.Remote;
using Selenium.Axe;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Lombiq.Tests.UI.Services
{
    public sealed class UITestExecutor : IAsyncDisposable
    {
        private readonly UITestManifest _testManifest;
        private readonly OrchardCoreUITestExecutorConfiguration _configuration;
        private readonly UITestExecutorFailureDumpConfiguration _dumpConfiguration;
        private readonly ITestOutputHelper _testOutputHelper;

        private static readonly object _setupSnapshotManangerLock = new();
        private static readonly ConcurrentDictionary<int, int> _setupOperationFailureCount = new();
        private static SynchronizingWebApplicationSnapshotManager _setupSnapshotManangerInstance;

        private SqlServerManager _sqlServerManager;
        private SmtpService _smtpService;
        private AzureBlobStorageManager _azureBlobStorageManager;
        private IWebApplicationInstance _applicationInstance;
        private UITestContext _context;
        private List<BrowserLogMessage> _browserLogMessages;
        private DockerConfiguration _dockerConfiguration;

        private UITestExecutor(UITestManifest testManifest, OrchardCoreUITestExecutorConfiguration configuration)
        {
            _testManifest = testManifest;
            _configuration = configuration;
            _dumpConfiguration = configuration.FailureDumpConfiguration;
            _testOutputHelper = configuration.TestOutputHelper;
        }

        public ValueTask DisposeAsync() => ShutdownAsync();

        private async ValueTask ShutdownAsync()
        {
            if (_applicationInstance != null) await _applicationInstance.DisposeAsync();

            _sqlServerManager?.Dispose();
            _context?.Scope?.Dispose();

            if (_smtpService != null) await _smtpService.DisposeAsync();
            if (_azureBlobStorageManager != null) await _azureBlobStorageManager.DisposeAsync();
        }

        private async Task<bool> ExecuteAsync(
            int retryCount,
            bool runSetupOperation,
            string dumpRootPath)
        {
            var startTime = DateTime.UtcNow;

            _testOutputHelper.WriteLineTimestampedAndDebug("Starting execution of {0}.", _testManifest.Name);

            try
            {
                if (runSetupOperation) await SetupAsync();

                _context ??= await CreateContextAsync();

                _testManifest.Test(_context);

                await AssertLogsAsync();

                return true;
            }
            catch (Exception ex)
            {
                _testOutputHelper.WriteLineTimestampedAndDebug($"The test failed with the following exception: {ex}");

                if (ex is SetupFailedFastException) throw;

                await CreateFailureDumpAsync(ex, dumpRootPath, retryCount);

                if (retryCount == _configuration.MaxRetryCount)
                {
                    var dumpFolderAbsolutePath = Path.Combine(AppContext.BaseDirectory, dumpRootPath);

                    _testOutputHelper.WriteLineTimestampedAndDebug(
                        "The test was attempted {0} time(s) and won't be retried anymore. You can see more details " +
                            "on why it's failing in the FailureDumps folder: {1}",
                        retryCount + 1,
                        dumpFolderAbsolutePath);

                    throw;
                }

                _testOutputHelper.WriteLineTimestampedAndDebug(
                    "The test was attempted {0} time(s). {1} more attempt(s) will be made after waiting {2}.",
                    retryCount + 1,
                    _configuration.MaxRetryCount - retryCount,
                    _configuration.RetryInterval);

                await Task.Delay(_configuration.RetryInterval);
            }
            finally
            {
                _testOutputHelper.WriteLineTimestampedAndDebug(
                    "Finishing execution of {0}, total time: {1}", _testManifest.Name, DateTime.UtcNow - startTime);
            }

            return false;
        }

        private async Task<List<BrowserLogMessage>> GetBrowserLogAsync(RemoteWebDriver driver)
        {
            if (_browserLogMessages != null) return _browserLogMessages;

            var allMessages = new List<BrowserLogMessage>();

            foreach (var windowHandle in _context.Driver.WindowHandles)
            {
                // Not using the logging SwitchTo() deliberately as this is not part of what the test does.
                _context.Driver.SwitchTo().Window(windowHandle);
                allMessages.AddRange(await driver.GetAndEmptyBrowserLogAsync());
            }

            return _browserLogMessages = allMessages;
        }

        private async Task CreateFailureDumpAsync(Exception ex, string dumpRootPath, int retryCount)
        {
            var dumpContainerPath = Path.Combine(dumpRootPath, $"Attempt {retryCount}");
            var debugInformationPath = Path.Combine(dumpContainerPath, "DebugInformation");

            try
            {
                Directory.CreateDirectory(dumpContainerPath);
                Directory.CreateDirectory(debugInformationPath);

                await File.WriteAllTextAsync(Path.Combine(dumpRootPath, "TestName.txt"), _testManifest.Name);

                if (_context == null) return;

                // Saving the screenshot and HTML output should be as early after the test fail as possible so they show
                // an accurate state. Otherwise, e.g. the UI can change, resources can load in the meantime.
                if (_dumpConfiguration.CaptureScreenshot)
                {
                    // Only PNG is supported on .NET Core.
                    var imagePath = Path.Combine(debugInformationPath, "Screenshot.png");
                    _context.Scope.Driver.GetScreenshot().SaveAsFile(imagePath);

                    if (_configuration.ReportTeamCityMetadata)
                    {
                        TeamCityMetadataReporter.ReportImage(_testManifest.Name, "Screenshot", imagePath);
                    }
                }

                if (_dumpConfiguration.CaptureHtmlSource)
                {
                    var htmlPath = Path.Combine(debugInformationPath, "PageSource.html");
                    await File.WriteAllTextAsync(htmlPath, _context.Scope.Driver.PageSource);

                    if (_configuration.ReportTeamCityMetadata)
                    {
                        TeamCityMetadataReporter.ReportArtifactLink(_testManifest.Name, "PageSource", htmlPath);
                    }
                }

                if (_dumpConfiguration.CaptureBrowserLog)
                {
                    var browserLogPath = Path.Combine(debugInformationPath, "BrowserLog.log");

                    await File.WriteAllLinesAsync(
                        browserLogPath,
                        (await GetBrowserLogAsync(_context.Scope.Driver)).Select(message => message.ToString()));

                    if (_configuration.ReportTeamCityMetadata)
                    {
                        TeamCityMetadataReporter.ReportArtifactLink(_testManifest.Name, "BrowserLog", browserLogPath);
                    }
                }

                if (_dumpConfiguration.CaptureAppSnapshot) await CaptureAppSnapshotAsync(dumpContainerPath);

                if (ex is AccessibilityAssertionException accessibilityAssertionException
                    && _configuration.AccessibilityCheckingConfiguration.CreateReportOnFailure)
                {
                    _context.Driver.CreateAxeHtmlReport(
                        accessibilityAssertionException.AxeResult,
                        Path.Combine(debugInformationPath, "AccessibilityReport.html"));
                }
            }
            catch (Exception dumpException)
            {
                _testOutputHelper.WriteLineTimestampedAndDebug(
                    $"Creating the failure dump of the test failed with the following exception: {dumpException}");
            }
            finally
            {
                await SaveTestOutputAsync(debugInformationPath);
            }
        }

        private async Task SaveTestOutputAsync(string debugInformationPath)
        {
            try
            {
                if (_testOutputHelper is TestOutputHelper concreteTestOutputHelper)
                {
                    // While this depends on the directory creation in the above try block it needs to come after the
                    // catch otherwise the message saved there wouldn't be included.

                    var testOutputPath = Path.Combine(debugInformationPath, "TestOutput.log");
                    await File.WriteAllTextAsync(testOutputPath, concreteTestOutputHelper.Output);

                    if (_configuration.ReportTeamCityMetadata)
                    {
                        TeamCityMetadataReporter.ReportArtifactLink(_testManifest.Name, "TestOutput", testOutputPath);
                    }
                }
            }
            catch (Exception testOutputHelperException)
            {
                _testOutputHelper.WriteLine(
                    $"Saving the contents of the test output failed with the following exception: {testOutputHelperException}");
            }
        }

        private async Task CaptureAppSnapshotAsync(string dumpContainerPath)
        {
            var appDumpPath = Path.Combine(dumpContainerPath, "AppDump");
            await _context.Application.TakeSnapshotAsync(appDumpPath);

            if (_sqlServerManager != null)
            {
                try
                {
                    var remotePath = appDumpPath;
                    if (_dockerConfiguration != null)
                    {
                        appDumpPath = _dockerConfiguration.HostSnapshotPath;
                        remotePath = _dockerConfiguration.ContainerSnapshotPath;
                    }

                    _sqlServerManager.TakeSnapshot(remotePath, appDumpPath, true);
                }
                catch (Exception failureException)
                {
                    _testOutputHelper.WriteLineTimestampedAndDebug(
                        $"Taking an SQL Server DB snapshot failed with the following exception: {failureException}");
                }
            }

            if (_azureBlobStorageManager != null)
            {
                try
                {
                    await _azureBlobStorageManager.TakeSnapshotAsync(appDumpPath);
                }
                catch (Exception failureException)
                {
                    _testOutputHelper.WriteLineTimestampedAndDebug(
                        $"Taking an Azure Blob Storage snapshot failed with the following exception: {failureException}");
                }
            }
        }

        private async Task SetupAsync()
        {
            var setupConfiguration = _configuration.SetupConfiguration;

            try
            {
                _testOutputHelper.WriteLineTimestampedAndDebug("Starting waiting for the setup operation.");

                SetupDocker();

                var resultUri = await _setupSnapshotManangerInstance.RunOperationAndSnapshotIfNewAsync(async () =>
                {
                    _testOutputHelper.WriteLineTimestampedAndDebug("Starting setup operation.");

                    if (Directory.Exists(_dockerConfiguration?.HostSnapshotPath))
                    {
                        Directory.Delete(_dockerConfiguration!.HostSnapshotPath, recursive: true);
                    }

                    if (setupConfiguration.BeforeSetup != null) await setupConfiguration.BeforeSetup.Invoke(_configuration);

                    if (setupConfiguration.FastFailSetup &&
                        _setupOperationFailureCount.TryGetValue(GetSetupHashCode(), out var failureCount) &&
                        failureCount > _configuration.MaxRetryCount)
                    {
                        throw new SetupFailedFastException(failureCount);
                    }

                    // Note that the context creation needs to be done here too because the Orchard app needs the
                    // snapshot config to be available at startup too.
                    _context = await CreateContextAsync();

                    SetupSqlServer();
                    SetupAzureBlobStorage();

                    var result = (_context, setupConfiguration.SetupOperation(_context));
                    await AssertLogsAsync();
                    _testOutputHelper.WriteLineTimestampedAndDebug("Finished setup operation.");
                    return result;
                });

                _testOutputHelper.WriteLineTimestampedAndDebug("Finished waiting for the setup operation.");

                // Restart the app after even a fresh setup so all tests run with an app newly started from a snapshot.
                if (_context != null)
                {
                    await ShutdownAsync();
                    _context = null;
                }

                _context = await CreateContextAsync();

                _context.GoToRelativeUrl(resultUri.PathAndQuery);
            }
            catch (Exception ex) when (ex is not SetupFailedFastException)
            {
                if (setupConfiguration.FastFailSetup)
                {
                    _setupOperationFailureCount.AddOrUpdate(GetSetupHashCode(), 1, (_, value) => value + 1);
                }

                throw;
            }
        }

        private void SetupDocker()
        {
            if (TestConfigurationManager.GetConfiguration<DockerConfiguration>() is not { } docker ||
                string.IsNullOrEmpty(docker.HostSnapshotPath))
            {
                return;
            }

            // We add this subdirectory to ensure the HostSnapshotPath isn't set to the mounted volume's directory
            // itself (which would be logical). Removing the volume directory instantly severs the connection between
            // host and the container so that should be avoided at all costs.
            docker.ContainerSnapshotPath += '/' + Snapshots.DefaultSetupSnapshotPath; // Always a Unix path.
            docker.HostSnapshotPath = Path.Combine(docker.HostSnapshotPath, Snapshots.DefaultSetupSnapshotPath);

            _dockerConfiguration = docker;
        }

        private void SetupSqlServer()
        {
            if (!_configuration.UseSqlServer) return;

            // This is only necessary for the setup snapshot.
            Task SqlServerManagerBeforeTakeSnapshotHandlerAsync(string contentRootPath, string snapshotDirectoryPath)
            {
                _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot -=
                    SqlServerManagerBeforeTakeSnapshotHandlerAsync;

                var remotePath = snapshotDirectoryPath;
                if (_dockerConfiguration != null)
                {
                    snapshotDirectoryPath = _dockerConfiguration.HostSnapshotPath;
                    remotePath = _dockerConfiguration.ContainerSnapshotPath;
                }

                _sqlServerManager.TakeSnapshot(remotePath, snapshotDirectoryPath);
                return Task.CompletedTask;
            }

            // This is necessary because a simple subtraction wouldn't remove previous instances of the
            // local function. Thus if anything goes wrong between the below delegate registration and it
            // being called then it'll remain registered and later during a retry try to run (and fail on
            // the disposed SqlServerManager.
            _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot =
                _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot.RemoveAll(
                    SqlServerManagerBeforeTakeSnapshotHandlerAsync);
            _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot +=
                SqlServerManagerBeforeTakeSnapshotHandlerAsync;
        }

        private void SetupAzureBlobStorage()
        {
            if (!_configuration.UseAzureBlobStorage) return;

            // This is only necessary for the setup snapshot.
            Task AzureBlobStorageManagerBeforeTakeSnapshotHandlerAsync(string contentRootPath, string snapshotDirectoryPath)
            {
                _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot -= AzureBlobStorageManagerBeforeTakeSnapshotHandlerAsync;
                return _azureBlobStorageManager.TakeSnapshotAsync(snapshotDirectoryPath);
            }

            _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot =
                _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot.RemoveAll(AzureBlobStorageManagerBeforeTakeSnapshotHandlerAsync);
            _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot += AzureBlobStorageManagerBeforeTakeSnapshotHandlerAsync;
        }

        private async Task<UITestContext> CreateContextAsync()
        {
            SqlServerRunningContext sqlServerContext = null;
            AzureBlobStorageRunningContext azureBlobStorageContext = null;

            if (_configuration.UseSqlServer)
            {
                _sqlServerManager = new SqlServerManager(_configuration.SqlServerDatabaseConfiguration);
                sqlServerContext = _sqlServerManager.CreateDatabase();

                async Task SqlServerManagerBeforeAppStartHandlerAsync(string contentRootPath, ArgumentsBuilder argumentsBuilder)
                {
                    _configuration.OrchardCoreConfiguration.BeforeAppStart -= SqlServerManagerBeforeAppStartHandlerAsync;

                    var snapshotDirectoryPath =
                        _dockerConfiguration?.ContainerSnapshotPath ??
                        _configuration.OrchardCoreConfiguration.SnapshotDirectoryPath;

                    if (!Directory.Exists(_dockerConfiguration?.HostSnapshotPath ?? snapshotDirectoryPath)) return;

                    _sqlServerManager.RestoreSnapshot(snapshotDirectoryPath);

                    var appSettingsPath = Path.Combine(contentRootPath, "App_Data", "Sites", "Default", "appsettings.json");

                    if (!File.Exists(appSettingsPath))
                    {
                        throw new InvalidOperationException(
                            "The setup snapshot's appsettings.json file wasn't found. This most possibly means that the setup failed.");
                    }

                    var appSettings = JObject.Parse(await File.ReadAllTextAsync(appSettingsPath));
                    appSettings["ConnectionString"] = sqlServerContext.ConnectionString;
                    await File.WriteAllTextAsync(appSettingsPath, appSettings.ToString());
                }

                _configuration.OrchardCoreConfiguration.BeforeAppStart =
                    _configuration.OrchardCoreConfiguration.BeforeAppStart.RemoveAll(SqlServerManagerBeforeAppStartHandlerAsync);
                _configuration.OrchardCoreConfiguration.BeforeAppStart += SqlServerManagerBeforeAppStartHandlerAsync;
            }

            if (_configuration.UseAzureBlobStorage)
            {
                _azureBlobStorageManager = new AzureBlobStorageManager(_configuration.AzureBlobStorageConfiguration);
                azureBlobStorageContext = await _azureBlobStorageManager.SetupBlobStorageAsync();

                async Task AzureBlobStorageManagerBeforeAppStartHandlerAsync(string contentRootPath, ArgumentsBuilder argumentsBuilder)
                {
                    _configuration.OrchardCoreConfiguration.BeforeAppStart -= AzureBlobStorageManagerBeforeAppStartHandlerAsync;

                    var snapshotDirectoryPath = _configuration.OrchardCoreConfiguration.SnapshotDirectoryPath;

                    argumentsBuilder
                        .Add("--Lombiq_Tests_UI:MediaBlobStorageOptions:BasePath")
                        .Add(azureBlobStorageContext.BasePath);
                    argumentsBuilder
                        .Add("--Lombiq_Tests_UI:MediaBlobStorageOptions:ConnectionString")
                        .Add(_configuration.AzureBlobStorageConfiguration.ConnectionString);
                    argumentsBuilder
                        .Add("--Lombiq_Tests_UI:MediaBlobStorageOptions:ContainerName")
                        .Add(_configuration.AzureBlobStorageConfiguration.ContainerName);

                    if (!Directory.Exists(snapshotDirectoryPath)) return;

                    await _azureBlobStorageManager.RestoreSnapshotAsync(snapshotDirectoryPath);
                }

                _configuration.OrchardCoreConfiguration.BeforeAppStart =
                    _configuration.OrchardCoreConfiguration.BeforeAppStart.RemoveAll(AzureBlobStorageManagerBeforeAppStartHandlerAsync);
                _configuration.OrchardCoreConfiguration.BeforeAppStart += AzureBlobStorageManagerBeforeAppStartHandlerAsync;
            }

            SmtpServiceRunningContext smtpContext = null;

            if (_configuration.UseSmtpService)
            {
                _smtpService = new SmtpService(_configuration.SmtpServiceConfiguration);
                smtpContext = await _smtpService.StartAsync();

                Task SmtpServiceBeforeAppStartHandlerAsync(string contentRootPath, ArgumentsBuilder argumentsBuilder)
                {
                    _configuration.OrchardCoreConfiguration.BeforeAppStart -= SmtpServiceBeforeAppStartHandlerAsync;
                    argumentsBuilder.Add("--Lombiq_Tests_UI:SmtpSettings:Port").Add(smtpContext.Port, CultureInfo.InvariantCulture);
                    argumentsBuilder.Add("--Lombiq_Tests_UI:SmtpSettings:Host").Add("localhost");
                    return Task.CompletedTask;
                }

                _configuration.OrchardCoreConfiguration.BeforeAppStart =
                    _configuration.OrchardCoreConfiguration.BeforeAppStart.RemoveAll(SmtpServiceBeforeAppStartHandlerAsync);
                _configuration.OrchardCoreConfiguration.BeforeAppStart += SmtpServiceBeforeAppStartHandlerAsync;
            }

            Task UITestingBeforeAppStartHandlerAsync(string contentRootPath, ArgumentsBuilder argumentsBuilder)
            {
                _configuration.OrchardCoreConfiguration.BeforeAppStart -= UITestingBeforeAppStartHandlerAsync;

                argumentsBuilder.Add("--Lombiq_Tests_UI:IsUITesting").Add("true");

                if (_configuration.ShortcutsConfiguration.InjectApplicationInfo)
                {
                    argumentsBuilder.Add("--Lombiq_Tests_UI:InjectApplicationInfo").Add("true");
                }

                return Task.CompletedTask;
            }

            _configuration.OrchardCoreConfiguration.BeforeAppStart =
                _configuration.OrchardCoreConfiguration.BeforeAppStart.RemoveAll(UITestingBeforeAppStartHandlerAsync);
            _configuration.OrchardCoreConfiguration.BeforeAppStart += UITestingBeforeAppStartHandlerAsync;

            _applicationInstance = new OrchardCoreInstance(_configuration.OrchardCoreConfiguration, _testOutputHelper);
            var uri = await _applicationInstance.StartUpAsync();

            var atataScope = AtataFactory.StartAtataScope(
                _testOutputHelper,
                uri,
                _configuration);

            return new UITestContext(
                _testManifest.Name,
                _configuration,
                sqlServerContext,
                _applicationInstance,
                atataScope,
                smtpContext,
                azureBlobStorageContext);
        }

        private int GetSetupHashCode() => _configuration.SetupConfiguration.SetupOperation.GetHashCode();

        private async Task AssertLogsAsync()
        {
            try
            {
                if (_configuration.AssertAppLogs != null) await _configuration.AssertAppLogs(_context.Application);
            }
            catch (Exception)
            {
                _testOutputHelper.WriteLine("Application logs: " + Environment.NewLine);
                _testOutputHelper.WriteLine(await _context.Application.GetLogOutputAsync());

                throw;
            }

            try
            {
                _configuration.AssertBrowserLog?.Invoke(await GetBrowserLogAsync(_context.Scope.Driver));
            }
            catch (Exception)
            {
                _testOutputHelper.WriteLine("Browser logs: " + Environment.NewLine);
                _testOutputHelper.WriteLine((await GetBrowserLogAsync(_context.Scope.Driver)).ToFormattedString());

                throw;
            }
        }

        /// <summary>
        /// Executes a test on a new Orchard Core web app instance within a newly created Atata scope.
        /// </summary>
        public static Task ExecuteOrchardCoreTestAsync(
            UITestManifest testManifest,
            OrchardCoreUITestExecutorConfiguration configuration)
        {
            if (string.IsNullOrEmpty(testManifest.Name))
            {
                throw new ArgumentException("You need to specify the name of the test.");
            }

            if (configuration.OrchardCoreConfiguration == null)
            {
                throw new ArgumentException($"{nameof(configuration.OrchardCoreConfiguration)} should be provided.");
            }

            return ExecuteOrchardCoreTestInnerAsync(testManifest, configuration);
        }

        private static async Task ExecuteOrchardCoreTestInnerAsync(UITestManifest testManifest, OrchardCoreUITestExecutorConfiguration configuration)
        {
            configuration.TestOutputHelper.WriteLine(
                "NOTE: This log is cumulative for all test execution attempts. If the test fails repeatedly with " +
                "retries then Attempt 0's output will contain only that execution's output, but Attempt 2's will " +
                "contain 0's and 1's too in addition to 2's.");
            configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Starting preparation for {0}.", testManifest.Name);

            var setupConfiguration = configuration.SetupConfiguration;
            configuration.OrchardCoreConfiguration.SnapshotDirectoryPath = setupConfiguration.SetupSnapshotPath;
            var runSetupOperation = setupConfiguration.SetupOperation != null;

            if (runSetupOperation)
            {
                lock (_setupSnapshotManangerLock)
                {
                    _setupSnapshotManangerInstance ??= new SynchronizingWebApplicationSnapshotManager(setupConfiguration.SetupSnapshotPath);
                }
            }

            configuration.AtataConfiguration.TestName = testManifest.Name;

            var dumpRootPath = PrepareDumpFolder(testManifest, configuration);

            if (configuration.AccessibilityCheckingConfiguration.CreateReportAlways)
            {
                var directoryPath = configuration.AccessibilityCheckingConfiguration.AlwaysCreatedAccessibilityReportsDirectoryPath;
                DirectoryHelper.CreateDirectoryIfNotExists(directoryPath);
            }

            configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Finished preparation for {0}.", testManifest.Name);

            var retryCount = 0;
            var passed = false;
            while (!passed)
            {
                try
                {
                    await using var instance = new UITestExecutor(testManifest, configuration);
                    passed = await instance.ExecuteAsync(retryCount, runSetupOperation, dumpRootPath);
                }
                catch (Exception ex) when (retryCount < configuration.MaxRetryCount)
                {
                    configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
                        $"Unhandled exception during text execution: {ex}.");
                }
                finally
                {
                    if (configuration.ReportTeamCityMetadata && (passed || retryCount == configuration.MaxRetryCount))
                    {
                        TeamCityMetadataReporter.ReportInt(testManifest.Name, "TryCount", retryCount + 1);
                    }
                }

                retryCount++;
            }
        }

        private static string PrepareDumpFolder(
            UITestManifest testManifest,
            OrchardCoreUITestExecutorConfiguration configuration)
        {
            var dumpConfiguration = configuration.FailureDumpConfiguration;
            var dumpFolderNameBase = testManifest.Name;
            if (dumpConfiguration.UseShortNames && dumpFolderNameBase.Contains('(', StringComparison.Ordinal))
            {
                // Incorrect suggestion.
#pragma warning disable S4635 // String offset-based methods should be preferred for finding substrings from offsets
                // Incorrect suggestion.
#pragma warning disable S1854 // Unused assignments should be removed
                var dumpFolderNameBeginningIndex =
#pragma warning restore S1854 // Unused assignments should be removed
                    dumpFolderNameBase.Substring(0, dumpFolderNameBase.IndexOf('(', StringComparison.Ordinal)).LastIndexOf('.') + 1;
                dumpFolderNameBase = dumpFolderNameBase[dumpFolderNameBeginningIndex..];
#pragma warning restore S4635 // String offset-based methods should be preferred for finding substrings from offsets
            }

            dumpFolderNameBase = dumpFolderNameBase.MakeFileSystemFriendly();

            var dumpRootPath = Path.Combine(dumpConfiguration.DumpsDirectoryPath, dumpFolderNameBase);

            DirectoryHelper.SafelyDeleteDirectoryIfExists(dumpRootPath);

            // Probe creating the directory. At least on Windows this can still fail with "The filename, directory name,
            // or volume label syntax is incorrect" but not simply due to the presence of specific characters. Maybe
            // both length and characters play a role (a path containing either the same characters or having the same
            // length would work but not both). Playing safe here.

            try
            {
                Directory.CreateDirectory(dumpRootPath);
                DirectoryHelper.SafelyDeleteDirectoryIfExists(dumpRootPath);
            }
            catch (Exception ex) when (
                (ex is IOException &&
                    ex.Message.Contains("The filename, directory name, or volume label syntax is incorrect.", StringComparison.InvariantCultureIgnoreCase))
                || ex is PathTooLongException)
            {
                // The OS doesn't like the path or it's too long. So we shorten it by removing the test parameters which
                // usually make it long.

                // They're not actually unused.
#pragma warning disable S1854 // Unused assignments should be removed
                var openingBracketIndex = dumpFolderNameBase.IndexOf('(', StringComparison.Ordinal);
                var closingBracketIndex = dumpFolderNameBase.LastIndexOf(")", StringComparison.Ordinal);
#pragma warning restore S1854 // Unused assignments should be removed

                // Can't use string.GetHasCode() because that varies between executions.
                var hashedParameters = Sha256Helper
                    .ComputeHash(dumpFolderNameBase[(openingBracketIndex + 1)..(closingBracketIndex + 1)]);

                dumpFolderNameBase =
                    dumpFolderNameBase[0..(openingBracketIndex + 1)] +
                    hashedParameters +
                    dumpFolderNameBase[closingBracketIndex..];

                dumpRootPath = Path.Combine(dumpConfiguration.DumpsDirectoryPath, dumpFolderNameBase);

                DirectoryHelper.SafelyDeleteDirectoryIfExists(dumpRootPath);

                configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
                    "Couldn't create a folder with the same name as the test. A TestName.txt file containing the " +
                        "full name ({0}) will be put into the folder to help troubleshooting if the test fails.",
                    testManifest.Name);
            }

            return dumpRootPath;
        }
    }
}
