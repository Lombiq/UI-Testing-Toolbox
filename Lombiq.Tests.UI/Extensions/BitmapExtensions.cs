using SixLabors.ImageSharp;
using System.Drawing.Imaging;
using System.IO;

// We can't import the whole System.Drawing namespace because both of SixLabors.ImageSharp and System.Drawing are
// contains Image class, but we want it and some other from SixLabors.ImageSharp.
// So we import only Bitmap from System.Drawing here.
using Bitmap = System.Drawing.Bitmap;

// The SixLabors.ImageSharp.Web v2.0.0 which is depends on SixLabors.ImageSharp v2.1.1 will be introduced in Orchard
// in the future.
// See: https://github.com/OrchardCMS/OrchardCore/pull/11585
// When it will be done and we change the new version of OC in Lombiq.ChartJs.Samples, we should change from
// ImageSharpCompare v1.2.11 to Codeuctivity.ImageSharpCompare v2.0.46 in this project

namespace Lombiq.Tests.UI.Extensions;

public static class BitmapExtensions
{
    /// <summary>
    /// Converts a System.Drawing.Bitmap to SixLabors.ImageSharp.Image.
    /// </summary>
    /// <param name="bitmap">Bitmap instance to convert.</param>
    public static Image ToImageSharpImage(this Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Bmp);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return Image.Load(memoryStream);
    }
}
