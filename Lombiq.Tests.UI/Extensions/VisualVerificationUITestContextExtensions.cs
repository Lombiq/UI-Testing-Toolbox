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
using System.IO;
using System.Linq;

namespace Lombiq.Tests.UI.Extensions;

public static class VisualVerificationUITestContextExtensions
{
    /// <summary>
    /// Compares the reference image and screenshot of the element given by <paramref name="elementSelector"/>. The mean
    /// error percentage should be less or equal with the given <paramref name="meanErrorPercentageThreshold"/>.
    /// The reference image is automatically loaded from the project path based on
    /// <see cref="VisualMatchApprovedConfiguration"/> - it can be configured over <paramref name="configurator"/> -,
    /// if the reference image doesn't exist, a new one will be created based on the element's screenshot, and an
    /// <see cref="VisualVerificationReferenceImageCreatedException"/> will be thrown. The reference image path is
    /// generated from the method name - annotated with <see cref="VisualVerificationAttribute"/> - and the source file
    /// name and path, where the method is.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="meanErrorPercentageThreshold">Maximum acceptable mean error in percentage.</param>
    /// <param name="roi">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    /// <exception cref="VisualVerificationReferenceImageCreatedException">
    /// If no reference image found under project path.
    /// </exception>
    /// <exception cref="VisualVerificationAttributeNotFoundException">
    /// If no method found annotated with <see cref="VisualVerificationAttribute"/> in stacktrace.
    /// </exception>
    public static void ShouldVisualMatchApproved(
        this UITestContext context,
        By elementSelector,
        double meanErrorPercentageThreshold,
        Rectangle? roi = null,
        Action<VisualMatchApprovedConfiguration> configurator = null) => context
        .ShouldVisualMatchApproved(
            elementSelector,
            diff => diff.PixelErrorPercentage.ShouldBeLessThanOrEqualTo(meanErrorPercentageThreshold),
            roi,
            configurator);

    /// <summary>
    /// Compares the reference image and screenshot of the element given by <paramref name="elementSelector"/>.
    /// The reference image is automatically loaded from the project path based on
    /// <see cref="VisualMatchApprovedConfiguration"/> - it can be configured over <paramref name="configurator"/> -,
    /// if the reference image doesn't exist, a new one will be created based on the element's screenshot, and an
    /// <see cref="VisualVerificationReferenceImageCreatedException"/> will be thrown. The reference image path is
    /// generated from the method name - annotated with <see cref="VisualVerificationAttribute"/> - and the source file
    /// name and path, where the method is.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="comparator">To validate the comparison result.</param>
    /// <param name="roi">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    /// <exception cref="VisualVerificationReferenceImageCreatedException">
    /// If no reference image found under project path.
    /// </exception>
    /// <exception cref="VisualVerificationAttributeNotFoundException">
    /// If no method found annotated with <see cref="VisualVerificationAttribute"/> in stacktrace.
    /// </exception>
    public static void ShouldVisualMatchApproved(
        this UITestContext context,
        By elementSelector,
        Action<ICompareResult> comparator,
        Rectangle? roi = null,
        Action<VisualMatchApprovedConfiguration> configurator = null) => context
        .ShouldVisualMatchApproved(context.Get(elementSelector), comparator, roi, configurator);

    /// <summary>
    /// Compares the reference image and screenshot of the element.
    /// The reference image is automatically loaded from the project path based on
    /// <see cref="VisualMatchApprovedConfiguration"/> - it can be configured over <paramref name="configurator"/> -,
    /// if the reference image doesn't exist, a new one will be created based on the element's screenshot, and an
    /// <see cref="VisualVerificationReferenceImageCreatedException"/> will be thrown. The reference image path is
    /// generated from the method name - annotated with <see cref="VisualVerificationAttribute"/> - and the source file
    /// name and path, where the method is.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="element">Target element.</param>
    /// <param name="comparator">To validate the comparison result.</param>
    /// <param name="roi">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    /// <exception cref="VisualVerificationReferenceImageCreatedException">
    /// If no reference image found under project path.
    /// </exception>
    /// <exception cref="VisualVerificationAttributeNotFoundException">
    /// If no method found annotated with <see cref="VisualVerificationAttribute"/> in stacktrace.
    /// </exception>
    public static void ShouldVisualMatchApproved(
        this UITestContext context,
        IWebElement element,
        Action<ICompareResult> comparator,
        Rectangle? roi = null,
        Action<VisualMatchApprovedConfiguration> configurator = null)
    {
        var configuration = new VisualMatchApprovedConfiguration();
        configurator?.Invoke(configuration);

        var stackTrace = new EnhancedStackTrace(new StackTrace(fNeedFileInfo: true));
        var testFrame = stackTrace
            .FirstOrDefault(frame =>
                frame.MethodInfo.MethodBase.CustomAttributes
                    .Any(attribute => attribute.AttributeType == typeof(VisualVerificationAttribute)));

        if (testFrame == null)
        {
            throw new VisualVerificationAttributeNotFoundException();
        }

        var moduleName = testFrame.MethodInfo.DeclaringType.Name;
        var methodName = testFrame.MethodInfo.Name;
        var referenceFileName = configuration.ReferenceFileNameFormatter(moduleName, methodName);

        // Try loading reference image from embedded resources first.
        var referenceResourceName = $"{testFrame.MethodInfo.DeclaringType.Namespace}.{referenceFileName}.bmp";
        var referenceImage = testFrame.MethodInfo.DeclaringType.Assembly
            .TryGetResourceBitmap(referenceResourceName);

        if (referenceImage == null)
        {
            // Then if no resource exists, try load reference image from source.
            if (string.IsNullOrEmpty(testFrame.GetFileName()))
            {
                throw new SourceInformationNotAvailableException(
                    $"Source information not available, make sure you are compiling with full debug information."
                    + $"Frame: {testFrame.MethodInfo.DeclaringType.Name}.{testFrame.MethodInfo.Name}");
            }

            var moduleDirectory = Path.GetDirectoryName(testFrame.GetFileName());
            var referenceImagePath = Path.Combine(moduleDirectory, $"{referenceFileName}.bmp");

            if (!File.Exists(referenceImagePath))
            {
                using var suggestedImage = context.TakeElementScreenshot(element);
                suggestedImage.Save(referenceImagePath, ImageFormat.Bmp);

                throw new VisualVerificationReferenceImageCreatedException(referenceImagePath);
            }

            referenceImage = (Bitmap)Image.FromFile(referenceImagePath);
        }

        try
        {
            context.ShouldVisualMatch(
                element,
                referenceImage,
                comparator,
                roi,
                cfg => cfg.WithCroppedElementImageFileName(configuration.CroppedElementImageFileName)
                    .WithCroppedReferenceImageFileName(configuration.CroppedReferenceImageFileName)
                    .WithDiffImageFileName(configuration.DiffImageFileName)
                    .WithDiffLogFileName(configuration.DiffLogFileName)
                    .WithDumpFolderName(configuration.DumpFolderName)
                    .WithElementImageFileName(configuration.ElementImageFileName)
                    .WithFullScreenImageFileName(configuration.FullScreenImageFileName)
                    .WithReferenceImageFileName(configuration.ReferenceImageFileName)
                    .WithDumpFileNamePrefix(configuration.DumpFileNamePrefix));
        }
        finally
        {
            referenceImage?.Dispose();
        }
    }

