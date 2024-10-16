using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Delegates;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using SixLabors.ImageSharp;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI;

internal static class OrchardCoreUITestBaseCounter
{
    public static object SnapshotCopyLock { get; } = new();
    public static bool AppFolderCreated { get; set; }
}

/// <summary>
/// Delegate with the signature of <see
/// cref="OrchardCoreUITestBase{TEntryPoint}.ExecuteTestAfterSetupAsync(Func{UITestContext, Task}, Browser,
/// Func{OrchardCoreUITestExecutorConfiguration, Task})"/> so test case classes like <c>CustomAdminPrefixTestCase</c> in
/// <c>Lombiq.Tests.UI.Tests.UI</c> can easily depend on the method without having to define custom delegate
/// parameters.
/// </summary>
// If you change this, then also change the corresponding method below.
public delegate Task ExecuteTestAfterSetupAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync);

public abstract class OrchardCoreUITestBase<TEntryPoint> : UITestBase
     where TEntryPoint : class
{
    private const string AppFolder = nameof(AppFolder);

    protected virtual Size StandardBrowserSize => CommonDisplayResolutions.Standard;
    protected virtual Size MobileBrowserSize => CommonDisplayResolutions.NhdPortrait; // #spell-check-ignore-line

    protected OrchardCoreUITestBase(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // If you change this, then also change the corresponding delegate above.
    protected abstract Task ExecuteTestAfterSetupAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync);

    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTest standardAndMobileBrowserSizeTest,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(standardAndMobileBrowserSizeTest, default(Browser), changeConfiguration);

    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTest standardAndMobileBrowserSizeTest,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardAndMobileBrowserSizeTest, browser, changeConfiguration.AsCompletedTask());

    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTest standardAndMobileBrowserSizeTest,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardAndMobileBrowserSizeTest.AsCompletedTask(), browser, changeConfigurationAsync);

    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTestAsync standardAndMobileBrowserSizeTestAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(standardAndMobileBrowserSizeTestAsync, default(Browser), changeConfiguration);

    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTestAsync standardAndMobileBrowserSizeTestAsync,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardAndMobileBrowserSizeTestAsync, browser, changeConfiguration.AsCompletedTask());

    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTestAsync standardAndMobileBrowserSizeTestAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardAndMobileBrowserSizeTestAsync,
            standardAndMobileBrowserSizeTestAsync,
            browser,
            changeConfigurationAsync);

    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTest standardBrowserSizeTest,
        MultiSizeTest mobileBrowserSizeTest,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(standardBrowserSizeTest, mobileBrowserSizeTest, default, changeConfiguration);

    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTest standardBrowserSizeTest,
        MultiSizeTest mobileBrowserSizeTest,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(
            standardBrowserSizeTest, mobileBrowserSizeTest, browser, changeConfiguration.AsCompletedTask());

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

    protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
        MultiSizeTestAsync standardBrowserSizeTestAsync,
        MultiSizeTestAsync mobileBrowserSizeTestAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteMultiSizeTestAfterSetupAsync(standardBrowserSizeTestAsync, mobileBrowserSizeTestAsync, default, changeConfiguration);

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

    protected virtual Task ExecuteTestAfterBrowserSetupWithoutBrowserAsync(
        Func<UITestContext, Task> testAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterBrowserSetupWithoutBrowserAsync(testAsync, default, changeConfiguration);

    protected Task ExecuteTestAfterBrowserSetupWithoutBrowserAsync(
        Func<UITestContext, Task> testAsync,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAfterBrowserSetupWithoutBrowserAsync(testAsync, default, changeConfigurationAsync);

    protected virtual Task ExecuteTestAfterBrowserSetupWithoutBrowserAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterBrowserSetupWithoutBrowserAsync(testAsync, changeConfiguration.AsCompletedTask());

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

    protected virtual Task ExecuteTestAfterSetupWithoutBrowserAsync(
        Func<UITestContext, Task> testAsync,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterSetupWithoutBrowserAsync(testAsync, changeConfiguration.AsCompletedTask());

    protected Task ExecuteTestAfterSetupWithoutBrowserAsync(
        Func<UITestContext, Task> testAsync,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
        ExecuteTestAfterSetupAsync(testAsync, Browser.None, changeConfigurationAsync);

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
    /// Executes the given UI test, starting the app from an existing SQLite database available in the App_Data folder.
    /// </summary>
    protected virtual Task ExecuteTestFromExistingDBAsync(
        Func<UITestContext, Task> testAsync,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null) =>
        ExecuteTestFromExistingDBAsync(testAsync, default(Browser), changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, starting the app from an existing SQLite database available in the App_Data folder.
    /// </summary>
    protected virtual Task ExecuteTestFromExistingDBAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null) =>
        ExecuteTestFromExistingDBAsync(testAsync, browser, customSnapshotFolderPath: null, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, starting the app from an existing SQLite database available in the App_Data or in
    /// the given folder.
    /// </summary>
    protected virtual Task ExecuteTestFromExistingDBAsync(
        Func<UITestContext, Task> testAsync,
        string customSnapshotFolderPath = null,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null) =>
        ExecuteTestFromExistingDBAsync(testAsync, default, customSnapshotFolderPath, changeConfigurationAsync);

    /// <summary>
    /// Executes the given UI test, starting the app from an existing SQLite database available in the App_Data or in
    /// the given folder.
    /// </summary>
    protected virtual Task ExecuteTestFromExistingDBAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        string customSnapshotFolderPath = null,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null)
    {
        lock (OrchardCoreUITestBaseCounter.SnapshotCopyLock)
        {
            if (!OrchardCoreUITestBaseCounter.AppFolderCreated)
            {
                DirectoryHelper.SafelyDeleteDirectoryIfExists(AppFolder);

                OrchardCoreDirectoryHelper.CopyAppFolder(
                    customSnapshotFolderPath
                        ?? OrchardCoreDirectoryHelper.GetAppRootPath(typeof(TEntryPoint).Assembly.Location),
                    AppFolder);

                OrchardCoreUITestBaseCounter.AppFolderCreated = true;
            }
        }

        return ExecuteTestAsync(
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
    protected virtual async Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<UITestContext, Task<Uri>> setupOperation,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync)
    {
        var testManifest = new UITestManifest(_testOutputHelper) { TestAsync = testAsync };

        var configuration = new OrchardCoreUITestExecutorConfiguration
        {
            OrchardCoreConfiguration = new OrchardCoreConfiguration(),
            TestOutputHelper = _testOutputHelper,
            BrowserConfiguration = { Browser = browser },
            SetupConfiguration = { SetupOperation = setupOperation },
        };

        await changeConfigurationAsync.InvokeFuncAsync(configuration);

        await ExecuteOrchardCoreTestAsync(
            (configuration, contextId) =>
                new OrchardCoreInstance<TEntryPoint>(configuration.OrchardCoreConfiguration, contextId, configuration.TestOutputHelper),
            testManifest,
            configuration);
    }
}
