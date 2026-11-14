using Lombiq.Tests.UI.Services;
using Shouldly;

namespace Lombiq.Tests.UI.Tests.UI.TestCases;

public static class TimeoutTestCases
{
    [Obsolete($"Use the overload that accepts {nameof(UITestBase)}")]
    public static Task TestRunTimeoutShouldThrowAsync(
        ExecuteTestAfterSetupWithoutBrowserAsync executeTestAfterSetupWithoutBrowserAsync,
        Browser browser = Browser.None) =>
        Should.ThrowAsync(
            async () => await executeTestAfterSetupWithoutBrowserAsync(
                context => Task.Delay(TimeSpan.FromSeconds(1), context.Configuration.TestCancellationToken),
                configuration =>
                {
                    configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false;
                    configuration.MaxRetryCount = 0;
                    configuration.TestDumpConfiguration.CreateTestDump = false;
                    configuration.SetupConfiguration.SetupOperation = context => Task.FromResult(new Uri("http://example.com"));

                    configuration.TimeoutConfiguration.TestRunTimeout = TimeSpan.FromMilliseconds(10);

                    return Task.CompletedTask;
                }),
            typeof(TimeoutException));

    public static Task TestRunTimeoutShouldThrowAsync(UITestBase fixture) =>
        Should.ThrowAsync<TimeoutException>(async () => await fixture
            .AddSyncConfiguration(configuration =>
            {
                configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false;
                configuration.MaxRetryCount = 0;
                configuration.TestDumpConfiguration.CreateTestDump = false;
                configuration.SetupConfiguration.SetupOperation = _ => Task.FromResult(new Uri("http://example.com"));

                configuration.TimeoutConfiguration.TestRunTimeout = TimeSpan.FromMilliseconds(10);
            })
            .RunAsync(context => Task.Delay(TimeSpan.FromSeconds(1), context.Configuration.TestCancellationToken)));
}
