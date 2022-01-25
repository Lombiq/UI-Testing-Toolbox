using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Delegates;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System;
using System.Drawing;
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

        protected Task ExecuteMultiSizeTestAfterSetupAsync(
            MultiSizeTest standardBrowserSizeTest,
            MultiSizeTest mobileBrowserSizeTest,
            Browser browser,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context.SetBrowserSize(StandardBrowserSize);
                    standardBrowserSizeTest(context, isStandardSize: true);
                    context.SetBrowserSize(MobileBrowserSize);
                    mobileBrowserSizeTest(context, isStandardSize: false);
                },
                browser,
                changeConfiguration);

        protected Task ExecuteMultiSizeTestAfterSetupAsync(
            MultiSizeTest standardAndMobileBrowserSizeTest,
            Browser browser,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
            ExecuteMultiSizeTestAfterSetupAsync(
                standardAndMobileBrowserSizeTest,
                standardAndMobileBrowserSizeTest,
                browser,
                changeConfiguration);

        protected abstract Task ExecuteTestAfterSetupAsync(
            Action<UITestContext> test,
            Browser browser,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null);

        /// <summary>
        /// Executes the given UI test, optionally after setting up the site.
        /// </summary>
        protected virtual Task ExecuteTestAsync(
            Action<UITestContext> test,
            Browser browser,
            Func<UITestContext, Uri> setupOperation = null,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null)
        {
            var testManifest = new UITestManifest(_testOutputHelper)
            {
                Test = test,
            };

            var configuration = new OrchardCoreUITestExecutorConfiguration
            {
                OrchardCoreConfiguration = new OrchardCoreConfiguration { AppAssemblyPath = AppAssemblyPath },
                TestOutputHelper = _testOutputHelper,
                BrowserConfiguration = { Browser = browser },
            };

            configuration.SetupConfiguration.SetupOperation = setupOperation;

            changeConfiguration?.Invoke(configuration);

            return UITestExecutor.ExecuteOrchardCoreTestAsync(testManifest, configuration);
        }

        /// <summary>
        /// Executes the given UI test, starting the app from an existing SQLite database available in the App_Data
        /// folder.
        /// </summary>
        protected virtual Task ExecuteTestFromExistingDBAsync(
            Action<UITestContext> test,
            Browser browser,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null)
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
                test,
                browser,
                setupOperation: null,
                configuration =>
                {
                    configuration.SetupConfiguration.SetupSnapshotDirectoryPath = appFolder;
                    changeConfiguration?.Invoke(configuration);
                });
        }
    }
}
