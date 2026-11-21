using Lombiq.Tests.UI.Extensions;
using OpenQA.Selenium;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

public class FrontendTests : FrontendUITestBase
{
    // Since this server is not our code, we should disable HTML validation.
    public FrontendTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper) =>
        AddConfiguration(configuration => configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false);

    // The interesting details are in FrontendUITestBase, here we just show that you can freely interact with the pages
    // served by frontend server the same way as usual. In this case we have an HTTP file server, so you can navigate
    // these directories and files. If we had a large client application that interacts with the headless OC in the
    // backend, we would be able to do something more interesting but that's outside the scope of this demo.
    [Fact]
    public Task FrontendServerShouldStartWithTest() =>
        RunAsync(async context =>
        {
            // Don't forget that if you want to interact with the frontend manually, you can use the interactive
            // mode extension method.
            //// await context.SwitchToInteractiveAsync();

            await context.ClickReliablyOnAsync(By.LinkText("App_Data/"));
            await context.ClickReliablyOnAsync(By.LinkText("Sites/"));
            await context.ClickReliablyOnAsync(By.LinkText("Default/"));
            await context.ClickReliablyOnAsync(By.LinkText("DataProtection-Keys/"));
            await context.ClickReliablyOnAsync(By.XPath("//td/a[contains(@href, '.xml')]"));
        });
}

// END OF TRAINING SECTION: Test headless Orchard Core with a frontend subprocess.
// NEXT STATION: Head over to Tests/SqlQueryMonitoringTests.cs.
