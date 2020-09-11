using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI
{
    public abstract class OrchardCoreUITestBase
    {
        private static readonly object _snapshotCopyLock = new object();

        protected readonly ITestOutputHelper _testOutputHelper;

        private static bool _appFolderCreated;

        protected abstract string AppAssemblyPath { get; }


        protected OrchardCoreUITestBase(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;


        /// <summary>
        /// Executes the given UI test, optionally after setting up the site.
        /// </summary>
        protected virtual Task ExecuteTestAsync(
            Action<UITestContext> test,
            Browser browser,
            Func<UITestContext, Uri> setupOperation = null,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null)
        {
            var testManifest = new UITestManifest
            {
                Name = (_testOutputHelper.GetType()
                    .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(x => x.FieldType == typeof(ITest))
                    ?.GetValue(_testOutputHelper) as ITest)?.DisplayName,
                Test = test,
            };

            var configuration = new OrchardCoreUITestExecutorConfiguration
            {
                OrchardCoreConfiguration = new OrchardCoreConfiguration { AppAssemblyPath = AppAssemblyPath },
                SetupOperation = setupOperation,
                TestOutputHelper = _testOutputHelper,
                BrowserConfiguration = { Browser = browser },
            };

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
                null,
                configuration =>
                {
                    configuration.SetupSnapshotPath = appFolder;
                    changeConfiguration?.Invoke(configuration);
                });
        }
    }
}
