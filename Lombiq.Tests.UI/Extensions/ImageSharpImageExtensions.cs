using Codeuctivity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using System.IO;

namespace Lombiq.Tests.UI.Extensions;

public static class ImageSharpImageExtensions
{
    /// <summary>
    /// Calculates the difference between the given images.
    /// </summary>
    public static ICompareResult CompareTo(this Image actual, Image expected)
    {
        using var actualStream = actual.ToStream();
        using var expectedStream = expected.ToStream();

        return ImageSharpCompare.CalcDiff(actualStream, expectedStream);
    }

    /// <summary>
    /// Creates a diff mask image of two images.
    /// </summary>
    public static Image CalcDiffImage(this Image actual, Image expected)
    {
        using var actualStream = actual.ToStream();
        using var expectedStream = expected.ToStream();

        return ImageSharpCompare.CalcDiffMaskImage(actualStream, expectedStream);
    }

    private static Stream ToStream(this Image image)
    {
        var imageStream = new MemoryStream();

        image.Save(imageStream, new BmpEncoder());

        imageStream.Seek(0, SeekOrigin.Begin);

        return imageStream;
    }
}
