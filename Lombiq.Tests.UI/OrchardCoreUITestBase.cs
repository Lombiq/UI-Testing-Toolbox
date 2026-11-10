using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Delegates;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Services.GitHub;
using SixLabors.ImageSharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI;

// These are not static fields because those shouldn't be used in generic types. See:
// https://rules.sonarsource.com/csharp/RSPEC-2743/.
internal static class OrchardCoreUITestBaseCounter
{
    public static SemaphoreSlim SnapshotCopySemaphoreSlim = new(1, 1);
    public static bool AppFolderCreated { get; set; }
}

public abstract partial class OrchardCoreUITestBase<TEntryPoint> : UITestBase
     where TEntryPoint : class
{
    private const string AppFolder = nameof(AppFolder);

    protected virtual Size StandardBrowserSize => CommonDisplayResolutions.Standard;
    protected virtual Size MobileBrowserSize => CommonDisplayResolutions.NhdPortrait;

    protected override WebApplicationInstanceFactory InstanceFactory { get; }

    protected OrchardCoreUITestBase(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper) =>
        InstanceFactory = (configuration, contextId) => new OrchardCoreInstance<TEntryPoint>(configuration, contextId);

    /// <summary>
    /// Adds the provided two test methods, to be executed with the browser size of <see cref="StandardBrowserSize"/>
    /// and then <see cref="MobileBrowserSize"/>.
    /// </summary>
    /// <param name="standardBrowserSizeTestAsync">
    /// When ran, the browser size is changed to <see cref="StandardBrowserSize"/>, and then this method is executed.
    /// </param>
    /// <param name="mobileBrowserSizeTestAsync">
    /// When ran, the browser size is changed to <see cref="MobileBrowserSize"/>, and then this method is executed. If
    /// this parameter is <see langword="null"/>, <paramref name="standardBrowserSizeTestAsync"/> is used for the mobile
    /// size as well.
    /// </param>
    protected OrchardCoreUITestBase<TEntryPoint> AddMultiSizeTest(
        MultiSizeTestAsync standardBrowserSizeTestAsync,
        MultiSizeTestAsync mobileBrowserSizeTestAsync = null)
    {
        ArgumentNullException.ThrowIfNull(standardBrowserSizeTestAsync);
        mobileBrowserSizeTestAsync ??= standardBrowserSizeTestAsync;

        AddTest(context =>
        {
            context.SetBrowserSize(StandardBrowserSize);
            return standardBrowserSizeTestAsync(context, isStandardSize: true);
        });

        AddTest(context =>
        {
            context.SetBrowserSize(MobileBrowserSize);
            return mobileBrowserSizeTestAsync(context, isStandardSize: false);
        });

        return this;
    }

    // If you change this, then also change the corresponding delegate above.
    protected virtual Task ExecuteTestAfterSetupAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync)
    {
        Browser = browser;
        AddConfiguration(changeConfigurationAsync);
        return RunAsync(testAsync);
    }

    protected virtual Task ExecuteTestAfterSetupAsync(
        Action<UITestContext> test,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterSetupAsync(test, default, changeConfiguration);

    protected virtual Task ExecuteTestAfterSetupAsync(
        Action<UITestContext> test,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterSetupAsync(test.AsCompletedTask(), browser, changeConfiguration);

    protected virtual Task ExecuteTestAfterSetupAsync(
        Func<UITestContext, Task> testAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterSetupAsync(testAsync, default, changeConfiguration);

    protected virtual Task ExecuteTestAfterSetupAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterSetupAsync(testAsync, browser, changeConfiguration.AsCompletedTask());

    protected Task ExecuteTestAfterSetupAsync(
        Func<UITestContext, Task> testAsync,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAfterSetupAsync(testAsync, default, changeConfigurationAsync);

    /// <summary>
    /// Executes test by starting the app from an existing SQLite database.
    /// </summary>
    /// <param name="customSnapshotFolderPath">
    /// The directory containing the SQLite file. If it's <see langword="null"/>, then the App_Data folder is used.
    /// </param>
    protected async Task UseExistingDatabaseAndRunAsync(string customSnapshotFolderPath = null)
    {
        var token = TestContext.Current.CancellationToken;
        await OrchardCoreUITestBaseCounter.SnapshotCopySemaphoreSlim.WaitAsync(token);

        try
        {
            if (!OrchardCoreUITestBaseCounter.AppFolderCreated)
            {
                await DirectoryHelper.SafelyDeleteDirectoryIfExistsAsync(AppFolder, token);

                customSnapshotFolderPath ??= OrchardCoreDirectoryHelper.GetAppRootPath(typeof(TEntryPoint).Assembly.Location);
                await OrchardCoreDirectoryHelper.CopyAppFolderAsync(customSnapshotFolderPath, AppFolder, token);

                OrchardCoreUITestBaseCounter.AppFolderCreated = true;
            }
        }
        finally
        {
            OrchardCoreUITestBaseCounter.SnapshotCopySemaphoreSlim.Release();
        }

        UseSetup(setupAsync: null);
        AddSyncConfiguration(configuration => configuration.SetupConfiguration.SetupSnapshotDirectoryPath = AppFolder);
        await RunAsync();
    }

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Action<UITestContext> test,
        Func<UITestContext, Task<Uri>> setupOperation = null,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(test, default, setupOperation, changeConfiguration);

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Action<UITestContext> test,
        Browser browser,
        Func<UITestContext, Task<Uri>> setupOperation = null,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(test, browser, setupOperation, changeConfiguration.AsCompletedTask());

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Action<UITestContext> test,
        Browser browser,
        Func<UITestContext, Task<Uri>> setupOperation,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(test.AsCompletedTask(), browser, setupOperation, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Func<UITestContext, Task<Uri>> setupOperation = null,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(testAsync, default, setupOperation, changeConfiguration);

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<UITestContext, Task<Uri>> setupOperation = null,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(testAsync, browser, setupOperation, changeConfiguration.AsCompletedTask());

    /// <summary>
    /// Executes the given UI test.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(testAsync, default(Browser), changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(testAsync, browser, setupOperation: null, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Func<UITestContext, Task<Uri>> setupOperation,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(testAsync, default, setupOperation, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<UITestContext, Task<Uri>> setupOperation,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync)
    {
        Browser = browser;

        AddConfiguration(changeConfigurationAsync);
        AddTest(testAsync);
        UseSetup(setupOperation);

        return RunAsync();
    }

    /// <summary>
    /// Starts a "test" with configuration adjustments to make it more suitable for interactive exploration of the UI
    /// testing setup. In a GitHub Actions environment this method does nothing.
    /// </summary>
    protected Task OpenSandboxAfterSetupAsync(
        Func<UITestContext, Task> testAsync = null,
        Browser browser = default,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null)
    {
        // This "test" will wait indefinitely, so it's important to skip it in CI.
        if (GitHubHelper.IsGitHubEnvironment) return Task.CompletedTask;

        testAsync ??= context => context.SwitchToInteractiveAsync();

        return ExecuteTestAfterSetupAsync(
            testAsync,
            browser,
            configuration =>
            {
                // Since this made for human interaction, make sure the browser is always displayed and disable retries.
                configuration.BrowserConfiguration.Headless = false;
                configuration.MaxRetryCount = 0;

                return changeConfigurationAsync == null ? Task.CompletedTask : changeConfigurationAsync(configuration);
            });
    }

    public override Task RunAsync()
    {
        AddSyncConfiguration(configuration => configuration.SetupConfiguration.SetupOperation = Setup);
        return base.RunAsync();
    }
}
