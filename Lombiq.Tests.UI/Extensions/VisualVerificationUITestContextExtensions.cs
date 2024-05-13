using Codeuctivity.ImageSharpCompare;
using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// Compares the baseline image and screenshot of the selected element on multiple different resolutions.
    /// </summary>
    /// <param name="sizes">The comparison is performed on each of these resolutions.</param>
    /// <param name="getSelector">
    /// Returns the selector for the given screen size. This may return the same selector all the time, or a different
    /// selector, e.g. if mobile and desktop views have different DOMs.
    /// </param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be <see langword="null"/>.</param>
    /// <remarks>
    /// <para>
    /// The parameter <c>beforeAssertAsync</c> was removed, because it sometimes polluted the stack trace, which was
    /// used in visual verification tests, so it caused tests to fail. The point of <c>beforeAssertAsync</c> was, that
    /// sometimes the page can change on the resize window event. So the navigation happening after the window resize
    /// ensures that the currently loaded page only existed with the desired screen size.
    /// </para>
    /// </remarks>
    [VisualVerificationApprovedMethod]
    public static void AssertVisualVerificationOnAllResolutions(
        this UITestContext context,
        IEnumerable<Size> sizes,
        Func<Size, By> getSelector,
        double pixelErrorPercentageThreshold = 0,
        Action<VisualVerificationMatchApprovedConfiguration> configurator = null)
    {
        context.HideScrollbar();

        var exceptions = new List<Exception>();
        foreach (var size in sizes)
        {
            context.SetViewportSize(size);

            try
            {
                context.AssertVisualVerificationApproved(
                    getSelector(size),
                    pixelErrorPercentageThreshold: pixelErrorPercentageThreshold,
                    configurator: configuration =>
                    {
                        configuration.WithFileNameSuffix(StringHelper.CreateInvariant($"{size.Width}x{size.Height}"));
                        configurator?.Invoke(configuration);
                    });
            }
            catch (Exception exception)
            {
                // We don't throw yet, this way if there are missing images they are generated all in one run.
                exceptions.Add(exception);
            }
        }

        if (exceptions.Count == 1) throw exceptions.Single();

        if (exceptions.Count != 0)
        {
            // The UITestExecutionSession doesn't support AggregateException with multiple inner exceptions, so we just
            // concatenate the exceptions if there are multiple.
            throw new InvalidOperationException(
                "Several exceptions have occurred:\n" + string.Join("\n\n\n", exceptions.Select(ex => ex.ToString())));
        }

        context.RestoreHiddenScrollbar();
    }

    /// <summary>
    /// Compares the baseline image and screenshot of the selected element on multiple different resolutions, based on
    /// the operating system's platform.
    /// </summary>
    /// <param name="sizes">The comparison is performed on each of these resolutions.</param>
    /// <param name="getSelector">
    /// Returns the selector for the given screen size. This may return the same selector all the time, or a different
    /// selector, e.g. if mobile and desktop views have different DOMs.
    /// </param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be <see langword="null"/>, but in the
    /// method we are always using <see
    /// cref="VisualVerificationMatchApprovedConfiguration.WithUsePlatformAsSuffix()"/>.</param>
    /// <remarks>
    /// <para>
    /// The parameter <c>beforeAssertAsync</c> was removed, because it sometimes polluted the stack trace, which was
    /// used in visual verification tests, so it caused tests to fail. The point of <c>beforeAssertAsync</c> was, that
    /// sometimes the page can change on the resize window event. So the navigation happening after the window resize
    /// ensures that the currently loaded page only existed with the desired screen size.
    /// </para>
    /// </remarks>
    [VisualVerificationApprovedMethod]
    public static void AssertVisualVerificationApprovedOnAllResolutionsWithPlatformSuffix(
        this UITestContext context,
        IEnumerable<Size> sizes,
        Func<Size, By> getSelector,
        double pixelErrorPercentageThreshold = 0,
        Action<VisualVerificationMatchApprovedConfiguration> configurator = null) =>
        context.AssertVisualVerificationOnAllResolutions(
            sizes,
            getSelector,
            pixelErrorPercentageThreshold,
            configuration => configuration.WithUsePlatformAsSuffix());

    /// <summary>
    /// Compares the baseline image and screenshot of the whole page.
    /// <see cref="AssertVisualVerificationApproved(UITestContext, By, double, Rectangle?, Action{VisualVerificationMatchApprovedConfiguration})"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be <see langword="null"/>.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be <see langword="null"/>.</param>
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

    // This is because of the long method signature.
