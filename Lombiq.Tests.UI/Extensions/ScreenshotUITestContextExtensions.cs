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
        context.Scope.Driver.GetScreenshot();

    /// <summary>
    /// Takes a screenshot of an element.
    /// </summary>
    public static Bitmap TakeScreenshotImage(this UITestContext context, IWebElement element)
    {
        var screen = context.TakeScreenshot();
        using var screenRaw = new MemoryStream(screen.AsByteArray);
        using var screenImage = Image.FromStream(screenRaw) as Bitmap;

        return screenImage.Clone(new Rectangle(element.Location, element.Size), screenImage.PixelFormat);
    }
}
