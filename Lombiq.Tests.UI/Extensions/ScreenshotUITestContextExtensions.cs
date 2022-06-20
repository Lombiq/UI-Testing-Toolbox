using Atata;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
    /// Takes a screeenshot of the whole page.
    /// </summary>
    public static Bitmap TakePageScreenshot(this UITestContext context)
    {
        var originalScrollPosition = context.GetScrollPosition();
        var images = new Dictionary<Point, Bitmap>();

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
                var image = context.TakeScreenshot()
                    .ToBitmap();
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
                var image = context.TakeScreenshot()
                    .ToBitmap();
                images.Add(currentScrollPosition, image);
            }

            var height = images.Keys.Sum(
                position =>
                    position.Y % viewportSize.Height == 0
                    ? viewportSize.Height
                    : viewportSize.Height - (viewportSize.Height % position.Y));

            var screenshot = new Bitmap(viewportSize.Width, height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(screenshot);

            foreach (var item in images)
            {
                graphics.DrawImage(item.Value, item.Key);
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
    public static Bitmap TakeElementScreenshot(this UITestContext context, IWebElement element)
    {
        using var screenshot = context.TakePageScreenshot();

        screenshot.Size
            .ShouldBeGreaterThanOrEqualTo(
                new Size(element.Location.X + element.Size.Width, element.Location.Y + element.Size.Height),
                new GenericComparer<Size>((left, right) =>
                {
                    if (left.Height < right.Height || left.Width < right.Width) return -1;
                    if (left.Height > right.Height || left.Width > right.Width) return 1;

                    return 0;
                }));

        return screenshot.Clone(
            new Rectangle(element.Location.X, element.Location.Y, element.Size.Width, element.Size.Height),
            screenshot.PixelFormat);
    }

    /// <summary>
    /// Takes a screenshot of an element region only.
    /// </summary>
    public static Bitmap TakeElementScreenshot(this UITestContext context, By elementSelector) =>
        context.TakeElementScreenshot(context.Get(elementSelector));
}
