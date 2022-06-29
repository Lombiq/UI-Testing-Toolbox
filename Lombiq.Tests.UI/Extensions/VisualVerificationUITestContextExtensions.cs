using Atata;
using Codeuctivity.ImageSharpCompare;
using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lombiq.Tests.UI.Extensions;

public static class VisualVerificationUITestContextExtensions
{
    private const string ConditionLessThenOrEqualTo = "less than or equal to";
    private const string HintFailureDumpItemAlreadyExists = $@"
Hint: You can use the configurator callback of {nameof(AssertVisualVerificationApproved)} and {nameof(AssertVisualVerification)}
to customize the name of the dump item.";

    /// <summary>
    /// Compares the baseline image and screenshot of the whole page.
    /// <see cref="AssertVisualVerificationApproved(UITestContext, By, double, Rectangle?, Action{VisualVerificationMatchApprovedConfiguration})"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    /// <exception cref="VisualVerificationBaselineImageNotFoundException">
    /// If no baseline image found under project path.
    /// </exception>
    [VisualVerificationApprovedMethod]
    public static void AssertVisualVerificationApproved(
        this UITestContext context,
        double pixelErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualVerificationMatchApprovedConfiguration> configurator = null) =>
        context.AssertVisualVerificationApproved(
            By.TagName("body"),
            pixelErrorPercentageThreshold,
            regionOfInterest,
            configurator);

    // This is because the long method signature.
#pragma warning disable S103 // Lines should not be too long
    /// <summary>
    /// Compares the baseline image and screenshot of the element given by <paramref name="elementSelector"/>.
    /// <see cref="AssertVisualVerificationApproved(UITestContext, IWebElement, double, Rectangle?, Action{VisualVerificationMatchApprovedConfiguration})"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    /// <exception cref="VisualVerificationBaselineImageNotFoundException">
    /// If no baseline image found under project path.
    /// </exception>
#pragma warning restore S103 // Lines should not be too long
    [VisualVerificationApprovedMethod]
    public static void AssertVisualVerificationApproved(
        this UITestContext context,
        By elementSelector,
        double pixelErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualVerificationMatchApprovedConfiguration> configurator = null) =>
        context.AssertVisualVerificationApproved(
            elementSelector,
            (approvedContext, diff) =>
                AssertInternal(
                    approvedContext,
                    (actual, expected) => actual <= expected,
                    diff.PixelErrorPercentage,
                    pixelErrorPercentageThreshold,
                    nameof(diff.PixelErrorPercentage),
                    ConditionLessThenOrEqualTo),
            regionOfInterest,
            configurator);

    /// <summary>
    /// <para>
    /// Compares the baseline image and screenshot of the element. The pixel error percentage should be less than or
    /// equal to the given <paramref name="pixelErrorPercentageThreshold"/>.
    /// The baseline image is automatically loaded from assembly resource, if it doesn't exist then from the
    /// project path based on <see cref="VisualVerificationMatchApprovedConfiguration"/> - it can be configured over
    /// <paramref name="configurator"/> -, if the baseline image doesn't exist, a new one will be created based on the
    /// element's screenshot, and an <see cref="VisualVerificationBaselineImageNotFoundException"/> will be thrown. The
    /// baseline image path is generated from the first method name - from the call stack which is not annotated with
    /// <see cref="VisualVerificationApprovedMethodAttribute"/> - and the source file name and path, where the method is.
    /// </para>
    /// <para>
    /// In case when you want visually validate elements that contain text on multiple platforms/browsers then keep in
    /// mind that, the font rendering results different visuals. This means that you should use different baseline
    /// images for each platform. You can generate baseline images for each platform with locally build and run tests
    /// and follow the instructions in <see cref="VisualVerificationBaselineImageNotFoundException"/> or running on a CI
    /// and using the image dumped on failure. If you need different baseline images on each platfrom/browser you can
    /// configure suffixes as needed using <paramref name="configurator"/>.
    /// </para>
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="element">Target element.</param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    /// <exception cref="VisualVerificationBaselineImageNotFoundException">
    /// If no baseline image found under project path.
    /// </exception>
    [VisualVerificationApprovedMethod]
    public static void AssertVisualVerificationApproved(
        this UITestContext context,
        IWebElement element,
        double pixelErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualVerificationMatchApprovedConfiguration> configurator = null) =>
        context.AssertVisualVerificationApproved(
            element,
            (approvedContext, diff) =>
                AssertInternal(
                    approvedContext,
                    (actual, expected) => actual <= expected,
                    diff.PixelErrorPercentage,
                    pixelErrorPercentageThreshold,
                    nameof(diff.PixelErrorPercentage),
                    ConditionLessThenOrEqualTo),
            regionOfInterest,
            configurator);