#pragma warning disable S103 // Lines should not be too long
    /// <summary>
    /// Compares the baseline image and screenshot of the element given by <paramref name="elementSelector"/>.
    /// <see cref="AssertVisualVerificationApproved(UITestContext, IWebElement, double, Rectangle?, Action{VisualVerificationMatchApprovedConfiguration})"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be <see langword="null"/>.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be <see langword="null"/>.</param>
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
    /// mind that, the font rendering results in different visuals. This means that you should use different baseline
    /// images for each platform. You can generate baseline images for each platform with locally build and run tests
    /// and follow the instructions in <see cref="VisualVerificationBaselineImageNotFoundException"/> or running on a CI
    /// and using the image dumped on failure. If you need different baseline images on each platform/browser you can
    /// configure suffixes as needed using <paramref name="configurator"/>.
    /// </para>
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="element">Target element.</param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be <see langword="null"/>.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be <see langword="null"/>.</param>
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
    /// <see cref="AssertVisualVerification(UITestContext, By, Image, double, Rectangle?, Action{VisualMatchConfiguration})"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="baseline">The baseline image.</param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be <see langword="null"/>.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be <see langword="null"/>.</param>
    public static void AssertVisualVerification(
        this UITestContext context,
        Image baseline,
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
    /// <see cref="AssertVisualVerification(UITestContext, IWebElement, Image, double, Rectangle?, Action{VisualMatchConfiguration})"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="baseline">The baseline image.</param>
    /// <param name="pixelErrorPercentageThreshold">Maximum acceptable pixel error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be <see langword="null"/>.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be <see langword="null"/>.</param>
    public static void AssertVisualVerification(
        this UITestContext context,
        By elementSelector,
        Image baseline,
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
    /// <param name="regionOfInterest">Region of interest. Can be <see langword="null"/>.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be <see langword="null"/>.</param>
    public static void AssertVisualVerification(
        this UITestContext context,
        IWebElement element,
        Image baseline,
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
            .Where(frame => frame.GetMethodBase() != null && !IsCompilerGenerated(frame))
            .ToList();
        var testFrame = stackTrace.Find(frame =>
            !IsVisualVerificationMethod(frame) &&
            !string.IsNullOrEmpty(frame.StackFrame.GetFileName()));

        if (testFrame != null && configuration.StackOffset > 0)
        {
            testFrame = stackTrace
                .AsEnumerable()
                .Reverse()
                .TakeWhile(frame => frame.GetMethodBase() != testFrame.GetMethodBase())
                .TakeLast(configuration.StackOffset)
                .FirstOrDefault();
        }

        if (testFrame == null)
        {
            throw new VisualVerificationCallerMethodNotFoundException();
        }

        var approvedContext = new VisualVerificationMatchApprovedContext(context, configuration, testFrame);

        // Try loading baseline image from embedded resources first.
        var baselineImage = testFrame
            .MethodInfo
            .DeclaringType?
            .Assembly
            .GetResourceImageSharpImage(approvedContext.BaselineImageResourceName);

        if (baselineImage == null)
        {
            // Then if no resource exists, try load baseline image from source.
            if (string.IsNullOrEmpty(testFrame.GetFileName()))
            {
                using var suggestedImage = context.TakeElementScreenshot(element);

                var suggestedImageFileName = $"{approvedContext.BaselineImageFileName}.png";

                context.AppendFailureDump(
                    Path.Combine(
                        VisualVerificationMatchNames.DumpFolderName,
                        suggestedImageFileName),
                    suggestedImage.Clone(),
                    messageIfExists: HintFailureDumpItemAlreadyExists);

                throw new VisualVerificationSourceInformationNotAvailableException(
                    $"Source information not available, make sure you are compiling with full debug information."
                    + $" Frame: {testFrame.MethodInfo.DeclaringType?.Name}.{testFrame.MethodInfo.Name}."
                    + $" The suggested baseline image was added to the failure dump as {suggestedImageFileName}");
            }

            if (!File.Exists(approvedContext.BaselineImagePath))
            {
                if (context.Configuration.MaxRetryCount == 0)
                {
                    context.SaveSuggestedImage(
                        element,
                        approvedContext.BaselineImagePath,
                        approvedContext.BaselineImageFileName);
                }

                throw new VisualVerificationBaselineImageNotFoundException(
                    approvedContext.BaselineImagePath, context.Configuration.MaxRetryCount);
            }

            baselineImage = Image.Load(approvedContext.BaselineImagePath);
        }

        try
        {
            context.AssertVisualVerification(
                element,
                baselineImage,
                diff => comparator(approvedContext, diff),
                regionOfInterest,
                cfg => cfg.WithFileNamePrefix(approvedContext.BaselineImageFileName)
                    .WithFileNameSuffix(string.Empty));
        }
        finally
        {
            baselineImage?.Dispose();
        }
    }

    private static void SaveSuggestedImage(
        this UITestContext context,
        IWebElement element,
        string baselineImagePath,
        string baselineFileName)
    {
        using var suggestedImage = context.TakeElementScreenshot(element);
        suggestedImage.Save(baselineImagePath, new PngEncoder());
        context.AddImageToFailureDump(baselineFileName + ".png", suggestedImage);
    }

    private static void AddImageToFailureDump(
        this UITestContext context,
        string fileName,
        Image image) =>
        context.AppendFailureDump(
            Path.Combine(VisualVerificationMatchNames.DumpFolderName, fileName),
            image.Clone(),
            messageIfExists: HintFailureDumpItemAlreadyExists);

    private static void AssertVisualVerification(
        this UITestContext context,
        By elementSelector,
        Image baseline,
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
        Image baseline,
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
        using var elementImageOriginal = context.TakeElementScreenshot(element).ShouldNotBeNull();

        var originalElementScreenshotFileName =
            configuration.WrapFileName(VisualVerificationMatchNames.ElementImageFileName);

        // Checking the dimensions of captured image. This needs to happen before any other comparisons, because that
        // can only be done on images with the same dimensions.
        var cropWidth = cropRegion.Left + cropRegion.Width;
        var cropHeight = cropRegion.Top + cropRegion.Height;
        if (elementImageOriginal.Width < cropWidth || elementImageOriginal.Height < cropHeight)
        {
            var cropRegionName = regionOfInterest == null ? "baseline image" : "selected region of interest";
            var message = $"The dimensions of the captured element ({elementImageOriginal.Width.ToTechnicalString()}" +
                $"px x {elementImageOriginal.Height.ToTechnicalString()}px) are smaller than the dimensions of the " +
                $"{cropRegionName} ({cropWidth.ToTechnicalString()}px x {cropHeight.ToTechnicalString()}px). This " +
                "can happen if due to a change in the app the captured element got smaller than before, or if the " +
                $"{cropRegionName} is mistakenly too large. The suggested baseline image with a screenshot of the " +
                "captured element was saved to the failure dump. Compare this with the original image used by the " +
                "test and if suitable, use it as the baseline going forward.";
            context.AddImageToFailureDump(originalElementScreenshotFileName, elementImageOriginal);

            throw new VisualVerificationAssertionException(message);
        }

        using var baselineImageOriginal = baseline.Clone();

        // Here we crop the regionOfInterest.
        using var baselineImageCropped = baselineImageOriginal.Clone();
        using var elementImageCropped = elementImageOriginal.Clone();

        baselineImageCropped.Mutate(imageContext => imageContext.Crop(cropRegion));
        elementImageCropped.Mutate(imageContext => imageContext.Crop(cropRegion));

        // At this point, we have baseline and captured images too.
        // Creating a diff image is not required, but it can be very useful to investigate failing tests.
        // You can read more about how diff created here:
        // https://github.com/Codeuctivity/ImageSharp.Compare/blob/2.0.46/ImageSharpCompare/ImageSharpCompare.cs#L303.
        // So lets create it now and append it to failure dump later.
        using var diffImage = baselineImageCropped
            .CalcDiffImage(elementImageCropped)
            .ShouldNotBeNull();

        // Now we are one step away from the end. Here we create a statistical summary of the differences between the
        // captured and the baseline image. In the end, lower values are better. You can read more about how these
        // statistical calculations are created here:
        // https://github.com/Codeuctivity/ImageSharp.Compare/blob/2.0.46/ImageSharpCompare/ImageSharpCompare.cs#L218.
        var diff = baselineImageCropped.CompareTo(elementImageCropped);

        try
        {
            comparator(diff);
        }
        catch
        {
            // Here we append all the relevant items to the failure dump to help the investigation.
            void AddImageToFailureDumpLocal(string fileName, Image image, bool dontWrap = false) =>
                context.AddImageToFailureDump(dontWrap ? fileName : configuration.WrapFileName(fileName), image);

            AddImageToFailureDumpLocal(VisualVerificationMatchNames.FullScreenImageFileName, fullScreenImage);
            AddImageToFailureDumpLocal(originalElementScreenshotFileName, elementImageOriginal, dontWrap: true);
            AddImageToFailureDumpLocal(VisualVerificationMatchNames.BaselineImageFileName, baselineImageOriginal);
            AddImageToFailureDumpLocal(VisualVerificationMatchNames.CroppedBaselineImageFileName, baselineImageCropped);
            AddImageToFailureDumpLocal(VisualVerificationMatchNames.CroppedElementImageFileName, elementImageCropped);
            AddImageToFailureDumpLocal(VisualVerificationMatchNames.DiffImageFileName, diffImage);

            // The diff stats.
            context.AppendFailureDump(
                Path.Combine(
                    VisualVerificationMatchNames.DumpFolderName,
                    configuration.WrapFileName(VisualVerificationMatchNames.DiffLogFileName)),
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
            else if (!string.IsNullOrEmpty(approvedContext.BaselineImageResourceName))
            {
                loadedFrom = $"embedded resource: {approvedContext.BaselineImageResourceName}";
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
        frame.GetMethodBase().IsDefined(typeof(VisualVerificationApprovedMethodAttribute), inherit: true);

    private static bool IsCompilerGenerated(EnhancedStackFrame frame) =>
        frame.GetMethodBase().IsDefined(typeof(CompilerGeneratedAttribute), inherit: true);

    private static MethodBase GetMethodBase(this EnhancedStackFrame frame) =>
        frame.MethodInfo.MethodBase ?? frame.MethodInfo.SubMethodBase;
}
