using Lombiq.Tests.UI.Extensions;
using OpenQA.Selenium;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

public class FrontendTests : FrontendUITestBase
{
    public FrontendTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // The interesting details are in FrontendUITestBase, here we just show that you can freely interact with the pages
    // served by frontend server the same way as usual. In this case we have an HTTP file server, so you can navigate
    // these directories and files. If we had a large client application that interacts with the headless OC in the
    // backend, we would be able to do something more interesting but that's outside the scope of this demo.
    [Fact]
    public Task FrontendServerShouldStartWithTest() =>
        ExecuteFrontendTestAfterSetupAsync(
            async context =>
            {
                // Don't forget that if you want to interact with the frontend manually, you can just switch the context
                // back to the back end and use the interactive mode extension method.
                //// context.SwitchToBackend();
                //// await context.SwitchToInteractiveAsync();

                await context.ClickReliablyOnAsync(By.LinkText("App_Data/"));
                await context.ClickReliablyOnAsync(By.LinkText("Sites/"));
                await context.ClickReliablyOnAsync(By.LinkText("Default/"));
                await context.ClickReliablyOnAsync(By.LinkText("DataProtection-Keys/"));
                await context.ClickReliablyOnAsync(By.XPath("//td/a[contains(@href, '.xml')]"));
            },
            browser: default,
            configuration =>
            {
                // Since this server is not our code, we should disable HTML validation.
                configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false;
                return Task.CompletedTask;
            });
}

// END OF TRAINING SECTION: Test headless Orchard Core with a frontend subprocess.
// NEXT STATION: Head over to Tests/JavascriptTests.cs.
