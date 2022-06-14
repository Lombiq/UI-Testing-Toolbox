using Codeuctivity.ImageSharpCompare;
using Lombiq.Tests.UI.Attributes;
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

    /// <summary>
    /// Compares the reference image and screenshot of the whole page. The mean error percentage should be less than or
    /// equal to the given <paramref name="meanErrorPercentageThreshold"/>. The reference image is automatically
    /// loaded from assembly resource, if it doesn't exist exists then from the project path based on
    /// <see cref="VisualVerificationMatchApprovedConfiguration"/> - it can be configured over <paramref name="configurator"/> -,
    /// if the reference image doesn't exist, a new one will be created based on the element's screenshot, and an
    /// <see cref="VisualVerificationReferenceImageNotFoundException"/> will be thrown. The reference image path is
    /// generated from the method name - annotated with <see cref="VisualVerificationApprovedMethodAttribute"/> - and the source file
    /// name and path, where the method is.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="meanErrorPercentageThreshold">Maximum acceptable mean error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    /// <exception cref="VisualVerificationReferenceImageNotFoundException">
    /// If no reference image found under project path.
    /// </exception>
    [VisualVerificationApprovedMethod]
    public static void AssertVisualVerificationApproved(
        this UITestContext context,
        double meanErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualVerificationMatchApprovedConfiguration> configurator = null) =>
            context.AssertVisualVerificationApproved(
                By.TagName("body"),
                meanErrorPercentageThreshold,
                regionOfInterest,
                configurator);

    /// <summary>
    /// Compares the reference image and screenshot of the element given by <paramref name="elementSelector"/>. The mean
    /// error percentage should be less than or equal to the given <paramref name="meanErrorPercentageThreshold"/>.
    /// The reference image is automatically loaded from assembly resource, if it doesn't exist exists then from the
    /// project path based on <see cref="VisualVerificationMatchApprovedConfiguration"/> - it can be configured over
    /// <paramref name="configurator"/> -, if the reference image doesn't exist, a new one will be created based on the
    /// element's screenshot, and an <see cref="VisualVerificationReferenceImageNotFoundException"/> will be thrown. The
    /// reference image path is generated from the method name - annotated with
    /// <see cref="VisualVerificationApprovedMethodAttribute"/> - and the source file name and path, where the method is.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="meanErrorPercentageThreshold">Maximum acceptable mean error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    /// <exception cref="VisualVerificationReferenceImageNotFoundException">
    /// If no reference image found under project path.
    /// </exception>
    [VisualVerificationApprovedMethod]
    public static void AssertVisualVerificationApproved(
        this UITestContext context,
        By elementSelector,
        double meanErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualVerificationMatchApprovedConfiguration> configurator = null) =>
            context.AssertVisualVerificationApproved(
                elementSelector,
                (approvedContext, diff) =>
                    AssertInternal(
                        approvedContext,
                        (actual, expected) => actual <= expected,
                        diff.PixelErrorPercentage,
                        meanErrorPercentageThreshold,
                        nameof(diff.PixelErrorPercentage),
                        ConditionLessThenOrEqualTo),
                regionOfInterest,
                configurator);

    /// <summary>
    /// Compares the reference image and screenshot of the element. The mean error percentage should be less than or
    /// equal to the given <paramref name="meanErrorPercentageThreshold"/>.
    /// The reference image is automatically loaded from assembly resource, if it doesn't exist exists then from the
    /// project path based on <see cref="VisualVerificationMatchApprovedConfiguration"/> - it can be configured over
    /// <paramref name="configurator"/> -, if the reference image doesn't exist, a new one will be created based on the
    /// element's screenshot, and an <see cref="VisualVerificationReferenceImageNotFoundException"/> will be thrown. The
    /// reference image path is generated from the method name - annotated with
    /// <see cref="VisualVerificationApprovedMethodAttribute"/> - and the source file name and path, where the method is.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="element">Target element.</param>
    /// <param name="meanErrorPercentageThreshold">Maximum acceptable mean error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    /// <exception cref="VisualVerificationReferenceImageNotFoundException">
    /// If no reference image found under project path.
    /// </exception>
    [VisualVerificationApprovedMethod]
    public static void AssertVisualVerificationApproved(
        this UITestContext context,
        IWebElement element,
        double meanErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualVerificationMatchApprovedConfiguration> configurator = null) =>
            context.AssertVisualVerificationApproved(
                element,
                (approvedContext, diff) =>
                    AssertInternal(
                        approvedContext,
                        (actual, expected) => actual <= expected,
                        diff.PixelErrorPercentage,
                        meanErrorPercentageThreshold,
                        nameof(diff.PixelErrorPercentage),
                        ConditionLessThenOrEqualTo),
                regionOfInterest,
                configurator);

    /// <summary>
    /// Compares the reference image and screenshot of the element given by <paramref name="elementSelector"/>.
    /// The reference image is automatically loaded from assembly resource, if it doesn't exist exists then from the
    /// project path based on <see cref="VisualVerificationMatchApprovedConfiguration"/> - it can be configured over
    /// <paramref name="configurator"/> -, if the reference image doesn't exist, a new one will be created based on the
    /// element's screenshot, and an <see cref="VisualVerificationReferenceImageNotFoundException"/> will be thrown. The
    /// reference image path is generated from the method name - annotated with
    /// <see cref="VisualVerificationApprovedMethodAttribute"/> - and the source file name and path, where the method is.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="comparator">To validate the comparison result.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    /// <exception cref="VisualVerificationReferenceImageNotFoundException">
    /// If no reference image found under project path.
    /// </exception>
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
                        .JoinNotEmptySafe("-")
                    );
                });

    /// <summary>
    /// Compares the reference image and screenshot of the element.
    /// The reference image is automatically loaded from assembly resource, if it doesn't exist exists then from the
    /// project path based on <see cref="VisualVerificationMatchApprovedConfiguration"/> - it can be configured over
    /// <paramref name="configurator"/> -, if the reference image doesn't exist, a new one will be created based on the
    /// element's screenshot, and an <see cref="VisualVerificationReferenceImageNotFoundException"/> will be thrown. The
    /// reference image path is generated from the method name - annotated with
    /// <see cref="VisualVerificationApprovedMethodAttribute"/> - and the source file name and path, where the method is.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="element">Target element.</param>
    /// <param name="comparator">To validate the comparison result.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    /// <exception cref="VisualVerificationReferenceImageNotFoundException">
    /// If no reference image found under project path.
    /// </exception>
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
        };

        approvedContext.ReferenceFileName = configuration.ReferenceFileNameFormatter(
            configuration,
            approvedContext.ModuleName,
            approvedContext.MethodName);

        // Try loading reference image from embedded resources first.
        approvedContext.ReferenceResourceName = $"{testFrame.MethodInfo.DeclaringType.Namespace}.{approvedContext.ReferenceFileName}.bmp";
        var referenceImage = testFrame.MethodInfo.DeclaringType.Assembly
            .TryGetResourceBitmap(approvedContext.ReferenceResourceName);

        if (referenceImage == null)
        {
            // Then if no resource exists, try load reference image from source.
            if (string.IsNullOrEmpty(testFrame.GetFileName()))
            {
                throw new VisualVerificationSourceInformationNotAvailableException(
                    $"Source information not available, make sure you are compiling with full debug information."
                    + $"Frame: {testFrame.MethodInfo.DeclaringType.Name}.{testFrame.MethodInfo.Name}");
            }

            approvedContext.ModuleDirectory = Path.GetDirectoryName(testFrame.GetFileName());
            approvedContext.ReferenceImagePath = Path.Combine(
                approvedContext.ModuleDirectory,
                $"{approvedContext.ReferenceFileName}.bmp");

            if (!File.Exists(approvedContext.ReferenceImagePath))
            {
                using var suggestedImage = context.TakeElementScreenshot(element);
                suggestedImage.Save(approvedContext.ReferenceImagePath, ImageFormat.Bmp);

                throw new VisualVerificationReferenceImageNotFoundException(approvedContext.ReferenceImagePath);
            }

            referenceImage = (Bitmap)Image.FromFile(approvedContext.ReferenceImagePath);
        }

        try
        {
            context.AssertVisualVerification(
                element,
                referenceImage,
                diff => comparator(approvedContext, diff),
                regionOfInterest,
                cfg => cfg.WithCroppedElementImageFileName(configuration.CroppedElementImageFileName)
                    .WithCroppedReferenceImageFileName(configuration.CroppedReferenceImageFileName)
                    .WithDiffImageFileName(configuration.DiffImageFileName)
                    .WithDiffLogFileName(configuration.DiffLogFileName)
                    .WithDumpFolderName(configuration.DumpFolderName)
                    .WithElementImageFileName(configuration.ElementImageFileName)
                    .WithFullScreenImageFileName(configuration.FullScreenImageFileName)
                    .WithReferenceImageFileName(configuration.ReferenceImageFileName)
                    .WithFileNamePrefix(approvedContext.ReferenceFileName)
                    .WithFileNameSuffix(string.Empty));
        }
        finally
        {
            referenceImage?.Dispose();
        }
    }

    /// <summary>
    /// Compares the reference image and screenshot of the whole page. The mean error percentage should be less than or
    /// equal to the given <paramref name="meanErrorPercentageThreshold"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="reference">Reference image.</param>
    /// <param name="meanErrorPercentageThreshold">Maximum acceptable mean error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    public static void AssertVisualVerification(
        this UITestContext context,
        Bitmap reference,
        double meanErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualMatchConfiguration> configurator = null) =>
        context.AssertVisualVerification(
            By.TagName("body"),
            reference,
            meanErrorPercentageThreshold,
            regionOfInterest,
            configurator);

    /// <summary>
    /// Compares the reference image and screenshot of the element given by <paramref name="elementSelector"/>. The mean
    /// error percentage should be less than or equal to the given <paramref name="meanErrorPercentageThreshold"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="reference">Reference image.</param>
    /// <param name="meanErrorPercentageThreshold">Maximum acceptable mean error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    public static void AssertVisualVerification(
        this UITestContext context,
        By elementSelector,
        Bitmap reference,
        double meanErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualMatchConfiguration> configurator = null) =>
        context.AssertVisualVerification(
            elementSelector,
            reference,
            diff =>
                AssertInternal(
                    (actual, expected) => actual <= expected,
                    diff.PixelErrorPercentage,
                    meanErrorPercentageThreshold,
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
                    .JoinNotEmptySafe("-")
                );
            });

    /// <summary>
    /// Compares the reference image and screenshot of the element. The mean error percentage should be less than or
    /// equal to the given <paramref name="meanErrorPercentageThreshold"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="element">Target element.</param>
    /// <param name="reference">Reference image.</param>
    /// <param name="meanErrorPercentageThreshold">Maximum acceptable mean error in percentage.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    public static void AssertVisualVerification(
        this UITestContext context,
        IWebElement element,
        Bitmap reference,
        double meanErrorPercentageThreshold,
        Rectangle? regionOfInterest = null,
        Action<VisualMatchConfiguration> configurator = null) =>
        context.AssertVisualVerification(
            element,
            reference,
            diff =>
                AssertInternal(
                    (actual, expected) => actual <= expected,
                    diff.PixelErrorPercentage,
                    meanErrorPercentageThreshold,
                    nameof(diff.PixelErrorPercentage),
                    ConditionLessThenOrEqualTo),
            regionOfInterest,
            configurator);

    /// <summary>
    /// Compares the reference image and screenshot of the element given by <paramref name="elementSelector"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="reference">Reference image.</param>
    /// <param name="comparator">To validate the comparison result.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    private static void AssertVisualVerification(
        this UITestContext context,
        By elementSelector,
        Bitmap reference,
        Action<ICompareResult> comparator,
        Rectangle? regionOfInterest = null,
        Action<VisualMatchConfiguration> configurator = null) =>
        context.AssertVisualVerification(
            context.Get(elementSelector),
            reference,
            comparator,
            regionOfInterest,
            configurator);

    /// <summary>
    /// Compares the reference image and screenshot of the element.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="element">Target element.</param>
    /// <param name="reference">Reference image.</param>
    /// <param name="comparator">To validate the comparison result.</param>
    /// <param name="regionOfInterest">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    private static void AssertVisualVerification(
        this UITestContext context,
        IWebElement element,
        Bitmap reference,
        Action<ICompareResult> comparator,
        Rectangle? regionOfInterest = null,
        Action<VisualMatchConfiguration> configurator = null)
    {
        var configuration = new VisualMatchConfiguration();
        configurator?.Invoke(configuration);

        var cropRegion = regionOfInterest ?? new Rectangle(0, 0, reference.Width, reference.Height);

        // We take a screenshot and append it to the failure dump.
        var fullScreenImage = context.TakeScreenshot()
            .ToBitmap();
        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                new[]
                {
                    configuration.FileNamePrefix,
                    configuration.FullScreenImageFileName,
                    configuration.FileNameSuffix,
                }
                .JoinNotEmptySafe("-")),
            fullScreenImage);

        // We take a screenshot of the element area. This will be compared to a reference image.
        using var elementImage = context.TakeElementScreenshot(element)
            .ToImageSharpImage()
            .ShouldNotBeNull();

        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                new[]
                {
                    configuration.FileNamePrefix,
                    configuration.ElementImageFileName,
                    configuration.FileNameSuffix,
                }
                .JoinNotEmptySafe("-")),
            elementImage.Clone());

        // Checking the size of captured image.
        elementImage.Width
            .ShouldBeGreaterThanOrEqualTo(cropRegion.Left + cropRegion.Width);
        elementImage.Height
            .ShouldBeGreaterThanOrEqualTo(cropRegion.Top + cropRegion.Height);

        using var referenceImage = reference
            .ToImageSharpImage();

        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                new[]
                {
                    configuration.FileNamePrefix,
                    configuration.ReferenceImageFileName,
                    configuration.FileNameSuffix,
                }
                .JoinNotEmptySafe("-")),
            referenceImage.Clone());

        // Here we crop the regionOfInterest.
        referenceImage.Mutate(imageContext => imageContext.Crop(cropRegion.ToImageSharpRectangle()));
        elementImage.Mutate(imageContext => imageContext.Crop(cropRegion.ToImageSharpRectangle()));

        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                new[]
                {
                    configuration.FileNamePrefix,
                    configuration.CroppedReferenceImageFileName,
                    configuration.FileNameSuffix,
                }
                .JoinNotEmptySafe("-")),
            referenceImage.Clone());
        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                new[]
                {
                    configuration.FileNamePrefix,
                    configuration.CroppedElementImageFileName,
                    configuration.FileNameSuffix,
                }
                .JoinNotEmptySafe("-")),
            elementImage.Clone());

        // At this point, we have reference and captured images too.
        // Creating a diff image is not required, but it can be very useful to investigate failing tests.
        // You can read more about how diff created here:
        // https://github.com/Codeuctivity/ImageSharp.Compare/blob/2.0.46/ImageSharpCompare/ImageSharpCompare.cs#L303.
        // So lets create it and append it to failure dump.
        var diffImage = referenceImage
            .CalcDiffImage(elementImage)
            .ShouldNotBeNull();

        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                new[]
                {
                    configuration.FileNamePrefix,
                    configuration.DiffImageFileName,
                    configuration.FileNameSuffix,
                }
                .JoinNotEmptySafe("-")),
            diffImage);

        // Now we are one step away from the end. Here we create a statistical summary of the differences
        // between the captured and the reference image. In the end, the lower values are better.
        // You can read more about how these statistical calculations are created here:
        // https://github.com/Codeuctivity/ImageSharp.Compare/blob/2.0.46/ImageSharpCompare/ImageSharpCompare.cs#L218.
        var diff = referenceImage
            .CompareTo(elementImage);

        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                new[]
                {
                    configuration.FileNamePrefix,
                    configuration.DiffLogFileName,
                    configuration.FileNameSuffix,
                }
                .JoinNotEmptySafe("-")),
            @"
