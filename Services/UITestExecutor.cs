using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using OpenQA.Selenium.Remote;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    public class UITestManifest
    {
        public string Name { get; set; }
        public Action<UITestContext> Test { get; set; }
    }


    public static class UITestExecutor
    {
        private readonly static object _setupSnapshotManangerLock = new object();
        private static SynchronizingWebApplicationSnapshotManager _setupSnapshotManangerInstance;


        /// <summary>
        /// Executes a test on a new Orchard Core web app instance within a newly created Atata scope.
        /// </summary>
        public static async Task ExecuteOrchardCoreTest(UITestManifest testManifest, OrchardCoreUITestExecutorConfiguration configuration)
        {
            if (string.IsNullOrEmpty(testManifest.Name))
            {
                throw new ArgumentException("You need to specify the name of the test.");
            }

            if (configuration.OrchardCoreConfiguration == null)
            {
                throw new ArgumentNullException($"{nameof(configuration.OrchardCoreConfiguration)} should be provided.");
            }

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
            if (dumpConfiguration.UseShortNames && dumpFolderNameBase.Contains('('))
            {
                dumpFolderNameBase = dumpFolderNameBase.Substring(
                    dumpFolderNameBase.Substring(0, dumpFolderNameBase.IndexOf('(')).LastIndexOf('.') + 1);
            }
            var sanitizedTestName = string
                .Join("_", dumpFolderNameBase.Split(Path.GetInvalidFileNameChars()))
                .Replace('.', '_')
                .Replace(' ', '-');
            var dumpRootPath = Path.Combine(dumpConfiguration.DumpsDirectoryPath, sanitizedTestName);
            DirectoryHelper.SafelyDeleteDirectoryIfExists(dumpRootPath);

            var testOutputHelper = configuration.TestOutputHelper;
            var tryCount = 1;
            while (true)
            {
                BrowserLogMessage[] browserLogMessages = null;
                async Task<BrowserLogMessage[]> GetBrowserLog(RemoteWebDriver driver) =>
                    browserLogMessages ??= (await driver.GetAndEmptyBrowserLog()).ToArray();

                SmtpService smtpService = null;
                IWebApplicationInstance applicationInstance = null;
                UITestContext context = null;

                try
                {
                    async Task<UITestContext> CreateContext()
                    {
                        SmtpServiceRunningContext smtpContext = null;

                        if (configuration.UseSmtpService)
                        {
                            smtpService = new SmtpService();
                            smtpContext = await smtpService.Start();
                            configuration.OrchardCoreConfiguration.BeforeAppStart += (contentRoot, argumentsBuilder) =>
                                argumentsBuilder.Add("--SmtpPort").Add(smtpContext.Port);
                        }

                        applicationInstance = new OrchardCoreInstance(configuration.OrchardCoreConfiguration, testOutputHelper);
                        var uri = await applicationInstance.StartUp();

                        var atataScope = AtataFactory.StartAtataScope(
                            testOutputHelper,
                            uri,
                            configuration);

                        return new UITestContext(applicationInstance, atataScope, smtpContext);
                    }

                    if (runSetupOperation)
                    {
                        var resultUri = await _setupSnapshotManangerInstance.RunOperationAndSnapshotIfNew(async () =>
                        {
                            // Note that the context creation needs to be done here too because the Orchard app needs
                            // the snapshot config to be available at startup too.
                            context = await CreateContext();

                            return (context, configuration.SetupOperation(context));
                        });

                        if (context == null) context = await CreateContext();

                        context.GoToRelativeUrl(resultUri.PathAndQuery);
                    }

                    if (context == null) context = await CreateContext();

                    testManifest.Test(context);

                    try
                    {
                        if (configuration.AssertAppLogs != null) await configuration.AssertAppLogs(context.Application);
                    }
                    catch (Exception)
                    {
                        testOutputHelper.WriteLine("Application logs: " + Environment.NewLine);
                        testOutputHelper.WriteLine(await context.Application.GetLogOutput());

                        throw;
                    }

                    try
                    {
                        configuration.AssertBrowserLog?.Invoke(await GetBrowserLog(context.Scope.Driver));
                    }
                    catch (Exception)
                    {
                        testOutputHelper.WriteLine("Browser logs: " + Environment.NewLine);
                        testOutputHelper.WriteLine((await GetBrowserLog(context.Scope.Driver)).ToFormattedString());

                        throw;
                    }

                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"\n\n\n{ex}\n\n");

                    // If there is no context then something went really wrong. This did happen a few times already
                    // though, unclear yet why.
                    if (context == null) throw;

                    var dumpContainerPath = Path.Combine(dumpRootPath, "Attempt " + tryCount.ToString());

                    if (dumpConfiguration.CaptureAppSnapshot)
                    {
                        await context.Application.TakeSnapshot(dumpContainerPath);
                    }

                    if (dumpConfiguration.CaptureScreenshot)
                    {
                        // Only PNG is supported on .NET Core.
                        context.Scope.Driver.GetScreenshot().SaveAsFile(Path.Combine(dumpContainerPath, "Screenshot.png"));
                    }

                    if (dumpConfiguration.CaptureHtmlSource)
                    {
                        await File.WriteAllTextAsync(Path.Combine(dumpContainerPath, "PageSource.html"), context.Scope.Driver.PageSource);
                    }

                    if (dumpConfiguration.CaptureBrowserLog)
                    {
                        await File.WriteAllLinesAsync(
                            Path.Combine(dumpContainerPath, "BrowserLog.log"),
                            (await GetBrowserLog(context.Scope.Driver)).Select(message => message.ToString()));
                    }

                    if (tryCount == configuration.MaxTryCount)
                    {
                        testOutputHelper.WriteLine($"The test was attempted {tryCount} times and won't be retried anymore. You can see more details on why it's failing in the FailureDumps folder.");
                        throw;
                    }

                    testOutputHelper.WriteLine(
                        $"The test was attempted {tryCount} times. {configuration.MaxTryCount - tryCount} more attempt(s) will be made.");
                }
                finally
                {
                    if (context != null) context.Scope.Dispose();
                    if (applicationInstance != null) await applicationInstance.DisposeAsync();
                    if (smtpService != null) await smtpService.DisposeAsync();
                }

                tryCount++;
            }
        }
    }
}