    /// <summary>
    /// Compares the reference image and screenshot of the element given by <paramref name="elementSelector"/>.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="elementSelector">Selector for the target element.</param>
    /// <param name="reference">Reference image.</param>
    /// <param name="comparator">To validate the comparison result.</param>
    /// <param name="roi">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    public static void ShouldVisualMatch(
        this UITestContext context,
        By elementSelector,
        Bitmap reference,
        Action<ICompareResult> comparator,
        Rectangle? roi = null,
        Action<VisualMatchConfiguration> configurator = null) => context
        .ShouldVisualMatch(context.Get(elementSelector), reference, comparator, roi, configurator);

    /// <summary>
    /// Compares the reference image and screenshot of the element.
    /// </summary>
    /// <param name="context">The <see cref="UITestContext"/> in which the extension is executed on.</param>
    /// <param name="element">Target element.</param>
    /// <param name="reference">Reference image.</param>
    /// <param name="comparator">To validate the comparison result.</param>
    /// <param name="roi">Region of interest. Can be  null.</param>
    /// <param name="configurator">Action callback to configure the behavior. Can be null.</param>
    public static void ShouldVisualMatch(
        this UITestContext context,
        IWebElement element,
        Bitmap reference,
        Action<ICompareResult> comparator,
        Rectangle? roi = null,
        Action<VisualMatchConfiguration> configurator = null)
    {
        var configuration = new VisualMatchConfiguration();
        configurator?.Invoke(configuration);

        var cropRegion = roi ?? new Rectangle(0, 0, reference.Width, reference.Height);

        // We take a screenshot and append it to the failure dump.
        var fullScreenImage = context.TakeScreenshot()
            .ToBitmap();
        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                $"{configuration.DumpFileNamePrefix ?? string.Empty}{configuration.FullScreenImageFileName}"),
            fullScreenImage);

        // We take a screenshot of the element area. This will be compared to a reference image.
        using var elementImage = context.TakeElementScreenshot(element)
            .ToImageSharpImage()
            .ShouldNotBeNull();

        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                $"{configuration.DumpFileNamePrefix ?? string.Empty}{configuration.ElementImageFileName}"),
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
                $"{configuration.DumpFileNamePrefix ?? string.Empty}{configuration.ReferenceImageFileName}"),
            referenceImage.Clone());

        // Here we crop the RoI.
        referenceImage.Mutate(imageContext => imageContext.Crop(cropRegion.ToImageSharpRectangle()));
        elementImage.Mutate(imageContext => imageContext.Crop(cropRegion.ToImageSharpRectangle()));

        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                $"{configuration.DumpFileNamePrefix ?? string.Empty}{configuration.CroppedReferenceImageFileName}"),
            referenceImage.Clone());
        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                $"{configuration.DumpFileNamePrefix ?? string.Empty}{configuration.CroppedElementImageFileName}"),
            elementImage.Clone());

        // At this point, we have reference and captured images too.
        // Creating diff image is not required, but it can be very useful to investigate failing tests.
        // You can read more about how diff created here:
        // https://github.com/Codeuctivity/ImageSharp.Compare/blob/1.2.11/ImageSharpCompare/ImageSharpCompare.cs#L258.
        // So lets create it and append it to failure dump.
        var diffImage = referenceImage
            .CalcDiffImage(elementImage)
            .ShouldNotBeNull();

        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                $"{configuration.DumpFileNamePrefix ?? string.Empty}{configuration.DiffImageFileName}"),
            diffImage);

        // Now we are one step away from the end. Here we create a statistical summary of the differences
        // between the captured and the reference image. In the end, the lower values are better.
        // You can read more about how these statistical calculations are created here:
        // https://github.com/Codeuctivity/ImageSharp.Compare/blob/1.2.11/ImageSharpCompare/ImageSharpCompare.cs#L143.
        var diff = referenceImage
            .CompareTo(elementImage);

        context.AppendFailureDump(
            Path.Combine(
                configuration.DumpFolderName,
                $"{configuration.DumpFileNamePrefix ?? string.Empty}{configuration.DiffLogFileName}"),
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
}
