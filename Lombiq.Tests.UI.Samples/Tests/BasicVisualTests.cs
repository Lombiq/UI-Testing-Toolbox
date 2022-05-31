using Lombiq.Tests.UI.Attributes;
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
public class BasicVisualTests : UITestBase
{
    public BasicVisualTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Checking that everything is OK with the branding on navbar of homepage.
    // For this magic we are using ImageSharp.Compare package. You can find more info about it here:
    // https://github.com/Codeuctivity/ImageSharp.Compare/blob/1.2.11/README.md.
    [Theory, Chrome]
    public Task AnonymousHomePageShouldExist(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            context =>
            {
                var navbarElementSelector = By.ClassName("navbar-brand");
                // We set the outer window size at the beginning of the test, but not the inner size of the window, and
                // the element selected by the navbarElementSelector is a div, so it depends/fits the browser's inner
                // size. We don't know the exact inner size of the browser, so we should select the interested region
                // for the reference and the captured bitmaps, and use it at the end.
                var cropOptions = new ResizeOptions
                {
                    // RoI size.
                    Size = new Size(1400, 23),
                    // We want crop.
                    Mode = ResizeMode.Crop,
                    // Sets the upper left corner for crop operation.
                    CenterCoordinates = PointF.Empty,
                };

                // First we take a screenshot of element area. This will be compared to a reference image what we
                // prepared before.
                using var navbarImage = context.TakeElementScreenshot(navbarElementSelector)
                    .ToImageSharpImage()
                    .ShouldNotBeNull();
                var canvasImageTempFileName = "Temp/navbar_captured.bmp";

                navbarImage.SaveAsBmp("Temp/navbar_captured_full.bmp");
                // Here we crop the RoI. Don't be confused about imageContext.Resize(...) remember, we selected
                // ResizeMode.Crop above, in cropOptions.
                navbarImage.Mutate(imageContext => imageContext.Resize(cropOptions));
                // We save it to a temporary folder, and append temporary file to failure dump.
                navbarImage.SaveAsBmp(canvasImageTempFileName);
                context.AppendFailureDump(
                    "navbar_captured.bmp",
                    context => Task.FromResult((Stream)File.OpenRead(canvasImageTempFileName)));

                // Then we load the reference image. This is what we are expecting.
                using var referenceImage = typeof(BasicVisualTests).Assembly
                    .GetResourceImageSharpImage("Lombiq.Tests.UI.Samples.Assets.navbar.dib")
                    .ShouldNotBeNull();
                var referenceImageTempFileName = "Temp/navbar_reference.bmp";

                referenceImage.SaveAsBmp("Temp/navbar_reference_full.bmp");
                // Here we crop the RoI. Don't be confused about imageContext.Resize(...) remember, we selected
                // ResizeMode.Crop above, in cropOptions.
                referenceImage.Mutate(imageContext => imageContext.Resize(cropOptions));
                // Just like above, save and append it to failure dump.
                referenceImage.SaveAsBmp(referenceImageTempFileName);
                context.AppendFailureDump(
                    "navbar_reference.bmp",
                    context => Task.FromResult((Stream)File.OpenRead(referenceImageTempFileName)));

                // At this point, we have reference and captured images too.
                // Creating diff image is not required, but it can be very useful to investigate failing tests.
                // You can read more about how diff created here:
                // https://github.com/Codeuctivity/ImageSharp.Compare/blob/1.2.11/ImageSharpCompare/ImageSharpCompare.cs#L258.
                // So lets create it and append it to failure dump.
                using var diffImage = referenceImage
                    .CalcDiffImage(navbarImage);
                var diffImageTempFileName = "Temp/navbar_diff.bmp";

                diffImage.ShouldNotBeNull()
                    .SaveAsBmp(diffImageTempFileName);
                context.AppendFailureDump(
                    "navbar_diff.bmp",
                    context => Task.FromResult((Stream)File.OpenRead(diffImageTempFileName)));

                // Now we are one step far from the end. Here we create a statistical summary of differences between the
                // captured and the reference image. At the end, the lower values are better.
                // You can read more about how this statistical calculations are created here:
                // https://github.com/Codeuctivity/ImageSharp.Compare/blob/1.2.11/ImageSharpCompare/ImageSharpCompare.cs#L143.
                var diff = referenceImage
                    .CompareTo(navbarImage);
                var diffLogTempFileName = "Temp/navbar_diff.log";

                File.WriteAllText(
                    diffLogTempFileName,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"navbar: calculated differences:
    absoluteError={0},
    meanError={1},
    pixelErrorCount={2},
    pixelErrorPercentage={3}
            ",
                        diff.AbsoluteError,
                        diff.MeanError,
                        diff.PixelErrorCount,
                        diff.PixelErrorPercentage));
                context.AppendFailureDump(
                    "navbar_diff.log",
                    context => Task.FromResult((Stream)File.OpenRead(diffLogTempFileName)));
                // All the stuff above are made for this comparison. Here we check that, the erroneous pixels percentage
                // is less than a threshold.
                diff.PixelErrorPercentage.ShouldBeLessThan(10);

                return Task.CompletedTask;
            },
            browser);
}

// END OF TRAINING SECTION: Basic visual tests.