    /// <summary>
    /// Compares the baseline image and screenshot of the whole page.
    /// <see cref="AssertVisualVerification(UITestContext, By, Bitmap, double, Rectangle?, Action{VisualMatchConfiguration})"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="baseline">The baseline image.</param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    public static void AssertVisualVerification(
        this UITestContext context,
        Bitmap baseline,
        double pixelErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualMatchConfiguration> configurator = null) =>
        context.AssertVisualVerification(
            By.TagName("body"),
            baseline,
            pixelErrorPercentageThreshold,
            regionOfInterest,
            configurator);

    /// <summary>
    /// Compares the baseline image and screenshot of the element given by <paramref name="elementSelector"/>.
    /// <see cref="AssertVisualVerification(UITestContext, IWebElement, Bitmap, double, Rectangle?, Action{VisualMatchConfiguration})"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="baseline">The baseline image.</param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    public static void AssertVisualVerification(
        this UITestContext context,
        By elementSelector,
        Bitmap baseline,
        double pixelErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualMatchConfiguration> configurator = null) =>
        context.AssertVisualVerification(
            elementSelector,
            baseline,
            diff =>
                AssertInternal(
                    (actual, expected) => actual <= expected,
                    diff.PixelErrorPercentage,
                    pixelErrorPercentageThreshold,
                    nameof(diff.PixelErrorPercentage),
                    ConditionLessThenOrEqualTo),
            regionOfInterest,
            configuration =>
            {
                configurator?.Invoke(configuration);
                configuration.WithFileNameSuffix(
                    new[]
                    {
                            elementSelector.ToString().MakeFileSystemFriendly(),
                            configuration.FileNameSuffix,
                    }
                    .JoinNotNullOrEmpty("-")
                );
            });

    /// <summary>
    /// <para>
    /// Compares the baseline image and screenshot of the <paramref name="element"/>. The pixel error percentage should
    /// be less than or equal to the given <paramref name="pixelErrorPercentageThreshold"/>.
    /// </para>
    /// <para>
    /// In case when you want visually validate elements that contain text on multiple platforms/browsers then keep in
    /// mind that, the font rendering results different visuals. This means that you should use different baseline
    /// images for each platform.
    /// </para>
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="element">The target element.</param>
    /// <param name="baseline">The baseline image.</param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    public static void AssertVisualVerification(
        this UITestContext context,
        IWebElement element,
        Bitmap baseline,
        double pixelErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualMatchConfiguration> configurator = null) =>
        context.AssertVisualVerification(
            element,
            baseline,
            diff =>
                AssertInternal(
                    (actual, expected) => actual <= expected,
                    diff.PixelErrorPercentage,
                    pixelErrorPercentageThreshold,
                    nameof(diff.PixelErrorPercentage),
                    ConditionLessThenOrEqualTo),
            regionOfInterest,
            configurator);

    [VisualVerificationApprovedMethod]
    private static void AssertVisualVerificationApproved(
        this UITestContext context,
        By elementSelector,
        Action<VisualVerificationMatchApprovedContext, ICompareResult> comparator,
        Rectangle? regionOfInterest = null,
        Action<VisualVerificationMatchApprovedConfiguration> configurator = null) =>
        context.AssertVisualVerificationApproved(
            context.Get(elementSelector),
            comparator,
            regionOfInterest,
            configuration =>
            {
                configurator?.Invoke(configuration);
                configuration.WithFileNameSuffix(
                    new[]
                    {
                        elementSelector.ToString().MakeFileSystemFriendly(),
                        configuration.FileNameSuffix,
                    }
                    .JoinNotNullOrEmpty("-")
                );
            });

    [VisualVerificationApprovedMethod]
    private static void AssertVisualVerificationApproved(
        this UITestContext context,
        IWebElement element,
        Action<VisualVerificationMatchApprovedContext, ICompareResult> comparator,
        Rectangle? regionOfInterest = null,
        Action<VisualVerificationMatchApprovedConfiguration> configurator = null)
    {
        var configuration = new VisualVerificationMatchApprovedConfiguration();
        configurator?.Invoke(configuration);

        var stackTrace = new EnhancedStackTrace(new StackTrace(fNeedFileInfo: true))
            .Where(frame => frame.MethodInfo.MethodBase != null && !IsCompilerGenerated(frame));
        var testFrame = stackTrace
            .FirstOrDefault(frame => !IsVisualVerificationMethod(frame));

        if (testFrame != null && configuration.StackOffset > 0)
        {
            testFrame = stackTrace
                .Reverse()
                .TakeWhile(frame => frame.MethodInfo.MethodBase != testFrame.MethodInfo.MethodBase)
                .TakeLast(configuration.StackOffset)
                .FirstOrDefault();
        }

        if (testFrame == null)
        {
            throw new VisualVerificationCallerMethodNotFoundException();
        }

        var approvedContext = new VisualVerificationMatchApprovedContext
        {
            ModuleName = testFrame.MethodInfo.DeclaringType.Name,
            MethodName = testFrame.MethodInfo.Name,
            BrowserName = context.Driver.As<IHasCapabilities>().Capabilities.GetCapability("browserName") as string,
        };

        approvedContext.BaselineFileName = configuration.BaselineFileNameFormatter(configuration, approvedContext);

        // Try loading baseline image from embedded resources first.
        approvedContext.BaselineResourceName = $"{testFrame.MethodInfo.DeclaringType.Namespace}.{approvedContext.BaselineFileName}.png";
        var baselineImage = testFrame.MethodInfo.DeclaringType.Assembly
            .TryGetResourceBitmap(approvedContext.BaselineResourceName);

        if (baselineImage == null)
        {
            // Then if no resource exists, try load baseline image from source.
            if (string.IsNullOrEmpty(testFrame.GetFileName()))
            {
                using var suggestedImage = context.TakeElementScreenshot(element);

                var suggestedImageFileName = $"{approvedContext.BaselineFileName}.png";

                context.AppendFailureDump(
                    Path.Combine(
                        VisualVerificationMatchNames.DumpFolderName,
                        suggestedImageFileName),
                    suggestedImage.Clone(new Rectangle(Point.Empty, suggestedImage.Size), suggestedImage.PixelFormat),
                    messageIfExists: HintFailureDumpItemAlreadyExists);

                throw new VisualVerificationSourceInformationNotAvailableException(
                    $"Source information not available, make sure you are compiling with full debug information."
                    + $" Frame: {testFrame.MethodInfo.DeclaringType.Name}.{testFrame.MethodInfo.Name}."
                    + $" The suggested baseline image was added to the failure dump as {suggestedImageFileName}");
            }

            approvedContext.ModuleDirectory = Path.GetDirectoryName(testFrame.GetFileName());
            approvedContext.BaselineImagePath = Path.Combine(
                approvedContext.ModuleDirectory,
                $"{approvedContext.BaselineFileName}.png");

            if (!File.Exists(approvedContext.BaselineImagePath))
            {
                using var suggestedImage = context.TakeElementScreenshot(element);

                suggestedImage.Save(approvedContext.BaselineImagePath, ImageFormat.Png);

                // Appending suggested baseline image to failure dump too.
                context.AppendFailureDump(
                    Path.Combine(
                        VisualVerificationMatchNames.DumpFolderName,
                        $"{approvedContext.BaselineFileName}.png"),
                    suggestedImage.Clone(new Rectangle(Point.Empty, suggestedImage.Size), suggestedImage.PixelFormat),
                    messageIfExists: HintFailureDumpItemAlreadyExists);

                throw new VisualVerificationBaselineImageNotFoundException(approvedContext.BaselineImagePath);
            }

            baselineImage = (Bitmap)Image.FromFile(approvedContext.BaselineImagePath);
        }

        try
        {
            context.AssertVisualVerification(
                element,
                baselineImage,
                diff => comparator(approvedContext, diff),
                regionOfInterest,
                cfg => cfg.WithFileNamePrefix(approvedContext.BaselineFileName)
                    .WithFileNameSuffix(string.Empty));
        }
        finally
        {
            baselineImage?.Dispose();
        }
    }

    private static void AssertVisualVerification(
        this UITestContext context,
        By elementSelector,
        Bitmap baseline,
        Action<ICompareResult> comparator,
        Rectangle? regionOfInterest = null,
        Action<VisualMatchConfiguration> configurator = null) =>
        context.AssertVisualVerification(
            context.Get(elementSelector),
            baseline,
            comparator,
            regionOfInterest,
            configurator);

    private static void AssertVisualVerification(
        this UITestContext context,
        IWebElement element,
        Bitmap baseline,
        Action<ICompareResult> comparator,
        Rectangle? regionOfInterest = null,
        Action<VisualMatchConfiguration> configurator = null)
    {
        var configuration = new VisualMatchConfiguration();
        configurator?.Invoke(configuration);

        var cropRegion = regionOfInterest ?? new Rectangle(0, 0, baseline.Width, baseline.Height);

        // We take a full-page screenshot before validating. It will be appended to the failure dump later. This is
        // useful to investigate validation errors.
        using var fullScreenImage = context.TakeFullPageScreenshot();

        // We take a screenshot of the element area. This will be compared to a baseline image.
        using var elementImageOriginal = context.TakeElementScreenshot(element)
            .ToImageSharpImage()
            .ShouldNotBeNull();

        // Checking the size of captured image.
        elementImageOriginal.Width
            .ShouldBeGreaterThanOrEqualTo(cropRegion.Left + cropRegion.Width);
        elementImageOriginal.Height
            .ShouldBeGreaterThanOrEqualTo(cropRegion.Top + cropRegion.Height);

        using var baselineImageOriginal = baseline
            .ToImageSharpImage();

        // Here we crop the regionOfInterest.
        using var baselineImageCropped = baselineImageOriginal.Clone();
        using var elementImageCropped = elementImageOriginal.Clone();

        baselineImageCropped.Mutate(imageContext => imageContext.Crop(cropRegion.ToImageSharpRectangle()));
        elementImageCropped.Mutate(imageContext => imageContext.Crop(cropRegion.ToImageSharpRectangle()));

        // At this point, we have baseline and captured images too.
        // Creating a diff image is not required, but it can be very useful to investigate failing tests.
        // You can read more about how diff created here:
        // https://github.com/Codeuctivity/ImageSharp.Compare/blob/2.0.46/ImageSharpCompare/ImageSharpCompare.cs#L303.
        // So lets create it now and append it to failure dump later.
        using var diffImage = baselineImageCropped
            .CalcDiffImage(elementImageCropped)
            .ShouldNotBeNull();

        // Now we are one step away from the end. Here we create a statistical summary of the differences
        // between the captured and the baseline image. In the end, the lower values are better.
        // You can read more about how these statistical calculations are created here:
        // https://github.com/Codeuctivity/ImageSharp.Compare/blob/2.0.46/ImageSharpCompare/ImageSharpCompare.cs#L218.
        var diff = baselineImageCropped
            .CompareTo(elementImageCropped);

        try
        {
            comparator(diff);
        }
        catch
        {
            // Here we append all the relevant items to the failure dump to help the investigation.
            // The full-page screenshot
            context.AppendFailureDump(
                Path.Combine(
                    VisualVerificationMatchNames.DumpFolderName,
                    new[]
                    {
                        configuration.FileNamePrefix,
                        VisualVerificationMatchNames.FullScreenImageFileName,
                        configuration.FileNameSuffix,
                    }
                    .JoinNotNullOrEmpty("-")),
                fullScreenImage.Clone(new Rectangle(Point.Empty, fullScreenImage.Size), fullScreenImage.PixelFormat),
                messageIfExists: HintFailureDumpItemAlreadyExists);

            // The original element screenshot
            context.AppendFailureDump(
                Path.Combine(
                    VisualVerificationMatchNames.DumpFolderName,
                    new[]
                    {
                        configuration.FileNamePrefix,
                        VisualVerificationMatchNames.ElementImageFileName,
                        configuration.FileNameSuffix,
                    }
                    .JoinNotNullOrEmpty("-")),
                elementImageOriginal.Clone(),
                messageIfExists: HintFailureDumpItemAlreadyExists);

            // The original baseline image
            context.AppendFailureDump(
                Path.Combine(
                    VisualVerificationMatchNames.DumpFolderName,
                    new[]
                    {
                        configuration.FileNamePrefix,
                        VisualVerificationMatchNames.BaselineImageFileName,
                        configuration.FileNameSuffix,
                    }
                    .JoinNotNullOrEmpty("-")),
                baselineImageOriginal.Clone(),
                messageIfExists: HintFailureDumpItemAlreadyExists);

            // The cropped baseline image
            context.AppendFailureDump(
                Path.Combine(
                    VisualVerificationMatchNames.DumpFolderName,
                    new[]
                    {
                        configuration.FileNamePrefix,
                        VisualVerificationMatchNames.CroppedBaselineImageFileName,
                        configuration.FileNameSuffix,
                    }
                    .JoinNotNullOrEmpty("-")),
                baselineImageCropped.Clone(),
                messageIfExists: HintFailureDumpItemAlreadyExists);

            // The cropped element image
            context.AppendFailureDump(
                Path.Combine(
                    VisualVerificationMatchNames.DumpFolderName,
                    new[]
                    {
                        configuration.FileNamePrefix,
                        VisualVerificationMatchNames.CroppedElementImageFileName,
                        configuration.FileNameSuffix,
                    }
                    .JoinNotNullOrEmpty("-")),
                elementImageCropped.Clone(),
                messageIfExists: HintFailureDumpItemAlreadyExists);

            // The diff image
            context.AppendFailureDump(
                Path.Combine(
                    VisualVerificationMatchNames.DumpFolderName,
                    new[]
                    {
                        configuration.FileNamePrefix,
                        VisualVerificationMatchNames.DiffImageFileName,
                        configuration.FileNameSuffix,
                    }
                    .JoinNotNullOrEmpty("-")),
                diffImage.Clone(),
                messageIfExists: HintFailureDumpItemAlreadyExists);

            // The diff stats
            context.AppendFailureDump(
                Path.Combine(
                    VisualVerificationMatchNames.DumpFolderName,
                    new[]
                    {
                        configuration.FileNamePrefix,
                        VisualVerificationMatchNames.DiffLogFileName,
                        configuration.FileNameSuffix,
                    }
                    .JoinNotNullOrEmpty("-")),
                content: string.Format(
                    CultureInfo.InvariantCulture,
                    @"
calculated differences:
    absoluteError={0},
    meanError={1},
    pixelErrorCount={2},
    pixelErrorPercentage={3}",
                    diff.AbsoluteError,
                    diff.MeanError,
                    diff.PixelErrorCount,
                    diff.PixelErrorPercentage),
                messageIfExists: HintFailureDumpItemAlreadyExists);

            throw;
        }
    }

    private static void AssertInternal<TValue>(
        VisualVerificationMatchApprovedContext approvedContext,
        Func<TValue, TValue, bool> comparer,
        TValue actual,
        TValue expected,
        string propertyName,
        string condition)
    {
        if (comparer(actual, expected))
        {
            return;
        }

        throw new VisualVerificationAssertionException(
            FormatAssertionMessage(
                approvedContext,
                actual,
                expected,
                propertyName,
                condition));
    }

    private static void AssertInternal<TValue>(
        Func<TValue, TValue, bool> comparer,
        TValue actual,
        TValue expected,
        string propertyName,
        string condition)
    {
        if (comparer(actual, expected))
        {
            return;
        }

        throw new VisualVerificationAssertionException(
            FormatAssertionMessage(
                approvedContext: null,
                actual,
                expected,
                propertyName,
                condition));
    }

    private static string FormatAssertionMessage<TValue>(
        VisualVerificationMatchApprovedContext approvedContext,
        TValue actual,
        TValue expected,
        string propertyName,
        string condition)
    {
        var message = new StringBuilder()
            .AppendLine()
            .AppendLine(
                CultureInfo.InvariantCulture,
                $"{propertyName} should be {condition} {expected} but the calculated value is {actual}");

        if (approvedContext != null)
        {
            string loadedFrom = null;
            if (!string.IsNullOrEmpty(approvedContext.BaselineImagePath))
            {
                loadedFrom = $"file: {approvedContext.BaselineImagePath}";
            }
            else if (!string.IsNullOrEmpty(approvedContext.BaselineResourceName))
            {
                loadedFrom = $"embedded resource: {approvedContext.BaselineResourceName}";
            }

            if (!string.IsNullOrEmpty(loadedFrom))
            {
                message
                    .AppendLine()
                    .AppendLine("Visual verification failed since the asserted element looks different from the baseline image.")
                    .AppendLine(
                        CultureInfo.InvariantCulture,
                        $"The baseline image was loaded from {loadedFrom}.")
                    .Append("If you want a new baseline image, simply delete the existing one ")
                    .AppendLine("and a new one will be generated on next run.");
            }
        }

        return message.ToString();
    }

    private static bool IsVisualVerificationMethod(EnhancedStackFrame frame) =>
        frame.MethodInfo.MethodBase.IsDefined(typeof(VisualVerificationApprovedMethodAttribute), inherit: true);

    private static bool IsCompilerGenerated(EnhancedStackFrame frame) =>
        frame.MethodInfo.MethodBase.IsDefined(typeof(CompilerGeneratedAttribute), inherit: true);
}
