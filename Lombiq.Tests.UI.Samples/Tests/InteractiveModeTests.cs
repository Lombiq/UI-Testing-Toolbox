using Lombiq.Tests.UI.Extensions;
using Shouldly;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// Sometimes you want to debug the test session and assuming direct control would be nice. But you can't just drop a
// breakpoint in the test, since the Orchard Core webapp and the test are the same process so it would pause both. The
// `context.SwitchToInteractiveAsync()` extension method opens a new tab with info about the interactive mode and then
// causes the test thread to wait until you've clicked on the "Continue Test" button in this tab. During that time you
// can interact with OC as if it was a normal execution.
// Note: this extension depends on Lombiq.Tests.UI.Shortcuts being enabled in your OC app.
public class InteractiveModeTests : UITestBase
{
    public InteractiveModeTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // If you want to try it out yourself, just remove the "Skip" parameter and run this test.
    [SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped", Justification = "Only a demo.")]
    [Fact(Skip = "Use this to test to try out the interactive mode. This is not a real test you can run in CI.")]
    public Task SampleTest() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                await context.SignInDirectlyAsync();

                // One use-case of interactive mode is to look around in the admin dashboard and troubleshoot the
                // current settings without programmatically navigating there.
                await context.SignInDirectlyAndGoToDashboardAsync();
                await context.SwitchToInteractiveAsync(cancellationToken: context.Configuration.TestCancellationToken);

                // Afterwards if you can still evaluate code as normal so `SignInDirectlyAndGoToDashboardAsync()` can be
                // inserted anywhere. Bare in mind, that it's safest to use it before code that's already going to
                // navigate away, like `GetCurrentUserNameAsync()` here. This ensures any manual navigation you did
                // won't affect the test. If that's not practical, you can also do your thing in a new tab and close it
                // before continuing the test.
                (await context.GetCurrentUserNameAsync()).ShouldNotBeNullOrWhiteSpace();

                // Interactive mode only makes sense if you can see the browser, so below we make sure it's not running
                // in headless mode.
            },
            configuration => configuration.BrowserConfiguration.Headless = false);
}

// END OF TRAINING SECTION: Interactive mode.
// NEXT STATION: Head over to Tests/SecurityScanningTests.cs.
