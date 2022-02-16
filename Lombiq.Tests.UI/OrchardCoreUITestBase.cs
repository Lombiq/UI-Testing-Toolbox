using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Delegates;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI
{
    public abstract class OrchardCoreUITestBase
    {
        private static readonly object _snapshotCopyLock = new();

        protected readonly ITestOutputHelper _testOutputHelper;

        private static bool _appFolderCreated;

        protected virtual Size StandardBrowserSize => CommonDisplayResolutions.Standard;
        protected virtual Size MobileBrowserSize => CommonDisplayResolutions.NhdPortrait;

        protected abstract string AppAssemblyPath { get; }

        protected OrchardCoreUITestBase(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
            MultiSizeTest standardAndMobileBrowserSizeTest,
            Browser browser,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
            ExecuteMultiSizeTestAfterSetupAsync(
                standardAndMobileBrowserSizeTest,
                browser,
                ConvertChangeConfigurationToAsynchronous(changeConfiguration));

        protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
            MultiSizeTest standardAndMobileBrowserSizeTest,
            Browser browser,
            Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
            ExecuteMultiSizeTestAfterSetupAsync(
                (context, isStandardSize) =>
                {
                    standardAndMobileBrowserSizeTest(context, isStandardSize);
                    return Task.CompletedTask;
                },
                browser,
                changeConfigurationAsync);

        protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
            MultiSizeTestAsync standardAndMobileBrowserSizeTestAsync,
            Browser browser,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
            ExecuteMultiSizeTestAfterSetupAsync(
                standardAndMobileBrowserSizeTestAsync,
                browser,
                ConvertChangeConfigurationToAsynchronous(changeConfiguration));

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
                standardBrowserSizeTest,
                mobileBrowserSizeTest,
                browser,
                ConvertChangeConfigurationToAsynchronous(changeConfiguration));

        protected virtual Task ExecuteMultiSizeTestAfterSetupAsync(
            MultiSizeTest standardBrowserSizeTest,
            MultiSizeTest mobileBrowserSizeTest,
            Browser browser,
            Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
            ExecuteMultiSizeTestAfterSetupAsync(
                ConvertMultiSizeTestToAsynchronous(standardBrowserSizeTest),
                ConvertMultiSizeTestToAsynchronous(mobileBrowserSizeTest),
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
                ConvertChangeConfigurationToAsynchronous(changeConfiguration));

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
            ExecuteTestAfterSetupAsync(
                ConvertTestToAsynchronous(test),
                browser,
                changeConfiguration);

        protected virtual Task ExecuteTestAfterSetupAsync(
           Func<UITestContext, Task> tesAsynct,
           Browser browser,
           Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
            ExecuteTestAfterSetupAsync(
                tesAsynct,
                browser,
                ConvertChangeConfigurationToAsynchronous(changeConfiguration));

        protected abstract Task ExecuteTestAfterSetupAsync(
            Func<UITestContext, Task> testAsync,
            Browser browser,
            Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync);

        /// <summary>
        /// Executes the given UI test, starting the app from an existing SQLite database available in the App_Data
        /// folder.
        /// </summary>
        protected virtual Task ExecuteTestFromExistingDBAsync(
            Func<UITestContext, Task> testAsync,
            Browser browser,
            Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync = null)
        {
            var appFolder = "AppFolder";

            lock (_snapshotCopyLock)
            {
                if (!_appFolderCreated)
                {
                    DirectoryHelper.SafelyDeleteDirectoryIfExists(appFolder);

                    OrchardCoreDirectoryHelper.CopyAppFolder(
                        OrchardCoreDirectoryHelper.GetAppRootPath(AppAssemblyPath),
                        appFolder);

                    _appFolderCreated = true;
                }
            }

            return ExecuteTestAsync(
                testAsync,
                browser,
                setupOperation: null,
                configuration =>
                {
                    configuration.SetupConfiguration.SetupSnapshotDirectoryPath = appFolder;
                    changeConfigurationAsync?.Invoke(configuration);
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
            ExecuteTestAsync(
                test,
                browser,
                setupOperation,
                ConvertChangeConfigurationToAsynchronous(changeConfiguration));

        /// <summary>
        /// Executes the given UI test, optionally after setting up the site.
        /// </summary>
        protected virtual Task ExecuteTestAsync(
            Action<UITestContext> test,
            Browser browser,
            Func<UITestContext, Task<Uri>> setupOperation,
            Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
            ExecuteTestAsync(
                ConvertTestToAsynchronous(test),
                browser,
                setupOperation,
                changeConfigurationAsync);

        /// <summary>
        /// Executes the given UI test, optionally after setting up the site.
        /// </summary>
        protected virtual Task ExecuteTestAsync(
            Func<UITestContext, Task> testAsync,
            Browser browser,
            Func<UITestContext, Task<Uri>> setupOperation = null,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
            ExecuteTestAsync(
                testAsync,
                browser,
                setupOperation,
                ConvertChangeConfigurationToAsynchronous(changeConfiguration));

        /// <summary>
        /// Executes the given UI test.
        /// </summary>
        protected virtual Task ExecuteTestAsync(
            Func<UITestContext, Task> testAsync,
            Browser browser,
            Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync) =>
            ExecuteTestAsync(
                testAsync,
                browser,
                setupOperation: null,
                changeConfigurationAsync);

        /// <summary>
        /// Executes the given UI test, optionally after setting up the site.
        /// </summary>
        protected virtual async Task ExecuteTestAsync(
            Func<UITestContext, Task> testAsync,
            Browser browser,
            Func<UITestContext, Task<Uri>> setupOperation,
            Func<OrchardCoreUITestExecutorConfiguration, Task> changeConfigurationAsync)
        {
            var testManifest = new UITestManifest
            {
                Name = (_testOutputHelper.GetType()
                    .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(field => field.FieldType == typeof(ITest))
                    ?.GetValue(_testOutputHelper) as ITest)
                    ?.DisplayName,
                TestAsync = testAsync,
            };

            var configuration = new OrchardCoreUITestExecutorConfiguration
            {
                OrchardCoreConfiguration = new OrchardCoreConfiguration { AppAssemblyPath = AppAssemblyPath },
                TestOutputHelper = _testOutputHelper,
                BrowserConfiguration = { Browser = browser },
            };

            configuration.SetupConfiguration.SetupOperation = setupOperation;

            if (changeConfigurationAsync != null) await changeConfigurationAsync(configuration);

            await UITestExecutor.ExecuteOrchardCoreTestAsync(testManifest, configuration);
        }

        private static MultiSizeTestAsync ConvertMultiSizeTestToAsynchronous(MultiSizeTest test) =>
            (context, isStandardSize) =>
            {
                test(context, isStandardSize);
                return Task.CompletedTask;
            };

        private static Func<UITestContext, Task> ConvertTestToAsynchronous(Action<UITestContext> test) =>
            context =>
            {
                test?.Invoke(context);
                return Task.CompletedTask;
            };

        private static Func<OrchardCoreUITestExecutorConfiguration, Task> ConvertChangeConfigurationToAsynchronous(
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration) =>
            configuration =>
            {
                changeConfiguration?.Invoke(configuration);
                return Task.CompletedTask;
            };
    }
}
