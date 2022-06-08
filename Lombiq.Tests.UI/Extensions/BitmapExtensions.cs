using SixLabors.ImageSharp;
using System.Drawing.Imaging;
using System.IO;

// We can't import the whole System.Drawing namespace, because both SixLabors.ImageSharp and System.Drawing contain
// Image classes, but we want it and some others from SixLabors.ImageSharp.
// So we import only Bitmap from System.Drawing here.
using Bitmap = System.Drawing.Bitmap;

namespace Lombiq.Tests.UI.Extensions;

public static class BitmapExtensions
{
    /// <summary>
    /// Converts a <see cref="Bitmap"/> to <see cref="Image"/>.
    /// </summary>
    /// <param name="bitmap"><see cref="Bitmap"/> instance to convert.</param>
    public static Image ToImageSharpImage(this Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Bmp);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return Image.Load(memoryStream);
    }
}
