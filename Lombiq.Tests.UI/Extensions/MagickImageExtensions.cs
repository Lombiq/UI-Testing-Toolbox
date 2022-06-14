using ImageMagick;
using System.Drawing;
using System.IO;

namespace Lombiq.Tests.UI.Extensions;

public static class MagickImageExtensions
{
    // [System.Drawing.Bitmap, System.Drawing] needed here, but System.Drawing.Bitmap is matching with
    // [System.Drawing.Bitmap, Microsoft.Data.Tools.Utilities].
#pragma warning disable CS0419 // Ambiguous reference in cref attribute
    /// <summary>
    /// Converts a <see cref="IMagickImage"/> to <see cref="System.Drawing.Bitmap"/>.
    /// </summary>
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
    public static Bitmap ToBitmap(this IMagickImage image)
    {
        // The returning Bitmap instance owns the stream.
#pragma warning disable CA2000 // Dispose objects before losing scope
        var imageStream = new MemoryStream();
#pragma warning restore CA2000 // Dispose objects before losing scope

        image.Write(imageStream, MagickFormat.Bmp);
        imageStream.Seek(0, SeekOrigin.Begin);

        return (Bitmap)Image.FromStream(imageStream);
    }
}
