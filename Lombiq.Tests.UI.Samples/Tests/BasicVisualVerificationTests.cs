using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
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

    [Theory, Chrome]
    public Task VerifyBlogImage(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            context =>
            {
                context.SetViewportSize(CommonDisplayResolutions.HdPlus);
                context.HideScrollbar();
                var blogImageElementSelector = By.ClassName("field-name-blog-image");

                context.AssertVisualVerificationApproved(blogImageElementSelector, 0);
            },
            browser);

    // Checking that everything is OK with the branding of the navbar on the homepage.
    // For this magic we are using the ImageSharp.Compare package. You can find more info about it here:
    // https://github.com/Codeuctivity/ImageSharp.Compare
    [Theory, Chrome, Firefox]
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

                // Here we check that the rendered content visually equals the baseline image within a given error
                // percentage only for the platforms given by WithPlatforms() configuration. You can read more about
                // this in the AssertVisualVerificationApproved method documentation.
                context.AssertVisualVerificationApproved(navbarElementSelector, 0, configurator: configuration => configuration
                // With this configuration, we select the platforms to which the verification is applied.
                .WithPlatforms(PlatformID.Win32NT, PlatformID.Unix)
                // With this configuration, the PlatformID will be used as a suffix at the end of the baseline image
                // file name.
                .WithUsePlatformAsSuffix()
                // With this configuration, the browser name provided by selenium will be used as a suffix at the end of
                // the baseline image file name.
                .WithUseBrowserNameAsSuffix());
            },
            browser);

    // Checking that everything is OK with the homepage, just for fun.
    [Theory, Chrome, Firefox]
    public Task VerifyHomePage(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            context =>
            {
                context.SetViewportSize(CommonDisplayResolutions.HdPlus);
                // Here we hide the scrollbars to reach a better comparison result. If there are more operations after
                // visual verification and the scrollbars are required, then don't forget to restore with
                // context.RestoreHiddenScrollbar().
                context.HideScrollbar();

                // Here we don't need any element selector to validate the whole page.
                context.AssertVisualVerificationApproved(0, configurator: configuration => configuration
                .WithPlatforms(PlatformID.Win32NT, PlatformID.Unix)
                .WithUsePlatformAsSuffix()
                .WithUseBrowserNameAsSuffix());
            },
            browser);
}

// END OF TRAINING SECTION: Basic visual verification tests.
