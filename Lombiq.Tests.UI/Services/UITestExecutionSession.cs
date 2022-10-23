using Atata.HtmlValidation;
using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Microsoft.VisualBasic.FileIO;
using Mono.Unix;
using Newtonsoft.Json.Linq;
using Selenium.Axe;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Lombiq.Tests.UI.Services;

internal sealed class UITestExecutionSession<TEntryPoint> : IAsyncDisposable
    where TEntryPoint : class
{
    private readonly UITestManifest _testManifest;
    private readonly OrchardCoreUITestExecutorConfiguration _configuration;
    private readonly UITestExecutorFailureDumpConfiguration _dumpConfiguration;
    private readonly ITestOutputHelper _testOutputHelper;

    private int _screenshotCount;
    private SynchronizingWebApplicationSnapshotManager _currentSetupSnapshotManager;
    private string _snapshotDirectoryPath;
    private bool _hasSetupOperation;
    private bool _setupSnapshotDirectoryContainsApp;
    private SqlServerManager _sqlServerManager;
    private SmtpService _smtpService;
    private AzureBlobStorageManager _azureBlobStorageManager;
    private IWebApplicationInstance _applicationInstance;
    private UITestContext _context;
    private DockerConfiguration _dockerConfiguration;

    public UITestExecutionSession(UITestManifest testManifest, OrchardCoreUITestExecutorConfiguration configuration)
    {
        _testManifest = testManifest;
        _configuration = configuration;
        _dumpConfiguration = configuration.FailureDumpConfiguration;
        _testOutputHelper = configuration.TestOutputHelper;
    }

    public ValueTask DisposeAsync() => ShutdownAsync();

    public async Task<bool> ExecuteAsync(int retryCount, string dumpRootPath)
    {
        var startTime = DateTime.UtcNow;
        IDictionary<string, IFailureDumpItem> failureDumpContainer = null;

        _testOutputHelper.WriteLineTimestampedAndDebug("Starting execution of {0}.", _testManifest.Name);

        try
        {
            var setupConfiguration = _configuration.SetupConfiguration;
            _hasSetupOperation = setupConfiguration.SetupOperation != null;

            _setupSnapshotDirectoryContainsApp = Directory.Exists(
                Path.Combine(setupConfiguration.SetupSnapshotDirectoryPath, "App_Data"));

            if (_hasSetupOperation)
            {
                var snapshotSubdirectory = "Default";
                if (_configuration.UseSqlServer)
                {
                    snapshotSubdirectory = _configuration.UseAzureBlobStorage
                        ? "SqlServer-AzureBlob"
                        : "SqlServer";
                }
                else if (_configuration.UseAzureBlobStorage)
                {
                    snapshotSubdirectory = "AzureBlob";
                }

                snapshotSubdirectory += "-" + setupConfiguration.SetupOperation!.GetHashCode().ToTechnicalString();

                _snapshotDirectoryPath = Path.Combine(setupConfiguration.SetupSnapshotDirectoryPath, snapshotSubdirectory);

                _configuration.OrchardCoreConfiguration.SnapshotDirectoryPath = _snapshotDirectoryPath;

                _currentSetupSnapshotManager = UITestExecutionSessionsMeta.SetupSnapshotManagers.GetOrAdd(
                    _snapshotDirectoryPath,
                    path => new SynchronizingWebApplicationSnapshotManager(path));

                await SetupAsync();
            }

            // In some cases, there is a temporary setup snapshot directory path but no setup operation. For example,
            // when calling the "ExecuteTestAsync()" method without setup operation.
            else if (_setupSnapshotDirectoryContainsApp)
            {
                _configuration.OrchardCoreConfiguration.SnapshotDirectoryPath = setupConfiguration.SetupSnapshotDirectoryPath;
            }

            _context ??= await CreateContextAsync();

            _context.FailureDumpContainer.Clear();
            _context.CounterDataCollector.Reset();
            _context.CounterDataCollector.AssertCounterData = _configuration.CounterConfiguration.Running.AssertCounterData
                ?? CounterConfiguration.DefaultAssertCounterData(_configuration.CounterConfiguration.Running);
            failureDumpContainer = _context.FailureDumpContainer;

            _context.SetDefaultBrowserSize();

            await _testManifest.TestAsync(_context);

            await _context.AssertLogsAsync();
            _context.CounterDataCollector.AssertCounter();

            return true;
        }
        catch (Exception ex)
        {
            ex = PrepareAndLogException(ex);

            if (ex is SetupFailedFastException) throw;

            await CreateFailureDumpAsync(ex, dumpRootPath, retryCount, failureDumpContainer);

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
            await ShutdownAsync();

            _testOutputHelper.WriteLineTimestampedAndDebug(
                "Finishing execution of {0}, total time: {1}", _testManifest.Name, DateTime.UtcNow - startTime);
        }

        return false;
    }

    private async ValueTask ShutdownAsync()
    {
        if (_configuration.RunAssertLogsOnAllPageChanges)
        {
            _configuration.CustomConfiguration.Remove("LogsAssertionOnPageChangeWasSetUp");
            _configuration.Events.AfterPageChange -= OnAssertLogsAsync;
        }

        if (_applicationInstance != null) await _applicationInstance.DisposeAsync();

        if (_context != null)
        {
            _context.Scope?.Dispose();

            DirectoryHelper.SafelyDeleteDirectoryIfExists(DirectoryPaths.GetTempSubDirectoryPath(_context.Id));

            _context.FailureDumpContainer.Values
                .ForEach(value => value.Dispose());
            _context.FailureDumpContainer.Clear();
        }

        if (_sqlServerManager is not null)
        {
            await _sqlServerManager.DisposeAsync();
        }

        if (_smtpService != null) await _smtpService.DisposeAsync();
        if (_azureBlobStorageManager != null) await _azureBlobStorageManager.DisposeAsync();

        _screenshotCount = 0;

        _context = null;
    }

    private Exception PrepareAndLogException(Exception ex)
    {
        if (ex is AggregateException aggregateException)
        {
            if (aggregateException.InnerExceptions.Count > 1)
            {
                throw new InvalidOperationException(
                    "More than one exceptions in the AggregateException. This shouldn't really happen.");
            }

            ex = aggregateException.InnerException;
        }

        if (ex is PageChangeAssertionException pageChangeAssertionException)
        {
            _testOutputHelper.WriteLineTimestampedAndDebug(pageChangeAssertionException.Message);
            ex = pageChangeAssertionException.InnerException;
        }
        else if (_context?.Driver is not null)
        {
            _testOutputHelper.WriteLineTimestampedAndDebug(
                $"An exception has occurred while interacting with the page {_context?.GetPageTitleAndAddress()}.");
        }

        _testOutputHelper.WriteLineTimestampedAndDebug($"The test failed with the following exception: {ex}");

        return ex;
    }

    private async Task CreateFailureDumpAsync(
        Exception ex,
        string dumpRootPath,
        int retryCount,
        IDictionary<string, IFailureDumpItem> failureDumpContainer)
    {
        var dumpContainerPath = Path.Combine(dumpRootPath, $"Attempt {retryCount.ToTechnicalString()}");
        var debugInformationPath = Path.Combine(dumpContainerPath, "DebugInformation");

        try
        {
            Directory.CreateDirectory(dumpContainerPath);
            Directory.CreateDirectory(debugInformationPath);

            await File.WriteAllTextAsync(Path.Combine(dumpRootPath, "TestName.txt"), _testManifest.Name);

            if (_context == null) return;

            // Saving the failure screenshot and HTML output should be as early after the test fail as possible so they
            // show an accurate state. Otherwise, e.g. the UI can change, resources can load in the meantime.
            if (_dumpConfiguration.CaptureScreenshots) await CreateScreenshotsDumpAsync(debugInformationPath);

            if (_dumpConfiguration.CaptureHtmlSource)
            {
                var htmlPath = Path.Combine(debugInformationPath, "PageSource.html");
                await File.WriteAllTextAsync(htmlPath, _context.Scope.Driver.PageSource);

                if (_configuration.ReportTeamCityMetadata)
                {
                    TeamCityMetadataReporter.ReportArtifactLink(_testManifest, "PageSource", htmlPath);
                }
            }

            if (_dumpConfiguration.CaptureBrowserLog)
            {
                var browserLogPath = Path.Combine(debugInformationPath, "BrowserLog.log");

                await File.WriteAllLinesAsync(
                    browserLogPath,
                    (await _context.UpdateHistoricBrowserLogAsync()).Select(message => message.ToString()));

                if (_configuration.ReportTeamCityMetadata)
                {
                    TeamCityMetadataReporter.ReportArtifactLink(_testManifest, "BrowserLog", browserLogPath);
                }
            }

            if (_dumpConfiguration.CaptureAppSnapshot) await CaptureAppSnapshotAsync(dumpContainerPath);

            CaptureMarkupValidationResults(ex, debugInformationPath);

            if (failureDumpContainer != null)
            {
                foreach (var toDump in failureDumpContainer)
                {
                    await SaveFailureDumpFromContextAsync(debugInformationPath, toDump.Key, toDump.Value);
                }
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

    private async Task SaveFailureDumpFromContextAsync(
        string debugInformationPath,
        string dumpRelativePath,
        IFailureDumpItem item)
    {
        try
        {
            using var dumpStream = await item.GetStreamAsync();
            string filePath = Path.Combine(debugInformationPath, dumpRelativePath);
            FileSystemHelper.EnsureDirectoryExists(Path.GetDirectoryName(filePath));

            using var dumpFile = File.Open(
                filePath,
                FileMode.Create,
                FileAccess.Write);
            await dumpStream.CopyToAsync(dumpFile);
        }
        catch (Exception dumpException)
        {
            _testOutputHelper.WriteLineTimestampedAndDebug(
                $"Saving dump({dumpRelativePath}) of the test from context failed with the following exception: {dumpException}");
        }
    }

    private async Task SaveTestOutputAsync(string debugInformationPath)
    {
        try
        {
            if (_testOutputHelper is TestOutputHelper concreteTestOutputHelper)
            {
                // While this depends on the directory creation in the above try block it needs to come after the catch
                // otherwise the message saved there wouldn't be included.

                var testOutputPath = Path.Combine(debugInformationPath, "TestOutput.log");
                await File.WriteAllTextAsync(testOutputPath, concreteTestOutputHelper.Output);

                if (_configuration.ReportTeamCityMetadata)
                {
                    TeamCityMetadataReporter.ReportArtifactLink(_testManifest, "TestOutput", testOutputPath);
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
                var containerName = _dockerConfiguration?.ContainerName;
                var remotePath = string.IsNullOrEmpty(containerName)
                    ? appDumpPath
                    : _dockerConfiguration.ContainerSnapshotPath;

                await _sqlServerManager.TakeSnapshotAsync(
                    remotePath,
                    appDumpPath,
                    containerName,
                    useCompressionIfAvailable: true);
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

    private void CaptureMarkupValidationResults(Exception ex, string debugInformationPath)
    {
        // Saving the accessibility and HTML validation reports to files should happen here and can't earlier since at
        // that point there's no FailureDumps folder yet.

        if (ex is AccessibilityAssertionException accessibilityAssertionException
            && _configuration.AccessibilityCheckingConfiguration.CreateReportOnFailure)
        {
            var accessbilityReportPath = Path.Combine(debugInformationPath, "AccessibilityReport.html");
            _context.Driver.CreateAxeHtmlReport(accessibilityAssertionException.AxeResult, accessbilityReportPath);

            if (_configuration.ReportTeamCityMetadata)
            {
                TeamCityMetadataReporter.ReportArtifactLink(_testManifest, "AccessibilityReport", accessbilityReportPath);
            }
        }

        if (ex is HtmlValidationAssertionException htmlValidationAssertionException
            && _configuration.HtmlValidationConfiguration.CreateReportOnFailure)
        {
            var resultFilePath = htmlValidationAssertionException.HtmlValidationResult.ResultFilePath;
            if (!string.IsNullOrEmpty(resultFilePath))
            {
                var htmlValidationReportPath = Path.Combine(debugInformationPath, "HtmlValidationReport.txt");
                File.Move(resultFilePath, htmlValidationReportPath);

                if (_configuration.ReportTeamCityMetadata)
                {
                    TeamCityMetadataReporter.ReportArtifactLink(_testManifest, "HtmlValidationReport", htmlValidationReportPath);
                }
            }
            else
            {
                _testOutputHelper.WriteLineTimestampedAndDebug(
                    "While it was configured to create an HTML validation report on validation failure, there was " +
                    $"no report generated due to {nameof(HtmlValidationOptions)}.{nameof(HtmlValidationOptions.SaveResultToFile)} " +
                    "being false.");
            }
        }
    }

    private async Task SetupAsync()
    {
        var setupConfiguration = _configuration.SetupConfiguration;

        try
        {
            _testOutputHelper.WriteLineTimestampedAndDebug("Starting waiting for the setup operation.");

            _dockerConfiguration = TestConfigurationManager.GetConfiguration<DockerConfiguration>();

            var resultUri = await _currentSetupSnapshotManager.RunOperationAndSnapshotIfNewAsync(async () =>
            {
                _testOutputHelper.WriteLineTimestampedAndDebug("Starting setup operation.");

                await setupConfiguration.BeforeSetup.InvokeAsync<BeforeSetupHandler>(handler => handler(_configuration));

                if (setupConfiguration.FastFailSetup &&
                    UITestExecutionSessionsMeta.SetupOperationFailureCount.TryGetValue(GetSetupHashCode(), out var failure) &&
                    failure.FailureCount > _configuration.MaxRetryCount)
                {
                    throw new SetupFailedFastException(failure.FailureCount, failure.LatestException);
                }

                // Note that the context creation needs to be done here too because the Orchard app needs the snapshot
                // config to be available at startup too.
                _context = await CreateContextAsync();
                _context.CounterDataCollector.AssertCounterData = _configuration.CounterConfiguration.Setup.AssertCounterData
                    ?? CounterConfiguration.DefaultAssertCounterData(_configuration.CounterConfiguration.Setup);

                SetupSqlServerSnapshot();
                SetupAzureBlobStorageSnapshot();

                _context.SetDefaultBrowserSize();

                var result = (_context, await setupConfiguration.SetupOperation(_context));

                await _context.AssertLogsAsync();
                _context.CounterDataCollector.AssertCounter();
                _testOutputHelper.WriteLineTimestampedAndDebug("Finished setup operation.");

                return result;
            });

            _testOutputHelper.WriteLineTimestampedAndDebug("Finished waiting for the setup operation.");

            // Restart the app even after a fresh setup so all tests run with an app newly started from a snapshot.
            if (_context != null)
            {
                await ShutdownAsync();
                _context = null;
            }

            _context = await CreateContextAsync();

            await _context.GoToRelativeUrlAsync(resultUri.PathAndQuery);
        }
        catch (CounterThresholdException) { throw; }
        catch (Exception ex) when (ex is not SetupFailedFastException)
        {
            if (setupConfiguration.FastFailSetup)
            {
                UITestExecutionSessionsMeta.SetupOperationFailureCount.AddOrUpdate(
                    GetSetupHashCode(),
                    (1, ex),
                    (_, pair) => (pair.FailureCount, ex));
            }

            throw;
        }
    }

    private void SetupSqlServerSnapshot()
    {
        if (!_configuration.UseSqlServer) return;

        // This is only necessary for the setup snapshot.
        Task SqlServerManagerBeforeTakeSnapshotHandlerAsync(string contentRootPath, string snapshotDirectoryPath)
        {
            ArgumentNullException.ThrowIfNull(snapshotDirectoryPath);

            _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot -= SqlServerManagerBeforeTakeSnapshotHandlerAsync;

            var containerName = _dockerConfiguration?.ContainerName;
            var remotePath = snapshotDirectoryPath;

            if (!string.IsNullOrEmpty(containerName))
            {
                remotePath = _dockerConfiguration.ContainerSnapshotPath;

                // Due to the multiuser focus of Unix-like platforms it's very common that Docker will be a different
                // user without access to freshly created directories by the current user. Since this is a subdirectory
                // that third parties can't list without prior knowledge and it only contains freshly created data this
                // is not a security concern.
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (!Directory.Exists(snapshotDirectoryPath)) Directory.CreateDirectory(snapshotDirectoryPath);
                    var unixFileInfo = new UnixFileInfo(snapshotDirectoryPath);
                    unixFileInfo.FileAccessPermissions |= FileAccessPermissions.OtherReadWriteExecute;
                }
            }

            return _sqlServerManager.TakeSnapshotAsync(remotePath, snapshotDirectoryPath, containerName);
        }

        // This is necessary because a simple subtraction wouldn't remove previous instances of the local function.
        // Thus, if anything goes wrong between the below delegate registration and its invocation, it will remain
        // registered and fail on the disposed SqlServerManager during a retry.
        _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot =
            _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot.RemoveAll(SqlServerManagerBeforeTakeSnapshotHandlerAsync);
        _configuration.OrchardCoreConfiguration.BeforeTakeSnapshot += SqlServerManagerBeforeTakeSnapshotHandlerAsync;
    }

    private void SetupAzureBlobStorageSnapshot()
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
        var contextId = Guid.NewGuid().ToString();

        FileSystemHelper.EnsureDirectoryExists(DirectoryPaths.GetTempSubDirectoryPath(contextId));

        SqlServerRunningContext sqlServerContext = null;
        AzureBlobStorageRunningContext azureBlobStorageContext = null;
        SmtpServiceRunningContext smtpContext = null;

        if (_configuration.UseSqlServer) sqlServerContext = await SetUpSqlServerAsync();
        if (_configuration.UseAzureBlobStorage) azureBlobStorageContext = await SetUpAzureBlobStorageAsync();
        if (_configuration.UseSmtpService) smtpContext = await StartSmtpServiceAsync();

        Task UITestingBeforeAppStartHandlerAsync(string contentRootPath, InstanceCommandLineArgumentsBuilder arguments)
        {
            _configuration.OrchardCoreConfiguration.BeforeAppStart -= UITestingBeforeAppStartHandlerAsync;

            arguments.AddWithValue("Lombiq_Tests_UI:IsUITesting", value: true);

            if (_configuration.ShortcutsConfiguration.InjectApplicationInfo)
            {
                arguments.AddWithValue("Lombiq_Tests_UI:InjectApplicationInfo", value: true);
            }

            return Task.CompletedTask;
        }

        _configuration.OrchardCoreConfiguration.BeforeAppStart =
            _configuration.OrchardCoreConfiguration.BeforeAppStart.RemoveAll(UITestingBeforeAppStartHandlerAsync);
        _configuration.OrchardCoreConfiguration.BeforeAppStart += UITestingBeforeAppStartHandlerAsync;

        var counterDataCollector = new CounterDataCollector();

        _applicationInstance = new OrchardCoreInstance<TEntryPoint>(
            _configuration.OrchardCoreConfiguration,
            contextId,
            _testOutputHelper,
            counterDataCollector);
        var uri = await _applicationInstance.StartUpAsync();

        _configuration.SetUpEvents();

        if (_configuration.AccessibilityCheckingConfiguration.RunAccessibilityCheckingAssertionOnAllPageChanges)
        {
            _configuration.SetUpAccessibilityCheckingAssertionOnPageChange();
        }

        if (_configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges)
        {
            _configuration.SetUpHtmlValidationAssertionOnPageChange();
        }

        if (_configuration.RunAssertLogsOnAllPageChanges &&
            _configuration.CustomConfiguration.TryAdd("LogsAssertionOnPageChangeWasSetUp", value: true))
        {
            _configuration.Events.AfterPageChange += OnAssertLogsAsync;
        }

        if (_dumpConfiguration.CaptureScreenshots)
        {
            _configuration.Events.AfterPageChange -= TakeScreenshotIfEnabledAsync;
            _configuration.Events.AfterPageChange += TakeScreenshotIfEnabledAsync;
        }

        var atataScope = AtataFactory.StartAtataScope(_testOutputHelper, uri, _configuration);

        return new UITestContext(
            contextId,
            _testManifest,
            _configuration,
            _applicationInstance,
            atataScope,
            new RunningContextContainer(sqlServerContext, smtpContext, azureBlobStorageContext),
            counterDataCollector);
    }

    private string GetSetupHashCode() =>
        _configuration.SetupConfiguration.SetupOperation.GetHashCode().ToTechnicalString() +
        _configuration.UseSqlServer +
        _configuration.UseAzureBlobStorage;

    private Task OnAssertLogsAsync(UITestContext context) => context.AssertLogsAsync();

    private async Task<SqlServerRunningContext> SetUpSqlServerAsync()
    {
        _sqlServerManager = new SqlServerManager(_configuration.SqlServerDatabaseConfiguration);
        var sqlServerContext = await _sqlServerManager.CreateDatabaseAsync();

        async Task SqlServerManagerBeforeAppStartHandlerAsync(string contentRootPath, InstanceCommandLineArgumentsBuilder arguments)
        {
            _configuration.OrchardCoreConfiguration.BeforeAppStart -= SqlServerManagerBeforeAppStartHandlerAsync;

            if (!_hasSetupOperation || !Directory.Exists(_snapshotDirectoryPath))
            {
                return;
            }

            var containerName = _dockerConfiguration?.ContainerName;
            var containerPath = string.IsNullOrEmpty(containerName)
                ? _snapshotDirectoryPath
                : _dockerConfiguration.ContainerSnapshotPath;

            await _sqlServerManager.RestoreSnapshotAsync(containerPath, _snapshotDirectoryPath, containerName);

            var appSettingsPath = Path.Combine(contentRootPath, "App_Data", "Sites", "Default", "appsettings.json");

            if (!File.Exists(appSettingsPath))
            {
                throw new InvalidOperationException(
                    "The setup snapshot's appsettings.json file wasn't found. This most possibly means that the setup failed.");
            }

            var appSettings = JObject.Parse(await File.ReadAllTextAsync(appSettingsPath));
            appSettings[nameof(sqlServerContext.ConnectionString)] = sqlServerContext.ConnectionString;
            await File.WriteAllTextAsync(appSettingsPath, appSettings.ToString());
        }

        _configuration.OrchardCoreConfiguration.BeforeAppStart =
            _configuration.OrchardCoreConfiguration.BeforeAppStart.RemoveAll(SqlServerManagerBeforeAppStartHandlerAsync);
        _configuration.OrchardCoreConfiguration.BeforeAppStart += SqlServerManagerBeforeAppStartHandlerAsync;

        return sqlServerContext;
    }

    private async Task<AzureBlobStorageRunningContext> SetUpAzureBlobStorageAsync()
    {
        _azureBlobStorageManager = new AzureBlobStorageManager(_configuration.AzureBlobStorageConfiguration);
        var azureBlobStorageContext = await _azureBlobStorageManager.SetupBlobStorageAsync();

        async Task AzureBlobStorageManagerBeforeAppStartHandlerAsync(string contentRootPath, InstanceCommandLineArgumentsBuilder arguments)
        {
            _configuration.OrchardCoreConfiguration.BeforeAppStart -= AzureBlobStorageManagerBeforeAppStartHandlerAsync;

            // These need to be configured directly, since that module reads the configuration directly instead of
            // allowing post-configuration.
            arguments
                .AddWithValue("OrchardCore:OrchardCore_Media_Azure:BasePath", value: azureBlobStorageContext.BasePath)
                .AddWithValue(
                    "OrchardCore:OrchardCore_Media_Azure:ConnectionString",
                    value: _configuration.AzureBlobStorageConfiguration.ConnectionString)
                .AddWithValue(
                    "OrchardCore:OrchardCore_Media_Azure:ContainerName",
                    value: _configuration.AzureBlobStorageConfiguration.ContainerName)
                .AddWithValue("OrchardCore:OrchardCore_Media_Azure:CreateContainer", value: true)
                .AddWithValue("Lombiq_Tests_UI:UseAzureBlobStorage", value: true);

            if (!_hasSetupOperation || !Directory.Exists(_snapshotDirectoryPath)) return;

            await _azureBlobStorageManager.RestoreSnapshotAsync(_snapshotDirectoryPath);
        }

        _configuration.OrchardCoreConfiguration.BeforeAppStart =
            _configuration.OrchardCoreConfiguration.BeforeAppStart.RemoveAll(AzureBlobStorageManagerBeforeAppStartHandlerAsync);
        _configuration.OrchardCoreConfiguration.BeforeAppStart += AzureBlobStorageManagerBeforeAppStartHandlerAsync;

        return azureBlobStorageContext;
    }

    private async Task<SmtpServiceRunningContext> StartSmtpServiceAsync()
    {
        _smtpService = new SmtpService(_configuration.SmtpServiceConfiguration);
        var smtpContext = await _smtpService.StartAsync();

        Task SmtpServiceBeforeAppStartHandlerAsync(string contentRootPath, InstanceCommandLineArgumentsBuilder arguments)
        {
            _configuration.OrchardCoreConfiguration.BeforeAppStart -= SmtpServiceBeforeAppStartHandlerAsync;
            arguments
                .AddWithValue("Lombiq_Tests_UI:SmtpSettings:Port", value: smtpContext.Port)
                .AddWithValue("Lombiq_Tests_UI:SmtpSettings:Host", value: "localhost");
            return Task.CompletedTask;
        }

        _configuration.OrchardCoreConfiguration.BeforeAppStart =
            _configuration.OrchardCoreConfiguration.BeforeAppStart.RemoveAll(SmtpServiceBeforeAppStartHandlerAsync);
        _configuration.OrchardCoreConfiguration.BeforeAppStart += SmtpServiceBeforeAppStartHandlerAsync;

        return smtpContext;
    }

    private Task TakeScreenshotIfEnabledAsync(UITestContext context)
    {
        if (_context == null || !_dumpConfiguration.CaptureScreenshots) return Task.CompletedTask;

        var screnshotsPath = DirectoryPaths.GetScreenshotsDirectoryPath(_context.Id);
        FileSystemHelper.EnsureDirectoryExists(screnshotsPath);

        try
        {
            context
                .TakeScreenshot()
                .SaveAsFile(GetScreenshotPath(screnshotsPath, _screenshotCount));
        }
        catch (FormatException ex) when (ex.Message.Contains("The input is not a valid Base-64 string"))
        {
            // Random "The input is not a valid Base-64 string as it contains a non-base 64 character, more than two
            // padding characters, or an illegal character among the padding characters." exceptions can happen.
            _testOutputHelper.WriteLineTimestampedAndDebug(
                $"Taking the screenshot #{_screenshotCount.ToTechnicalString()} failed with the following exception: {ex}");
        }

        _screenshotCount++;

        return Task.CompletedTask;
    }

    private async Task CreateScreenshotsDumpAsync(string debugInformationPath)
    {
        await TakeScreenshotIfEnabledAsync(_context);

        var screenshotsSourcePath = DirectoryPaths.GetScreenshotsDirectoryPath(_context.Id);
        if (Directory.Exists(screenshotsSourcePath))
        {
            var screenshotsDestinationPath = Path.Combine(debugInformationPath, DirectoryPaths.Screenshots);
            FileSystem.CopyDirectory(screenshotsSourcePath, screenshotsDestinationPath);

            if (_configuration.ReportTeamCityMetadata)
            {
                TeamCityMetadataReporter.ReportImage(
                    _testManifest,
                    "FailureScreenshot",
                    GetScreenshotPath(screenshotsDestinationPath, _screenshotCount - 1));
            }
        }
    }

    private static string GetScreenshotPath(string parentDirectoryPath, int index) =>
        Path.Combine(parentDirectoryPath, index.ToTechnicalString() + ".png");
}

internal static class UITestExecutionSessionsMeta
{
    // We need to have different snapshots based on whether the test uses the defaults, SQL Server and/or Azure Blob.
    public static ConcurrentDictionary<string, SynchronizingWebApplicationSnapshotManager> SetupSnapshotManagers { get; } = new();
    public static ConcurrentDictionary<string, (int FailureCount, Exception LatestException)> SetupOperationFailureCount { get; } = new();
}
