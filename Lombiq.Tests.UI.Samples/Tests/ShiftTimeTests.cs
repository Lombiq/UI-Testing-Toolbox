using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// When you enable the "Shift Time - Shortcuts - Lombiq UI Testing Toolbox" feature, it replaces Orchard Core's stock
// ICLock implementation with the custom TimeShiftingClock class. You can use the
// ~/Lombiq.Tests.UI.Samples/TimeShift/Set?days=... action to update the TimeShiftingClock.Shift property for the current
// tenant, which will trick any service that uses IClock into thinking you are in the future. This can be used to test
// features that can expire, such as a limited-time product discount in a web store, without having to wait.
public class ShiftTimeTests : UITestBase
{
    public ShiftTimeTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task TimeShouldUpdate() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                // You can enable the feature from recipe too, but if you only need it for specific tests, then you can
                // use this extension method.
                await context.EnableTimeShiftingAsync();

                // Create a simple widget that shows the current date, so we can compare the effects of this feature.
                await context.SignInDirectlyAsync();
                await context.GoToAdminRelativeUrlAsync(
                    "/Contents/ContentTypes/LiquidWidget/Create?returnUrl=%2FAdmin%2FLayers&" +
                    "LayerMetadata.Zone=Content&LayerMetadata.Position=1");
                await context.FillInCodeMirrorEditorWithRetriesAsync(
                    By.CssSelector(".CodeMirror.cm-s-default"),
                    "<div id=\"now\">{{ \"now\" | utc | date: \"%Y-%m-%d %H:%M\" }}</div>");
                await context.ClickReliablyOnAsync(By.ClassName("publish"));

                var now = await GetNowAsync(context);

                // This extension method navigates to the action which sets the time offset. You can set it in terms of
                // days or seconds. Both accept fractions and negative values. If both days and seconds are set, they
                // are added together.
                await context.SetTimeShiftAsync(TimeSpan.FromDays(10));

                // Let's verify the date!
                var tenDaysFromNow = await GetNowAsync(context);
                tenDaysFromNow.DayOfYear.ShouldBe(now.AddDays(10).DayOfYear);
            });

    private static async Task<DateTime> GetNowAsync(UITestContext context)
    {
        await context.GoToHomePageAsync();
        return DateTime.Parse(context.Get(By.Id("now")).Text, CultureInfo.InvariantCulture);
    }
}

// END OF TRAINING SECTION: Testing time-dependent functionality.
// NEXT STATION: Head over to FrontendUITestBase.cs.
