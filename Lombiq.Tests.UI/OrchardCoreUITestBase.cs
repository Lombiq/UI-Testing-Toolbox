using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Delegates;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using Microsoft.SqlServer.Management.Dmf;
using System;
using System.Drawing;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI;

public abstract class OrchardCoreUITestBase
{
    private const string AppFolder = nameof(AppFolder);

    private static readonly object _snapshotCopyLock = new();

    protected ITestOutputHelper _testOutputHelper;

    private static bool _appFolderCreated;

    protected abstract string AppAssemblyPath { get; }

    protected virtual Size StandardBrowserSize => CommonDisplayResolutions.Standard;
    protected virtual Size MobileBrowserSize => CommonDisplayResolutions.NhdPortrait;

    static OrchardCoreUITestBase() => AtataFactory.SetupShellCliCommandFactory();

    protected OrchardCoreUITestBase(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

    protected abstract Task ExecuteTestAfterSetupAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync);

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

    protected virtual Task ExecuteTestAfterSetupAsync(
        Action<UITestContext> test,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterSetupAsync(test.AsCompletedTask(), browser, changeConfiguration);

    protected virtual Task ExecuteTestAfterSetupAsync(
        Func<UITestContext, Task> tesAsynct,
        Browser browser,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAfterSetupAsync(tesAsynct, browser, changeConfiguration.AsCompletedTask());

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
        Browser browser,
        string customSnapshotFolderPath = null,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null)
    {
        lock (_snapshotCopyLock)
        {
            if (!_appFolderCreated)
            {
                DirectoryHelper.SafelyDeleteDirectoryIfExists(AppFolder);

                OrchardCoreDirectoryHelper.CopyAppFolder(
                    customSnapshotFolderPath ?? OrchardCoreDirectoryHelper.GetAppRootPath(AppAssemblyPath),
                    AppFolder);

                _appFolderCreated = true;
            }
        }

        return ExecuteTestAsync(
            testAsync,
            browser,
            setupOperation: null,
            async configuration =>
            {
                configuration.SetupConfiguration.SetupSnapshotDirectoryPath = AppFolder;
                if (changeConfigurationAsync != null) await changeConfigurationAsync(configuration);
            });
    }

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
        Browser browser,
        Func<UITestContext, Task<Uri>> setupOperation = null,
        Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
        ExecuteTestAsync(testAsync, browser, setupOperation, changeConfiguration.AsCompletedTask());

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
    protected virtual async Task ExecuteTestAsync(
        Func<UITestContext, Task> testAsync,
        Browser browser,
        Func<UITestContext, Task<Uri>> setupOperation,
        Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync)
    {
        var testManifest = new UITestManifest(_testOutputHelper) { TestAsync = testAsync };

        var originalTestOutputHelper = _testOutputHelper;
        Action afterTest = null;

        if (GitHubActionsGroupingTestOutputHelper.IsGitHubEnvironment.Value)
        {
            var gitHubActionsGroupingTestOutputHelper = new GitHubActionsGroupingTestOutputHelper(
                _testOutputHelper,
                $"{testManifest.XunitTest.TestCase.TestMethod.TestClass.Class.Name}.{testManifest.Name}");
            _testOutputHelper = gitHubActionsGroupingTestOutputHelper;
            afterTest += () => gitHubActionsGroupingTestOutputHelper.EndGroup();
        }

        var configuration = new OrchardCoreUITestExecutorConfiguration
        {
            OrchardCoreConfiguration = new OrchardCoreConfiguration { AppAssemblyPath = AppAssemblyPath },
            TestOutputHelper = _testOutputHelper,
            BrowserConfiguration = { Browser = browser },
            SetupConfiguration = { SetupOperation = setupOperation },
        };

        if (changeConfigurationAsync != null) await changeConfigurationAsync(configuration);

        await UITestExecutor.ExecuteOrchardCoreTestAsync(testManifest, configuration);

        _testOutputHelper = originalTestOutputHelper;
        afterTest?.Invoke();
    }

    public static void Throw() => throw new InvalidOperandException("Intentional failure.");
}
