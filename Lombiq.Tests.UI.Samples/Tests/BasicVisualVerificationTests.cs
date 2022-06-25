using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Drawing;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// In this basic test we will check visually the rendered content.
public class BasicVisualVerificationTests : UITestBase
{
    public BasicVisualVerificationTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Checking that everything is OK with the branding of the navbar on the homepage.
    // For this magic we are using the ImageSharp.Compare package. You can find more info about it here:
    // https://github.com/Codeuctivity/ImageSharp.Compare
    [Theory, Chrome]
    public Task VerifyNavbar(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            context =>
            {
                // Instead of using context.SetBrowserSize(...), which sets the outer size of the browser's window to
                // the given size, we are using context.SetViewportSize(...) here. This is because the window borders,
                // toolbars, tabs, and scroll bars usually have different sizes on different platforms/browsers, but we
                // want the same geometries of rendered content on all platforms.
                context.SetViewportSize(CommonDisplayResolutions.HdPlus);

                var navbarElementSelector = By.ClassName("navbar-brand");
                // We set the browser's window size, DPI, and scale settings explicitly to make the test environment
                // similar on every platform, but we have no control over the fonts. In this case, the selected block
                // element size, mainly height, depends on the font used on a given platform.
                // We don't know the exact size of the selected element, so we should select the interesting region for
                // reference and captured it as a bitmap and use it at the end. Without cropping the reference and
                // captured bitmap to the same size, ImageSharpCompare.CalcDiff(...) throws an
                // ImageSharpCompareException.
                var cropRegion = new Rectangle(0, 0, 1400, 23);

                // Here we check that the rendered content visually equals the reference image within a given error
                // percentage. You can read more about this in the AssertVisualVerificationApproved method documentation.
                context.AssertVisualVerificationApproved(
                    navbarElementSelector,
                    0,
                    cropRegion,
                    configurator: configuration => configuration.WithPlatforms(PlatformID.Win32NT));
            },
            browser);

    // Checking that everything is OK with the homepage, just for fun.
    [Theory, Chrome]
    public Task VerifyHomePage(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            context =>
            {
                context.SetViewportSize(CommonDisplayResolutions.HdPlus);
                // Here we hide the scrollbars to reach a better comparison result. If there are more operations after
                // visual verification and the scrollbars are required, then don't forget to restore with
                // context.RestoreHiddenScrollbar().
                context.HideScrollbar();

                // Here we need only the error percentage to validate the whole page.
                context.AssertVisualVerificationApproved(
                    0,
                    configurator: configuration => configuration.WithPlatforms(PlatformID.Win32NT));
            },
            browser);
}

// END OF TRAINING SECTION: Basic visual verification tests.
