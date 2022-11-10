using OpenQA.Selenium;
using SixLabors.ImageSharp;
using System.IO;

namespace Lombiq.Tests.UI.Extensions;

public static class ScreenshotExtensions
{
    /// <summary>
    /// Converts <see cref="Screenshot"/> to <see cref="Image"/>.
    /// </summary>
    public static Image ToBitmap(this Screenshot screenshot)
    {
        using var screenRaw = new MemoryStream(screenshot.AsByteArray);

        return Image.Load(screenRaw);
    }
}
