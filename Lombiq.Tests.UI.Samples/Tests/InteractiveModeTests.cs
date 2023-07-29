using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
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
