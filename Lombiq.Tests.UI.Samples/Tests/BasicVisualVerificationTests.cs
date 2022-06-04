using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using System.IO;
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
    // https://github.com/Codeuctivity/ImageSharp.Compare/blob/1.2.11/README.md.
    [Theory, Chrome]
    public Task VerifyNavbar(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            context =>
            {
                // Instead of using context.SetBrowserSize(...), witch is sets the outer size of the browsers window to
                // the given size, we are using context.SetViewportSize(...) here. This is because the windows borders,
                // toolbars, tabs and scroll bars usually have different sizes on different platforms/browsers, but we
                // want the same geometries of rendered content on all platforms.
                context.SetViewportSize(CommonDisplayResolutions.HdPlus);

                var testDumpFolderName = nameof(VerifyNavbar);
                var testTempRootFolder = FileSystemHelper.EnsureDirectoryExists(Path.Combine("Temp", testDumpFolderName));

                // We take a screenshot and save it to a temporary folder, and append the temporary file to the failure
                // dump.
                var fullScreenImageFileName = "navbar_full_screen.bmp";
                var fullScreenImageTempFileName = Path.Combine(testTempRootFolder, fullScreenImageFileName);
                context.TakeScreenshot(fullScreenImageTempFileName);
                context.AppendFailureDump(
                    Path.Combine(testDumpFolderName, fullScreenImageFileName),
                    context => Task.FromResult((Stream)File.OpenRead(fullScreenImageTempFileName)));

                var navbarElementSelector = By.ClassName("navbar-brand");
                // We set the browser's window size, DPI, and scale settings explicitly to make the test environment
                // similar on every platform, but we have no control over the fonts. In this case, the selected block
                // element size, mainly height, is depends on the font used on a given platform.
                // We don't know the exact size of the selected element, so we should select the interesting region for
                // reference and be captured as bitmap, and use it at the end. Without cropping the reference and
                // captured bitmap to the same size, the ImageSharpCompare.CalcDiff(...) throws an
                // ImageSharpCompareException.
                var cropRegion = new Rectangle(0, 0, 1400, 23);

                // First, we take a screenshot of thr element area. This will be compared to a reference image that we
                // prepared before.
                using var navbarImage = context.TakeElementScreenshot(navbarElementSelector)
                    .ToImageSharpImage()
                    .ShouldNotBeNull();
                var navbarImageFileName = "navbar_captured.bmp";

                SaveImageToTempAndAppendToFailureDump(
                    navbarImage,
                    context,
                    Path.Combine(testTempRootFolder, navbarImageFileName),
                    Path.Combine(testDumpFolderName, navbarImageFileName));

                // Checking the size of captured image.
                navbarImage.Width
                    .ShouldBeGreaterThanOrEqualTo(cropRegion.Left + cropRegion.Width);
                navbarImage.Height
                    .ShouldBeGreaterThanOrEqualTo(cropRegion.Top + cropRegion.Height);

                // Here we crop the RoI.
                navbarImage.Mutate(imageContext => imageContext.Crop(cropRegion));

                var navbarImageCroppedFileName = "navbar_captured_cropped.bmp";
                // We save it to a temporary folder, and append the temporary file to the failure dump.
                SaveImageToTempAndAppendToFailureDump(
                    navbarImage,
                    context,
                    Path.Combine(testTempRootFolder, navbarImageCroppedFileName),
                    Path.Combine(testDumpFolderName, navbarImageCroppedFileName));

                // Then we load the reference image. This is what we are expecting.
                using var referenceImage = typeof(BasicVisualVerificationTests).Assembly
                    .GetResourceImageSharpImage("Lombiq.Tests.UI.Samples.Assets.navbar.dib")
                    .ShouldNotBeNull();
                var referenceImageFileName = "navbar_reference.bmp";

                SaveImageToTempAndAppendToFailureDump(
                    referenceImage,
                    context,
                    Path.Combine(testTempRootFolder, referenceImageFileName),
                    Path.Combine(testDumpFolderName, referenceImageFileName));

                // Here we crop the RoI.
                referenceImage.Mutate(imageContext => imageContext.Crop(cropRegion));

                var referenceImageCroppedFileName = "navbar_reference_cropped.bmp";
                // Just like above, save and append it to the failure dump.
                SaveImageToTempAndAppendToFailureDump(
                    referenceImage,
                    context,
                    Path.Combine(testTempRootFolder, referenceImageCroppedFileName),
                    Path.Combine(testDumpFolderName, referenceImageCroppedFileName));

                // At this point, we have reference and captured images too.
                // Creating diff image is not required, but it can be very useful to investigate failing tests.
                // You can read more about how diff created here:
                // https://github.com/Codeuctivity/ImageSharp.Compare/blob/1.2.11/ImageSharpCompare/ImageSharpCompare.cs#L258.
                // So lets create it and append it to failure dump.
                using var diffImage = referenceImage
                    .CalcDiffImage(navbarImage)
                    .ShouldNotBeNull();
                var diffImageFileName = "navbar_diff.bmp";

                SaveImageToTempAndAppendToFailureDump(
                    diffImage,
                    context,
                    Path.Combine(testTempRootFolder, diffImageFileName),
                    Path.Combine(testDumpFolderName, diffImageFileName));

                // Now we are one step away from the end. Here we create a statistical summary of the differences
                // between the captured and the reference image. In the end, the lower values are better.
                // You can read more about how these statistical calculations are created here:
                // https://github.com/Codeuctivity/ImageSharp.Compare/blob/1.2.11/ImageSharpCompare/ImageSharpCompare.cs#L143.
                var diff = referenceImage
                    .CompareTo(navbarImage);
                var diffLogFileName = "navbar_diff.log";
                var diffLogTempFileName = Path.Combine(testTempRootFolder, diffLogFileName);

                File.WriteAllText(
                    diffLogTempFileName,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"
navbar: calculated differences:
    absoluteError={0},
    meanError={1},
    pixelErrorCount={2},
    pixelErrorPercentage={3}",
                        diff.AbsoluteError,
                        diff.MeanError,
                        diff.PixelErrorCount,
                        diff.PixelErrorPercentage));
                context.AppendFailureDump(
                    Path.Combine(testDumpFolderName, diffLogFileName),
                    context => Task.FromResult((Stream)File.OpenRead(diffLogTempFileName)));

                // All the stuff above are made for this comparison. Here we check that, the erroneous pixels percentage
                // is less than the given threshold.
                diff.PixelErrorPercentage.ShouldBeLessThan(8);
            },
            browser);

    private static void SaveImageToTempAndAppendToFailureDump(
        Image image,
        UITestContext context,
        string imageTempFileName,
        string imageDumpFileName)
    {
        image.SaveAsBmp(imageTempFileName);
        context.AppendFailureDump(
            imageDumpFileName,
            context => Task.FromResult((Stream)File.OpenRead(imageTempFileName)));
    }
}

// END OF TRAINING SECTION: Basic visual tests.
