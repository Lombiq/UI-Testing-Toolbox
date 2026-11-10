using Lombiq.Tests.UI.Delegates;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI;

// Obsolete methods moved from OrchardCoreUITestBase.cs to declutter it until the next release when they'll be removed.
public abstract partial class OrchardCoreUITestBase<TEntryPoint>
     where TEntryPoint : class
{
    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTest standardAndMobileBrowserSizeTest,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(standardAndMobileBrowserSizeTest, default(Browser), changeConfiguration);

    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTest standardAndMobileBrowserSizeTest,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardAndMobileBrowserSizeTest, browser, changeConfiguration.AsCompletedTask());

    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTest standardAndMobileBrowserSizeTest,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardAndMobileBrowserSizeTest.AsCompletedTask(), browser, changeConfigurationAsync);

    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTestAsync standardAndMobileBrowserSizeTestAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(standardAndMobileBrowserSizeTestAsync, default(Browser), changeConfiguration);

    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTestAsync standardAndMobileBrowserSizeTestAsync,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardAndMobileBrowserSizeTestAsync, browser, changeConfiguration.AsCompletedTask());

    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTestAsync standardAndMobileBrowserSizeTestAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardAndMobileBrowserSizeTestAsync,
            standardAndMobileBrowserSizeTestAsync,
            browser,
            changeConfigurationAsync);

    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTest standardBrowserSizeTest,
        MultiSizeTest mobileBrowserSizeTest,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(standardBrowserSizeTest, mobileBrowserSizeTest, default, changeConfiguration);

    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTest standardBrowserSizeTest,
        MultiSizeTest mobileBrowserSizeTest,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardBrowserSizeTest, mobileBrowserSizeTest, browser, changeConfiguration.AsCompletedTask());

    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTest standardBrowserSizeTest,
        MultiSizeTest mobileBrowserSizeTest,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardBrowserSizeTest.AsCompletedTask(),
            mobileBrowserSizeTest.AsCompletedTask(),
            browser,
            changeConfigurationAsync);

    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTestAsync standardBrowserSizeTestAsync,
        MultiSizeTestAsync mobileBrowserSizeTestAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(standardBrowserSizeTestAsync, mobileBrowserSizeTestAsync, default, changeConfiguration);

    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTestAsync standardBrowserSizeTestAsync,
        MultiSizeTestAsync mobileBrowserSizeTestAsync,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardBrowserSizeTestAsync,
            mobileBrowserSizeTestAsync,
            browser,
            changeConfiguration.AsCompletedTask());

    [Obsolete($"Use {nameof(AddMultiSizeTest)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTestAsync standardBrowserSizeTestAsync,
        MultiSizeTestAsync mobileBrowserSizeTestAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                context.SetBrowserSize(StandardBrowserSize);
                await standardBrowserSizeTestAsync(context, isStandardSize: true);
                context.SetBrowserSize(MobileBrowserSize);
                await mobileBrowserSizeTestAsync(context, isStandardSize: false);
            },
            browser,
            changeConfigurationAsync);

    [Obsolete($"Use {nameof(WithoutBrowser)} instead.")]
    protected virtual Task ExecuteTestAfterBrowserSetupWithoutBrowserAsync(
        Func<UITestContext, Task> testAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterBrowserSetupWithoutBrowserAsync(testAsync, default, changeConfiguration);

    [Obsolete($"Use {nameof(WithoutBrowser)} instead.")]
    protected Task ExecuteTestAfterBrowserSetupWithoutBrowserAsync(
        Func<UITestContext, Task> testAsync,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAfterBrowserSetupWithoutBrowserAsync(testAsync, default, changeConfigurationAsync);

    [Obsolete($"Use {nameof(WithoutBrowser)} instead.")]
    protected virtual Task ExecuteTestAfterBrowserSetupWithoutBrowserAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterBrowserSetupWithoutBrowserAsync(testAsync, changeConfiguration.AsCompletedTask());

    [Obsolete($"Use {nameof(WithoutBrowser)} instead.")]
    protected Task ExecuteTestAfterBrowserSetupWithoutBrowserAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAfterSetupWithoutBrowserAsync(testAsync, async configuration =>
        {
            configuration.SetupConfiguration.BeforeSetup = configuration =>
            {
                configuration.BrowserConfiguration.Browser = browser;
                return Task.CompletedTask;
            };

            configuration.SetupConfiguration.AfterSetup = configuration =>
            {
                configuration.BrowserConfiguration.Browser = Browser.None;
                return Task.CompletedTask;
            };

            await changeConfigurationAsync.InvokeFuncAsync(configuration);
        });

    [Obsolete($"Use {nameof(WithoutBrowser)} instead.")]
    protected virtual Task ExecuteTestAfterSetupWithoutBrowserAsync(
        Func<UITestContext, Task> testAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterSetupWithoutBrowserAsync(testAsync, changeConfiguration.AsCompletedTask());

    // If you change this, then also change the corresponding delegate above.
    [Obsolete($"Use {nameof(WithoutBrowser)} instead.")]
    protected Task ExecuteTestAfterSetupWithoutBrowserAsync(
        Func<UITestContext, Task> testAsync,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAfterSetupAsync(testAsync, Browser.None, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, starting the app from an existing SQLite database available in the App_Data folder.
    /// </summary>
    [Obsolete($"Use {nameof(UseExistingDatabaseAndRunAsync)} instead.")]
    protected virtual Task ExecuteTestFromExistingDBAsync(
        Func<UITestContext, Task> testAsync,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null) =>
        ExecuteTestFromExistingDBAsync(testAsync, default(Browser), changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, starting the app from an existing SQLite database available in the App_Data folder.
    /// </summary>
    [Obsolete($"Use {nameof(UseExistingDatabaseAndRunAsync)} instead.")]
    protected virtual Task ExecuteTestFromExistingDBAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null) =>
        ExecuteTestFromExistingDBAsync(testAsync, browser, customSnapshotFolderPath: null, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, starting the app from an existing SQLite database available in the App_Data or in
    /// the given folder.
    /// </summary>
    [Obsolete($"Use {nameof(UseExistingDatabaseAndRunAsync)} instead.")]
    protected virtual Task ExecuteTestFromExistingDBAsync(
        Func<UITestContext, Task> testAsync,
        string customSnapshotFolderPath = null,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null) =>
        ExecuteTestFromExistingDBAsync(testAsync, default, customSnapshotFolderPath, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, starting the app from an existing SQLite database available in the App_Data or in
    /// the given folder.
    /// </summary>
    [Obsolete($"Use {nameof(UseExistingDatabaseAndRunAsync)} instead.")]
    protected virtual async Task ExecuteTestFromExistingDBAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        string customSnapshotFolderPath = null,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null)
    {
        await OrchardCoreUITestBaseCounter.SnapshotCopySemaphoreSlim.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            if (!OrchardCoreUITestBaseCounter.AppFolderCreated)
            {
                await DirectoryHelper.SafelyDeleteDirectoryIfExistsAsync(AppFolder, TestContext.Current.CancellationToken);

                await OrchardCoreDirectoryHelper.CopyAppFolderAsync(
                    customSnapshotFolderPath
                        ?? OrchardCoreDirectoryHelper.GetAppRootPath(typeof(TEntryPoint).Assembly.Location),
                    AppFolder,
                    TestContext.Current.CancellationToken);

                OrchardCoreUITestBaseCounter.AppFolderCreated = true;
            }
        }
        finally
        {
            OrchardCoreUITestBaseCounter.SnapshotCopySemaphoreSlim.Release();
        }

        await ExecuteTestAsync(
            testAsync,
            browser,
            setupOperation: null,
            async configuration =>
            {
                configuration.SetupConfiguration.SetupSnapshotDirectoryPath = AppFolder;
                await changeConfigurationAsync.InvokeFuncAsync(configuration);
            });
    }

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    [Obsolete($"Use {nameof(UseSetup)}, {nameof(AddSyncConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Action<UITestContext> test,
        Func<UITestContext, Task<Uri>> setupOperation = null,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(test, default, setupOperation, changeConfiguration);

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    [Obsolete($"Use {nameof(Browser)}, {nameof(UseSetup)}, {nameof(AddSyncConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Action<UITestContext> test,
        Browser browser,
        Func<UITestContext, Task<Uri>> setupOperation = null,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(test, browser, setupOperation, changeConfiguration.AsCompletedTask());

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    [Obsolete($"Use {nameof(Browser)}, {nameof(UseSetup)}, {nameof(AddConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Action<UITestContext> test,
        Browser browser,
        Func<UITestContext, Task<Uri>> setupOperation,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(test.AsCompletedTask(), browser, setupOperation, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    [Obsolete($"Use {nameof(UseSetup)}, {nameof(AddSyncConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Func<UITestContext, Task<Uri>> setupOperation = null,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(testAsync, default, setupOperation, changeConfiguration);

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    [Obsolete($"Use {nameof(Browser)}, {nameof(UseSetup)}, {nameof(AddSyncConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<UITestContext, Task<Uri>> setupOperation = null,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(testAsync, browser, setupOperation, changeConfiguration.AsCompletedTask());

    /// <summary>
    /// Executes the given UI test.
    /// </summary>
    [Obsolete($"Use {nameof(AddConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(testAsync, default(Browser), changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test.
    /// </summary>
    [Obsolete($"Use {nameof(Browser)}, {nameof(AddConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(testAsync, browser, setupOperation: null, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    [Obsolete($"Use {nameof(UseSetup)}, {nameof(AddConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Func<UITestContext, Task<Uri>> setupOperation,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAsync(testAsync, default, setupOperation, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, optionally after setting up the site.
    /// </summary>
    [Obsolete($"Use {nameof(Browser)}, {nameof(UseSetup)}, {nameof(AddConfiguration)} and {nameof(RunAsync)} instead.")]
    protected virtual Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<UITestContext, Task<Uri>> setupOperation,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync)
    {
        Browser = browser;

        AddConfiguration(changeConfigurationAsync);
        UseSetup(setupOperation);

        return RunAsync(testAsync);
    }
}
