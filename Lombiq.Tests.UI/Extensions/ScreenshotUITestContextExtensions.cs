using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Drawing;
using System.IO;

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
    /// Takes a screenshot of an element regeion only.
    /// </summary>
    public static Bitmap TakeElementScreenshot(this UITestContext context, IWebElement element)
    {
        var screen = context.TakeScreenshot();
        using var screenRaw = new MemoryStream(screen.AsByteArray);
        using var screenImage = (Bitmap)Image.FromStream(screenRaw);

        return screenImage.Clone(new Rectangle(element.Location, element.Size), screenImage.PixelFormat);
    }

    /// <summary>
    /// Takes a screenshot of an element regeion only.
    /// </summary>
    public static Bitmap TakeElementScreenshot(this UITestContext context, By elementSelector) =>
        context.TakeElementScreenshot(context.Get(elementSelector));
}
