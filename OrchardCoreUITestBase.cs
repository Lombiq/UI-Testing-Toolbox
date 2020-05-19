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
        protected readonly ITestOutputHelper _testOutputHelper;

        protected abstract string AppAssemblyPath { get; }


        protected OrchardCoreUITestBase(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;


        protected virtual Task ExecuteTest(
            Action<UITestContext> test,
            Browser browser,
            Func<UITestContext, Uri> setupOperation,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null)
        {
            var testManifest = new UITestManifest
            {
                Name = (_testOutputHelper.GetType()
                    .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(x => x.FieldType == typeof(ITest))
                    ?.GetValue(_testOutputHelper) as ITest)?.DisplayName,
                Test = test
            };

            var configuration = new OrchardCoreUITestExecutorConfiguration
            {
                Browser = browser,
                OrchardCoreConfiguration = new OrchardCoreConfiguration { AppAssemblyPath = AppAssemblyPath },
                SetupOperation = setupOperation,
                TestOutputHelper = _testOutputHelper
            };

            changeConfiguration?.Invoke(configuration);

            return UITestExecutor.ExecuteOrchardCoreTest(testManifest, configuration);
        }
    }
}
