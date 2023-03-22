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

        var elementAbsoluteSize = new Size(
            element.Location.X + element.Size.Width,
            element.Location.Y + element.Size.Height);

        if (elementAbsoluteSize.Width > screenshot.Width || elementAbsoluteSize.Height > screenshot.Height)
        {
            throw new InvalidOperationException(
                "The captured screenshot size is smaller then the size required by the selected element. This can occur"
                + " if there was an unsuccessful scrolling operation while capturing page parts."
                + $"Captured size: {screenshot.Width.ToTechnicalString()} x {screenshot.Height.ToTechnicalString()}. "
                + $"Required size: {elementAbsoluteSize.Width.ToTechnicalString()} x "
                + $"{elementAbsoluteSize.Height.ToTechnicalString()}.");
        }

        var bounds = new Rectangle(element.Location.X, element.Location.Y, element.Size.Width, element.Size.Height);
        return screenshot.Clone(ctx => ctx.Crop(bounds));
    }

    /// <summary>
    /// Takes a screenshot of an element region only.
    /// </summary>
    public static Image TakeElementScreenshot(this UITestContext context, By elementSelector) =>
        context.TakeElementScreenshot(context.Get(elementSelector));
}
