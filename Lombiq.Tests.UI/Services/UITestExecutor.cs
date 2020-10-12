using CliWrap.Builders;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using Selenium.Axe;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Lombiq.Tests.UI.Services
{
    public static class UITestExecutor
    {
        private static readonly object _setupSnapshotManangerLock = new object();
        private static SynchronizingWebApplicationSnapshotManager _setupSnapshotManangerInstance;


        /// <summary>
        /// Executes a test on a new Orchard Core web app instance within a newly created Atata scope.
        /// </summary>
        public static Task ExecuteOrchardCoreTestAsync(UITestManifest testManifest, OrchardCoreUITestExecutorConfiguration configuration)
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

            var dumpFolderNameBase = testManifest.Name;
            if (configuration.FailureDumpConfiguration.UseShortNames && dumpFolderNameBase.Contains('(', StringComparison.Ordinal))
            {
#pragma warning disable S4635 // String offset-based methods should be preferred for finding substrings from offsets
                dumpFolderNameBase = dumpFolderNameBase.Substring(
                    dumpFolderNameBase.Substring(0, dumpFolderNameBase.IndexOf('(', StringComparison.Ordinal)).LastIndexOf('.') + 1);
#pragma warning restore S4635 // String offset-based methods should be preferred for finding substrings from offsets
            }

            var dumpRootPath = Path.Combine(configuration.FailureDumpConfiguration.DumpsDirectoryPath, dumpFolderNameBase.MakeFileSystemFriendly());
            DirectoryHelper.SafelyDeleteDirectoryIfExists(dumpRootPath);

            if (configuration.AccessibilityCheckingConfiguration.CreateReportAlways &&
                configuration.AccessibilityCheckingConfiguration.AlwaysCreatedAccessibilityReportsDirectoryPath is { } directoryPath &&
                !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            return Enumerable.Range(0, configuration.MaxRetryCount)
                .AwaitWhileAsync(retryCount => ExecuteOrchardCoreTestInnerAsync(
                    retryCount,
                    testManifest,
                    configuration,
                    runSetupOperation,
                    dumpRootPath))
                .ContinueWith(
                    task => DebugHelper.WriteTimestampedLine($"Finishing the execution of '{testManifest.Name}' with " +
                                                             $"total time: {DateTime.UtcNow - startTime}."),
                    TaskScheduler.Default);
        }


        private static async Task<bool> ExecuteOrchardCoreTestInnerAsync(
            int retryCount,
            UITestManifest testManifest,
            OrchardCoreUITestExecutorConfiguration configuration,
            bool runSetupOperation,
            string dumpRootPath)
        {
            var retry = true;
            var testOutputHelper = configuration.TestOutputHelper;

            await using var container = new UITestExecutorServiceContainer();
            if (runSetupOperation) await RunSetupOperationAsync(testManifest, configuration, container);

            try
            {
                if (container.Context == null) await CreateContextAsync(testManifest, configuration, container);
                testManifest.Test(container.Context);

                await configuration.AssertAppLogsMaybeAsync(container.Context!.Application, testOutputHelper.WriteLine);

                var browserLogs = await GetBrowserLogAsync(container.Context.Scope.Driver);
                configuration.AssertBrowserLogMaybe(browserLogs, testOutputHelper.WriteLine);

                retry = false;
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(
                    ex,
                    Path.Combine(dumpRootPath, $"Attempt {retryCount}"),
                    container,
                    configuration);

                var remaining = configuration.MaxRetryCount - retryCount;
                var remainingText = remaining > 0 ? remaining.ToString(CultureInfo.InvariantCulture) : "No";
                testOutputHelper.WriteLine(
                    $"The test was attempted {retryCount + 1} {(retryCount == 0 ? "time" : "times")}. " +
                    $"{remainingText} more {(remaining == 1 ? "attempt" : "attempts")} will be made.");

                if (retryCount == configuration.MaxRetryCount)
                {
                    var dumpFolderAbsolutePath = Path.Combine(AppContext.BaseDirectory, dumpRootPath);
                    testOutputHelper.WriteLine($"You can see more details in the folder: {dumpFolderAbsolutePath}");
                    throw;
                }
            }

            return retry;
        }

        private static async Task RunSetupOperationAsync(UITestManifest testManifest, OrchardCoreUITestExecutorConfiguration configuration, UITestExecutorServiceContainer container)
        {
            var resultUri = await _setupSnapshotManangerInstance.RunOperationAndSnapshotIfNewAsync(async () =>
            {
                // Note that the context creation needs to be done here too because the Orchard app needs
                // the snapshot config to be available at startup too.
                await CreateContextAsync(testManifest, configuration, container);

                if (configuration.UseSqlServer)
                {
                    // This is only necessary for the setup snapshot.
                    void SqlServerManagerBeforeTakeSnapshotHandler(string contentRootPath, string snapshotDirectoryPath)
                    {
                        configuration.OrchardCoreConfiguration.BeforeTakeSnapshot -= SqlServerManagerBeforeTakeSnapshotHandler;
                        container.SqlServerManager.TakeSnapshot(snapshotDirectoryPath);
                    }

                    configuration.OrchardCoreConfiguration.BeforeTakeSnapshot += SqlServerManagerBeforeTakeSnapshotHandler;
                }

                return (container.Context, configuration.SetupOperation(container.Context));
            });

            if (container.Context == null) await CreateContextAsync(testManifest, configuration, container);

            container.Context.GoToRelativeUrl(resultUri.PathAndQuery);
        }

        private static async Task CreateContextAsync(
            UITestManifest testManifest,
            OrchardCoreUITestExecutorConfiguration configuration,
            UITestExecutorServiceContainer serviceContainer)
        {
            var testOutputHelper = configuration.TestOutputHelper;

            if (configuration.UseSqlServer)
            {
                serviceContainer.SqlServerManager = new SqlServerManager(configuration.SqlServerDatabaseConfiguration);
                serviceContainer.SqlServerContext = serviceContainer.SqlServerManager.CreateDatabase();

                void SqlServerManagerBeforeAppStartHandler(string contentRootPath, ArgumentsBuilder argumentsBuilder)
                {
                    configuration.OrchardCoreConfiguration.BeforeAppStart -= SqlServerManagerBeforeAppStartHandler;

                    var snapshotDirectoryPath = configuration.OrchardCoreConfiguration.SnapshotDirectoryPath;

                    if (!Directory.Exists(snapshotDirectoryPath)) return;

                    serviceContainer.SqlServerManager.RestoreSnapshot(snapshotDirectoryPath);

                    // This method is not actually async.
#pragma warning disable AsyncFixer02 // Long-running or blocking operations inside an async method
                    var appSettingsPath = Path.Combine(contentRootPath, "App_Data", "Sites", "Default", "appsettings.json");
                    var appSettings = JObject.Parse(File.ReadAllText(appSettingsPath));
                    appSettings["ConnectionString"] = serviceContainer.SqlServerContext.ConnectionString;
                    File.WriteAllText(appSettingsPath, appSettings.ToString());
#pragma warning restore AsyncFixer02 // Long-running or blocking operations inside an async method
                }

                configuration.OrchardCoreConfiguration.BeforeAppStart += SqlServerManagerBeforeAppStartHandler;
            }

            SmtpServiceRunningContext smtpContext = null;

            if (configuration.UseSmtpService)
            {
                serviceContainer.SmtpService = new SmtpService(configuration.SmtpServiceConfiguration);
                smtpContext = await serviceContainer.SmtpService.StartAsync();

                void SmtpServiceBeforeAppStartHandler(string contentRootPath, ArgumentsBuilder argumentsBuilder)
                {
                    configuration.OrchardCoreConfiguration.BeforeAppStart -= SmtpServiceBeforeAppStartHandler;
                    argumentsBuilder.Add("--SmtpPort").Add(smtpContext.Port, CultureInfo.InvariantCulture);
                }

                configuration.OrchardCoreConfiguration.BeforeAppStart += SmtpServiceBeforeAppStartHandler;
            }

            serviceContainer.ApplicationInstance = new OrchardCoreInstance(configuration.OrchardCoreConfiguration, testOutputHelper);
            var uri = await serviceContainer.ApplicationInstance.StartUpAsync();

            var atataScope = AtataFactory.StartAtataScope(testOutputHelper, uri, configuration);

            serviceContainer.Context = new UITestContext(
                testManifest.Name,
                configuration,
                serviceContainer.SqlServerContext,
                serviceContainer.ApplicationInstance,
                atataScope,
                smtpContext);
        }

        private static async Task HandleErrorAsync(
            Exception exception,
            string dumpContainerPath,
            UITestExecutorServiceContainer container,
            OrchardCoreUITestExecutorConfiguration configuration)
        {
            var testOutputHelper = configuration.TestOutputHelper;
            testOutputHelper.WriteLine($"The test failed with the following exception: {exception}");

            var debugInformationPath = Path.Combine(dumpContainerPath, "DebugInformation");

            try
            {
                Directory.CreateDirectory(dumpContainerPath);
                Directory.CreateDirectory(debugInformationPath);

                if (container.Context != null)
                    await HandleErrorContextAsync(
                        exception,
                        container,
                        configuration,
                        dumpContainerPath,
                        debugInformationPath);
            }
            catch (Exception dumpException)
            {
                testOutputHelper.WriteLine(
                    $"Creating the failure dump of the test failed with the following exception: {dumpException}");
            }

            try
            {
                if (testOutputHelper is TestOutputHelper concreteTestOutputHelper)
                {
                    await File.WriteAllTextAsync(
                        Path.Combine(debugInformationPath, "TestOutput.log"),
                        concreteTestOutputHelper.Output);
                }
            }
            catch (Exception testOutputHelperException)
            {
                testOutputHelper.WriteLine(
                    $"Saving the contents of the test output failed with the following exception: {testOutputHelperException}");
            }
        }

        private static async Task HandleErrorContextAsync(
            Exception exception,
            UITestExecutorServiceContainer container,
            OrchardCoreUITestExecutorConfiguration configuration,
            string dumpContainerPath,
            string debugInformationPath)
        {
            var testOutputHelper = configuration.TestOutputHelper;
            if (configuration.FailureDumpConfiguration.CaptureAppSnapshot)
            {
                var appDumpPath = Path.Combine(dumpContainerPath, "AppDump");
                await container.Context.Application.TakeSnapshotAsync(appDumpPath);

                if (container.SqlServerManager != null)
                {
                    try
                    {
                        container.SqlServerManager.TakeSnapshot(appDumpPath, true);
                    }
                    catch (Exception failureException)
                    {
                        testOutputHelper.WriteLine(
                            $"Taking an SQL Server DB snapshot failed with the following exception: {failureException}");
                    }
                }
            }

            if (configuration.FailureDumpConfiguration.CaptureScreenshot)
            {
                // Only PNG is supported on .NET Core.
                container.Context.Scope.Driver.GetScreenshot().SaveAsFile(Path.Combine(debugInformationPath, "Screenshot.png"));
            }

            if (configuration.FailureDumpConfiguration.CaptureHtmlSource)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(debugInformationPath, "PageSource.html"),
                    container.Context.Scope.Driver.PageSource);
            }

            if (configuration.FailureDumpConfiguration.CaptureBrowserLog)
            {
                await File.WriteAllLinesAsync(
                    Path.Combine(debugInformationPath, "BrowserLog.log"),
                    (await GetBrowserLogAsync(container.Context.Scope.Driver))
                    .Select(message => message.ToString()));
            }

            if (exception is AccessibilityAssertionException accessibilityAssertionException
                && configuration.AccessibilityCheckingConfiguration.CreateReportOnFailure)
            {
                container.Context.Driver.CreateAxeHtmlReport(
                    accessibilityAssertionException.AxeResult,
                    Path.Combine(debugInformationPath, "AccessibilityReport.html"));
            }
        }

        private static Task<BrowserLogMessage[]> GetBrowserLogAsync(IWebDriver driver) =>
            driver.GetAndEmptyBrowserLogAsync().ContinueWith(task => task.Result.ToArray(), TaskScheduler.Default);
    }
}