calculated differences:
    absoluteError={0},
    meanError={1},
    pixelErrorCount={2},
    pixelErrorPercentage={3}",
            diff.AbsoluteError,
            diff.MeanError,
            diff.PixelErrorCount,
            diff.PixelErrorPercentage);

        comparator(diff);
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
            if (!string.IsNullOrEmpty(approvedContext.ReferenceImagePath))
            {
                loadedFrom = $"file: {approvedContext.ReferenceImagePath}";
            }
            else if (!string.IsNullOrEmpty(approvedContext.ReferenceResourceName))
            {
                loadedFrom = $"embedded resource: {approvedContext.ReferenceResourceName}";
            }

            if (!string.IsNullOrEmpty(loadedFrom))
            {
                message
            .AppendLine()
            .AppendLine("Visual verification failed since the asserted element looks different from the reference image.")
                    .AppendLine(
                        CultureInfo.InvariantCulture,
                        $"The reference image was loaded from {loadedFrom}.")
                    .AppendLine("If you want a new reference image, simply delete the existing one ")
                    .Append("and a new one will be generated on next run.");
            }
        }

        return message.ToString();
    }

    private static bool IsVisualVerificationMethod(EnhancedStackFrame frame) =>
        frame.MethodInfo.MethodBase.IsDefined(typeof(VisualVerificationApprovedMethodAttribute), inherit: true);

    private static bool IsCompilerGenerated(EnhancedStackFrame frame) =>
        frame.MethodInfo.MethodBase.IsDefined(typeof(CompilerGeneratedAttribute), inherit: true);
}
