using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
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

    // This is a very basic sample to check that the header image is what we expect and looks as we expect. For this
    // magic we are using the ImageSharp.Compare package. You can find more info about it here:
    // https://github.com/Codeuctivity/ImageSharp.Compare. This looks really simple, but there is some trap to comparing
    // block containers containing images like this. Take attention to reproducing the geometries, because the image
    // fits the container and the container size depends on the client area, so if the geometries are not exactly the
    // same, the test will fail. One more trap is the changes between browser versions, e.g. there was a change between
    // the Chrome version 67 and 68 in the image rendering. This caused that the rendered image looked similar, but
    // comparing pixel-by-pixel was different. You can investigate this or similar failure using the captured and
    // generated diff images under the path FailureDumps/<test-name>/Attempt <n>/DebugInformation/VisualVerification.
    // To update your baseline images after making changes to a visually checked element, remove the previous images
    // and run the test with maxRetryCount set to 0.
    [Fact]
    public Task VerifyBlogImage() =>
        ExecuteTestAfterSetupAsync(
            context =>
            {
                // Instead of using context.SetBrowserSize(...), which sets the outer size of the browser's window to
                // the given size, we are using context.SetViewportSize(...) here. This is because the window borders,
                // toolbars, tabs, and scroll bars usually have different sizes on different platforms/browsers, but we
                // want the same geometries of rendered content on all platforms.
                context.SetViewportSize(CommonDisplayResolutions.HdPlus);
                // Here we hide the scrollbars to get more control over the size of the client area. If there are more
                // operations after visual verification and the scrollbars are required, then don't forget to restore
                // with context.RestoreHiddenScrollbar().
                context.HideScrollbar();

                var blogImageElementSelector = By.ClassName("field-name-blog-image");

                // Here we check that the rendered content visually equals the baseline image within a given error
                // percentage. You can read more about this in the AssertVisualVerificationApproved method documentation.
                context.AssertVisualVerificationApproved(blogImageElementSelector, 0);
            });

    // Checking that everything is OK with the branding of the navbar on the homepage. If you want to visually validate
    // text content on different platforms (like Windows or Linux) or browsers, it can cause surprises too. The reason
    // is the different rendering of text on each platform, but it can occur between different Linux distributions too.
    // Here: https://pandasauce.org/post/linux-fonts/ you can find a good summary about this from 2019, but still valid
    // in 2022.
    [Theory, Chrome, Edge]
    public Task VerifyNavbar(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            context =>
            {
                context.SetViewportSize(CommonDisplayResolutions.HdPlus);

                // It's recommended to check the most specific element suitable, not the whole page. However, you can
                // also do that, just don't specify any elementSelector when calling AssertVisualVerificationApproved().
                // Be mindful of elements that may change on their own, like a copyright notice in the footer that
                // includes the current year.
                var navbarElementSelector = By.ClassName("navbar-brand");

                // Here we check that the rendered content visually equals the baseline image within a given error
                // percentage using different baseline image on each platform and browser. You can read more about
                // this in the AssertVisualVerificationApproved method documentation.
                context.AssertVisualVerificationApproved(
                    navbarElementSelector,
                    0,
                    configurator: configuration =>
                        configuration
                            // These configurations below are to generate/use different baseline images on each
                            // platform/browser.
                            .WithUsePlatformAsSuffix()
                            .WithUseBrowserNameAsSuffix());
            },
            browser);
}

// END OF TRAINING SECTION: Basic visual verification tests.
// NEXT STATION: Head over to Tests/TenantTests.cs.
