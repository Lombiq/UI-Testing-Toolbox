using OpenQA.Selenium;
using System.Drawing;
using System.IO;

namespace Lombiq.Tests.UI.Extensions;

public static class ScreenshotExtensions
{
    /// <summary>
    /// Converts Screenshot to Bitmap/>.
    /// </summary>
    public static Bitmap ToBitmap(this Screenshot screenshot)
    {
        using var screenRaw = new MemoryStream(screenshot.AsByteArray);

        return (Bitmap)Image.FromStream(screenRaw);
    }
}
