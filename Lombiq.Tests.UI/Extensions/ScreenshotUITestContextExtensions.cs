using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Drawing;
using WDSE;
using WDSE.Decorators;
using WDSE.ScreenshotMaker;

namespace Lombiq.Tests.UI.Extensions;

public static class ScreenshotUITestContextExtensions
{
    private const int ScrollDelay = 1000;

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
    /// Takes a screenshot of an element region only.
    /// </summary>
    public static Bitmap TakeElementScreenshot(this UITestContext context, IWebElement element)
    {
        var originalScrollPosition = context.GetScrollPosition();

        try
        {
            var decorator = new VerticalCombineDecorator(new ScreenshotMaker())
                .SetWaitAfterScrolling(TimeSpan.FromMilliseconds(ScrollDelay));
            using var magickImage = context.Driver.TakeScreenshot(decorator)
                .ToMagickImage();

            magickImage.Crop(element.Location.X, element.Location.Y, element.Size.Width, element.Size.Height);

            return magickImage
                .ToBitmap();
        }
        finally
        {
            context.ScrollTo(originalScrollPosition);
            context.WaitScrollToNotChange();
        }
    }

    /// <summary>
    /// Takes a screenshot of an element region only.
    /// </summary>
    public static Bitmap TakeElementScreenshot(this UITestContext context, By elementSelector) =>
        context.TakeElementScreenshot(context.Get(elementSelector));
}
