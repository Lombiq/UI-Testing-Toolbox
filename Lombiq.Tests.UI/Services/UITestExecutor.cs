using CliWrap.Builders;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium.Remote;
using Selenium.Axe;
using System;
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

        private static readonly object _setupSnapshotManangerLock = new object();
        private static SynchronizingWebApplicationSnapshotManager _setupSnapshotManangerInstance;

        private SqlServerManager _sqlServerManager;
        private SmtpService _smtpService;
        private IWebApplicationInstance _applicationInstance;
        private UITestContext _context;
        private BrowserLogMessage[] _browserLogMessages;


        public UITestExecutor(
            UITestManifest testManifest,
            OrchardCoreUITestExecutorConfiguration configuration,
            UITestExecutorFailureDumpConfiguration dumpConfiguration,
            ITestOutputHelper testOutputHelper)
        {
            _testManifest = testManifest;
            _configuration = configuration;
            _dumpConfiguration = dumpConfiguration;
            _testOutputHelper = testOutputHelper;
        }


        public ValueTask DisposeAsync()
        {
            _sqlServerManager?.Dispose();
            _context?.Scope?.Dispose();

            // Only call the truly async part if there is anything to do. Otherwise return default which is the
            // ValueTask equivalent of Task.CompletedTask.
            return _smtpService != null || _applicationInstance != null
                ? DisposeInnerAsync()
                : default;
        }

        private async ValueTask DisposeInnerAsync()
        {
            if (_smtpService != null) await _smtpService.DisposeAsync();
            if (_applicationInstance != null) await _applicationInstance.DisposeAsync();
        }


        private async Task<bool> ExecuteAsync(
            int retryCount,
            DateTime startTime,
            bool runSetupOperation,
            string dumpRootPath)
        {
            try
            {
                if (runSetupOperation) await SetupAsync();

                _context ??= await CreateContextAsync();

                _testManifest.Test(_context);

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

                return true;
            }
            catch (Exception ex)
            {
                _testOutputHelper.WriteLine($"The test failed with the following exception: {ex}");

                var dumpContainerPath = Path.Combine(dumpRootPath, $"Attempt {retryCount}");
                var debugInformationPath = Path.Combine(dumpContainerPath, "DebugInformation");
                await CreateFailureDumpAsync(ex, dumpContainerPath, debugInformationPath);

                try
                {
                    if (_testOutputHelper is TestOutputHelper concreteTestOutputHelper)
                    {
                        await File.WriteAllTextAsync(
                            Path.Combine(debugInformationPath, "TestOutput.log"),
                            concreteTestOutputHelper.Output);
                    }
                }
                catch (Exception testOutputHelperException)
                {
                    _testOutputHelper.WriteLine(
                        $"Saving the contents of the test output failed with the following exception: {testOutputHelperException}");
                }

                if (retryCount == _configuration.MaxRetryCount)
                {
                    var dumpFolderAbsolutePath = Path.Combine(AppContext.BaseDirectory, dumpRootPath);
                    _testOutputHelper.WriteLine(
                        $"The test was attempted {retryCount + 1} time(s) and won't be retried anymore. You can see " +
                        $"more details on why it's failing in the FailureDumps folder: {dumpFolderAbsolutePath}");
                    throw;
                }

                _testOutputHelper.WriteLine(
                    $"The test was attempted {retryCount + 1} time(s). {_configuration.MaxRetryCount - retryCount} more attempt(s) will be made.");
            }
            finally
            {
                DebugHelper.WriteTimestampedLine($"Finishing the execution of {_testManifest.Name}, total time: {DateTime.UtcNow - startTime}.");
            }

            return false;
        }

        private async Task<BrowserLogMessage[]> GetBrowserLogAsync(RemoteWebDriver driver) =>
            _browserLogMessages ??= (await driver.GetAndEmptyBrowserLogAsync()).ToArray();

        private async Task CreateFailureDumpAsync(Exception ex, string dumpContainerPath, string debugInformationPath)
        {
            try
            {
                Directory.CreateDirectory(dumpContainerPath);
                Directory.CreateDirectory(debugInformationPath);

                if (_context == null) return;

                if (_dumpConfiguration.CaptureAppSnapshot) await CaptureAppSnapshotAsync(dumpContainerPath);

                if (_dumpConfiguration.CaptureScreenshot)
                {
                    // Only PNG is supported on .NET Core.
                    _context.Scope.Driver.GetScreenshot()
                        .SaveAsFile(Path.Combine(debugInformationPath, "Screenshot.png"));
                }

                if (_dumpConfiguration.CaptureHtmlSource)
                {
                    await File.WriteAllTextAsync(
                        Path.Combine(debugInformationPath, "PageSource.html"),
                        _context.Scope.Driver.PageSource);
                }

                if (_dumpConfiguration.CaptureBrowserLog)
                {
                    await File.WriteAllLinesAsync(
                        Path.Combine(debugInformationPath, "BrowserLog.log"),
                        (await GetBrowserLogAsync(_context.Scope.Driver)).Select(message => message.ToString()));
                }

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
                _testOutputHelper.WriteLine(
                    $"Creating the failure dump of the test failed with the following exception: {dumpException}");
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
                    _sqlServerManager.TakeSnapshot(appDumpPath, true);
                }
                catch (Exception failureException)
                {
                    _testOutputHelper.WriteLine(
                        $"Taking an SQL Server DB snapshot failed with the following exception: {failureException}");
                }
            }
        }

        private async Task SetupAsync()
        {
            var resultUri = await _setupSnapshotManangerInstance.RunOperationAndSnapshotIfNewAsync(async () =>
            {
                // Note that the context creation needs to be done here too because the Orchard app needs
                // the snapshot config to be available at startup too.
                _context = await CreateContextAsync();

                if (_configuration.UseSqlServer)
                {
                    // This is only necessary for the setup snapshot.
                    void SqlServerManagerBeforeTakeSnapshotHandler(string contentRootPath, string snapshotDirectoryPath)
                    {
                        _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot -= SqlServerManagerBeforeTakeSnapshotHandler;
                        _sqlServerManager.TakeSnapshot(snapshotDirectoryPath);
                    }

                    _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot -= SqlServerManagerBeforeTakeSnapshotHandler;
                    _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot += SqlServerManagerBeforeTakeSnapshotHandler;
                }

                return (_context, _configuration.SetupOperation(_context));
            });

            _context ??= await CreateContextAsync();

            _context.GoToRelativeUrl(resultUri.PathAndQuery);
        }

        private async Task<UITestContext> CreateContextAsync()
        {
            SqlServerRunningContext sqlServerContext = null;

            if (_configuration.UseSqlServer)
            {
                _sqlServerManager = new SqlServerManager(_configuration.SqlServerDatabaseConfiguration);
                sqlServerContext = _sqlServerManager.CreateDatabase();

                void SqlServerManagerBeforeAppStartHandler(string contentRootPath, ArgumentsBuilder argumentsBuilder)
                {
                    _configuration.OrchardCoreConfiguration.BeforeAppStart -= SqlServerManagerBeforeAppStartHandler;

                    var snapshotDirectoryPath = _configuration.OrchardCoreConfiguration.SnapshotDirectoryPath;

                    if (!Directory.Exists(snapshotDirectoryPath)) return;

                    _sqlServerManager.RestoreSnapshot(snapshotDirectoryPath);

                    // This method is not actually async.
#pragma warning disable AsyncFixer02 // Long-running or blocking operations inside an async method
                    var appSettingsPath = Path.Combine(contentRootPath, "App_Data", "Sites", "Default", "appsettings.json");
                    var appSettings = JObject.Parse(File.ReadAllText(appSettingsPath));
                    appSettings["ConnectionString"] = sqlServerContext.ConnectionString;
                    File.WriteAllText(appSettingsPath, appSettings.ToString());
#pragma warning restore AsyncFixer02 // Long-running or blocking operations inside an async method
                }

                _configuration.OrchardCoreConfiguration.BeforeAppStart -= SqlServerManagerBeforeAppStartHandler;
                _configuration.OrchardCoreConfiguration.BeforeAppStart += SqlServerManagerBeforeAppStartHandler;
            }

            SmtpServiceRunningContext smtpContext = null;

            if (_configuration.UseSmtpService)
            {
                _smtpService = new SmtpService(_configuration.SmtpServiceConfiguration);
                smtpContext = await _smtpService.StartAsync();

                void SmtpServiceBeforeAppStartHandler(string contentRootPath, ArgumentsBuilder argumentsBuilder)
                {
                    _configuration.OrchardCoreConfiguration.BeforeAppStart -= SmtpServiceBeforeAppStartHandler;
                    argumentsBuilder.Add("--SmtpPort").Add(smtpContext.Port, CultureInfo.InvariantCulture);
                }

                _configuration.OrchardCoreConfiguration.BeforeAppStart -= SmtpServiceBeforeAppStartHandler;
                _configuration.OrchardCoreConfiguration.BeforeAppStart += SmtpServiceBeforeAppStartHandler;
            }

            _applicationInstance = new OrchardCoreInstance(_configuration.OrchardCoreConfiguration, _testOutputHelper);
            var uri = await _applicationInstance.StartUpAsync();

            var atataScope = AtataFactory.StartAtataScope(
                _testOutputHelper,
                uri,
                _configuration);

            return new UITestContext(_testManifest.Name, _configuration, sqlServerContext, _applicationInstance, atataScope, smtpContext);
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

            // It's nicer to have the argument checks separately. And we don't want to merge all of them into a single
            // big ternary.
#pragma warning disable IDE0046 // Convert to conditional expression
            if (configuration.OrchardCoreConfiguration == null)
#pragma warning restore IDE0046 // Convert to conditional expression
            {
                throw new ArgumentNullException($"{nameof(configuration.OrchardCoreConfiguration)} should be provided.");
            }

            return ExecuteOrchardCoreTestInnerAsync(testManifest, configuration);
        }


        private static async Task ExecuteOrchardCoreTestInnerAsync(UITestManifest testManifest, OrchardCoreUITestExecutorConfiguration configuration)
        {
            var startTime = DateTime.UtcNow;
            DebugHelper.WriteTimestampedLine($"Starting the execution of {testManifest.Name}.");

            configuration.OrchardCoreConfiguration.SnapshotDirectoryPath = configuration.SetupSnapshotPath;
            var runSetupOperation = configuration.SetupOperation != null;

            if (runSetupOperation)
            {
                lock (_setupSnapshotManangerLock)
                {
                    _setupSnapshotManangerInstance ??= new SynchronizingWebApplicationSnapshotManager(configuration.SetupSnapshotPath);
                }
            }

            configuration.AtataConfiguration.TestName = testManifest.Name;

            var dumpConfiguration = configuration.FailureDumpConfiguration;
            var dumpFolderNameBase = testManifest.Name;
            if (dumpConfiguration.UseShortNames && dumpFolderNameBase.Contains('(', StringComparison.Ordinal))
            {
#pragma warning disable S4635 // String offset-based methods should be preferred for finding substrings from offsets
                dumpFolderNameBase = dumpFolderNameBase.Substring(
                    dumpFolderNameBase.Substring(0, dumpFolderNameBase.IndexOf('(', StringComparison.Ordinal)).LastIndexOf('.') + 1);
#pragma warning restore S4635 // String offset-based methods should be preferred for finding substrings from offsets
            }

            var dumpRootPath = Path.Combine(dumpConfiguration.DumpsDirectoryPath, dumpFolderNameBase.MakeFileSystemFriendly());
            DirectoryHelper.SafelyDeleteDirectoryIfExists(dumpRootPath);

            if (configuration.AccessibilityCheckingConfiguration.CreateReportAlways)
            {
                var directoryPath = configuration.AccessibilityCheckingConfiguration.AlwaysCreatedAccessibilityReportsDirectoryPath;
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
            }

            var testOutputHelper = configuration.TestOutputHelper;
            var retryCount = 0;
            while (true)
            {
                await using var instance = new UITestExecutor(testManifest, configuration, dumpConfiguration, testOutputHelper);
                if (await instance.ExecuteAsync(retryCount, startTime, runSetupOperation, dumpRootPath)) return;
                retryCount++;
            }
        }
    }
}
