using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Services.GitHub;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI;

public abstract class UITestBase
{
    protected ITestOutputHelper _testOutputHelper;

    static UITestBase() => AtataFactory.SetupShellCliCommandFactory();

    protected UITestBase(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

    protected async Task ExecuteOrchardCoreTestAsync(
        WebApplicationInstanceFactory webApplicationInstanceFactory,
        UITestManifest testManifest,
        OrchardCoreUITestExecutorConfiguration configuration)
    {
        var originalTestOutputHelper = _testOutputHelper;
        var timeout = configuration.TimeoutConfiguration.TestRunTimeout;

        Action afterTest = null;
        if (configuration.ExtendGitHubActionsOutput &&
            configuration.GitHubActionsOutputConfiguration.EnablePerTestOutputGrouping &&
            GitHubHelper.IsGitHubEnvironment)
        {
            (_testOutputHelper, afterTest) =
                GitHubActionsGroupingTestOutputHelper.CreateDecorator(_testOutputHelper, testManifest);
            configuration.TestOutputHelper = _testOutputHelper;
        }

        try
        {
            var testTask = UITestExecutor.ExecuteOrchardCoreTestAsync(
                webApplicationInstanceFactory,
                testManifest,
                configuration);

            await Task.WhenAny(testTask, Task.Delay(timeout));

            if (!testTask.IsCompleted)
            {
                throw new TimeoutException($"The time allotted for the test ({timeout}) was exceeded.");
            }
        }
        finally
        {
            _testOutputHelper = originalTestOutputHelper;
            afterTest?.Invoke();
        }
    }
}
