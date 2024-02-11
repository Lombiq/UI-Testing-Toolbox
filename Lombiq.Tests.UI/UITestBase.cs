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
            await UITestExecutor.ExecuteOrchardCoreTestAsync(webApplicationInstanceFactory, testManifest, configuration);
        }
        finally
        {
            _testOutputHelper = originalTestOutputHelper;
            // This warning is a false positive as it is not considering the evaluation of the if statement above.
#pragma warning disable S2583 // Conditionally executed code should be reachable
            afterTest?.Invoke();
#pragma warning restore S2583 // Conditionally executed code should be reachable
        }
    }
}
