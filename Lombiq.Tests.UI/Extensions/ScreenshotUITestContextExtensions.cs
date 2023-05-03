using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Extensions;

public static class ScreenshotUITestContextExtensions
{
    /// <summary>
    /// Takes a screenshot of the current browser tab and saves it under the given path.
    /// </summary>
    public static void TakeScreenshot(this UITestContext context, string imagePath) =>
        context.TakeScreenshot().SaveAsFile(imagePath);

    /// <summary>
    /// Takes a screenshot of the current browser tab.
    /// </summary>
    public static Screenshot TakeScreenshot(this UITestContext context) =>
        context.Driver.AsScreenshotTaker().GetScreenshot();

    /// <summary>
    /// Takes a screenshot of the whole page, including content that needs to be scrolled down to.
    /// </summary>
    public static Image TakeFullPageScreenshot(this UITestContext context)
    {
        var originalScrollPosition = context.GetScrollPosition();
        var images = new Dictionary<Point, Image>();

        try
        {
            var requestedScrollPosition = Point.Empty;
            var viewportSize = context.GetViewportSize();

            if (context.GetScrollPosition() != requestedScrollPosition)
            {
                context.ScrollTo(requestedScrollPosition);
                context.WaitScrollToNotChange(interval: TimeSpan.FromMilliseconds(100));
            }

            var currentScrollPosition = context.GetScrollPosition();
            Point lastScrollPosition;

            do
            {
                lastScrollPosition = context.GetScrollPosition();
                var image = context.TakeScreenshot().ToBitmap();
                images.Add(currentScrollPosition, image);

                requestedScrollPosition = new Point(
                    currentScrollPosition.X,
                    currentScrollPosition.Y + viewportSize.Height);
                context.ScrollTo(requestedScrollPosition);
                context.WaitScrollToNotChange(interval: TimeSpan.FromMilliseconds(100));
                currentScrollPosition = context.GetScrollPosition();
            }
            while (currentScrollPosition == requestedScrollPosition);

            if (currentScrollPosition.Y < requestedScrollPosition.Y && currentScrollPosition != lastScrollPosition)
            {
                var image = context.TakeScreenshot().ToBitmap();
                images.Add(currentScrollPosition, image);
            }

            var height = images.Keys.Sum(
                position =>
                    position.Y % viewportSize.Height == 0
                        ? viewportSize.Height
                        : (position.Y + viewportSize.Height) % viewportSize.Height);

            var screenshot = new SixLabors.ImageSharp.Image<Argb32>(viewportSize.Width, height);

            foreach (var (point, image) in images)
            {
                screenshot.Mutate(ctx => ctx.DrawImage(image, point, 1));
            }

            return screenshot;
        }
        finally
        {
            foreach (var image in images.Values)
            {
                image.Dispose();
            }

            var currentScrollPosition = context.GetScrollPosition();
            if (currentScrollPosition != originalScrollPosition)
            {
                context.ScrollTo(originalScrollPosition);
                context.WaitScrollToNotChange(interval: TimeSpan.FromMilliseconds(100));
            }
        }
    }

    /// <summary>
    /// Takes a screenshot of an element region only.
    /// </summary>
    public static Image TakeElementScreenshot(this UITestContext context, IWebElement element)
    {
        using var screenshot = context.TakeFullPageScreenshot();

        var elementLocation = element.Location;
        var elementSize = element.Size;

        var expectedSize = new Size(elementLocation.X + elementSize.Width, elementLocation.Y + elementSize.Height);

        var widthDifference = expectedSize.Width - screenshot.Width;
        var heightDifference = expectedSize.Height - screenshot.Height;

        if (widthDifference > 0 || heightDifference > 0)
        {
            // A difference of 1px can occur when both element.Location and element.Size were rounded to the next
            // integer from exactly 0.5px, e.g. 212.5px + 287.5px = 500px in the browser, but due to both Size and Point
            // using int for their coordinates, this will become 213px + 288px = 501px, which will be caught as an error
            // here. In that case, we keep the elementSize as is, but reduce the Location coordinate by 1 so that
            // cropping the full page screenshot to the desired region will not fail due to too large dimensions.
            if (widthDifference <= 1 && heightDifference <= 1)
            {
                elementLocation.X -= widthDifference;
                elementLocation.Y -= heightDifference;
            }
            else
            {
                throw new InvalidOperationException(
                    "The captured screenshot size is smaller then the size required by the selected element. This can"
                    + " occur if there was an unsuccessful scrolling operation while capturing page parts."
                    + $" Captured size: {AsDimensions(screenshot)}. Expected size: {AsDimensions(expectedSize)}.");
            }
        }

        var cropRectangle = new Rectangle(elementLocation.X, elementLocation.Y, elementSize.Width, elementSize.Height);
        return screenshot.Clone(image => image.Crop(cropRectangle));
    }

    /// <summary>
    /// Takes a screenshot of an element region only.
    /// </summary>
    public static Image TakeElementScreenshot(this UITestContext context, By elementSelector) =>
        context.TakeElementScreenshot(context.Get(elementSelector));

    private static string AsDimensions(IImageInfo image) =>
        $"{image.Width.ToTechnicalString()} x {image.Height.ToTechnicalString()}";

    private static string AsDimensions(Size size) =>
        $"{size.Width.ToTechnicalString()} x {size.Height.ToTechnicalString()}";
}
