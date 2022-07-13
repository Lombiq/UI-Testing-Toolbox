using SixLabors.ImageSharp;
using System.Drawing.Imaging;
using System.IO;
using DrawingImage = System.Drawing.Image;

namespace Lombiq.Tests.UI.Extensions;

public static class ImageExtensions
{
    /// <summary>
    /// Converts a <see cref="DrawingImage"/> to <see cref="Image"/>.
    /// </summary>
    /// <param name="image"><see cref="DrawingImage"/> instance to convert.</param>
    public static Image ToImageSharpImage(this DrawingImage image)
    {
        using var memoryStream = new MemoryStream();
        image.Save(memoryStream, ImageFormat.Bmp);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return Image.Load(memoryStream);
    }
}
