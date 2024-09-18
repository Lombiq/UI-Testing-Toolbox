using Lombiq.Tests.UI.Extensions;
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

        // Used by many utilities to turn off ANSI escape sequences.
        Environment.SetEnvironmentVariable("NO_COLOR", "true");

        try
        {
            var testTask = UITestExecutor.ExecuteOrchardCoreTestAsync(
                webApplicationInstanceFactory,
                testManifest,
                configuration);
            var timeoutTask = Task.Delay(timeout);

            await Task.WhenAny(testTask, timeoutTask);

            if (timeoutTask.IsCompleted)
            {
                // If the EnterInteractiveModeAsync() extension method has been used, then timeout should be ignored to
                // make the debugging experience smoother. Note that EnterInteractiveModeAsync() should never be used in
                // committed tests.
                if (!ShortcutsUITestContextExtensions.InteractiveModeHasBeenUsed)
                {
                    throw new TimeoutException($"The time allotted for the test ({timeout}) was exceeded.");
                }

                await testTask;
            }

            // Since the timeout task is not yet completed but the Task.WhenAny has finished, the test task is done in
            // some way. So it's safe to await it here. It's also necessary to cleanly propagate any exceptions that may
            // have been thrown inside it.
            await testTask;
        }
        finally
        {
            _testOutputHelper = originalTestOutputHelper;
            afterTest?.Invoke();
        }
    }
}
