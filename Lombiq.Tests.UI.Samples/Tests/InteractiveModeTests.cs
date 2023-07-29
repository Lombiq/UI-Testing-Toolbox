using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

public class InteractiveModeTests : UITestBase
{
    public InteractiveModeTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Theory(Skip = "Use this to test to try out the interactive mode. This is not a real test you can run in CI."), Chrome]
    [SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped", Justification = "Only a demo.")]
    public Task SampleTest(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                await context.SignInDirectlyAsync();

                // One use-case of interactive mode is to look around in the admin dashboard and troubleshoot the
                // current settings without programmatically navigating there.
                await context.SignInDirectlyAndGoToDashboardAsync();
                await context.SwitchToInteractiveAsync();

                // Afterwards if you can still evaluate code as normal so `SignInDirectlyAndGoToDashboardAsync()` can be
                // inserted anywhere. Bare in mind, that it's safest to use it before code that's already going to
                // navigate away, like `GetCurrentUserNameAsync()` here. This ensures any manual navigation you did
                // won't affect the test. If that's not practical, you can also do your thing in a new tab and close it
                // before continuing the test.
                (await context.GetCurrentUserNameAsync()).ShouldNotBeNullOrWhiteSpace();
            },
            browser);

    [Theory, Chrome]
    public Task EnteringInteractiveModeShouldWait(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                var currentUrl = context.Driver.Url;

                await Task.WhenAll(
                    context.SwitchToInteractiveAsync(),
                    Task.Run(async () =>
                    {
                        // Ensure that the interactive mode polls for status at least once, so the arbitrary waiting
                        // actually works in a real testing scenario.
                        await Task.Delay(1000);

                        await context.ClickReliablyOnAsync(By.ClassName("interactive__continue"));
                    }));

                // Ensure that the info tab is closed and the control handed back to the last tab.
                context.Driver.Url.ShouldBe(currentUrl);
            },
            browser);
}
