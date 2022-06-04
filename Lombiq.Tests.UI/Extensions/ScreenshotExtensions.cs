using OpenQA.Selenium;
using System.Drawing;
using System.IO;

namespace Lombiq.Tests.UI.Extensions;

public static class ScreenshotExtensions
{
    // [System.Drawing.Bitmap, System.Drawing] needed here, but System.Drawing.Bitmap is matching with
    // [System.Drawing.Bitmap, Microsoft.Data.Tools.Utilities].
#pragma warning disable CS0419 // Ambiguous reference in cref attribute
    /// <summary>
    /// Converts <see cref="Screenshot"/> to <see cref="System.Drawing.Bitmap"/>.
    /// </summary>
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
    public static Bitmap ToBitmap(this Screenshot screenshot)
    {
        using var screenRaw = new MemoryStream(screenshot.AsByteArray);

        return (Bitmap)Image.FromStream(screenRaw);
    }
}
