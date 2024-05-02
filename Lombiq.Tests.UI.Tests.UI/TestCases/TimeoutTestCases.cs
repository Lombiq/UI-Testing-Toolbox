using Lombiq.Tests.UI.Services;
using Shouldly;

namespace Lombiq.Tests.UI.Tests.UI.TestCases;

public class TimeoutTestCases
{
    public static Task TestRunTimeoutShouldThrowAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync,
        Browser browser = default) =>
        Should.ThrowAsync(
            async () => await executeTestAfterSetupAsync(
                context => Task.Delay(TimeSpan.FromSeconds(1)),
                browser,
                configuration =>
                {
                    configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false;
                    configuration.MaxRetryCount = 0;

                    configuration.TimeoutConfiguration.TestRunTimeout = TimeSpan.FromMilliseconds(10);

                    return Task.CompletedTask;
                }),
            typeof(TimeoutException));
}
