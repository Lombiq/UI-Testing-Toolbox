using Codeuctivity.ImageSharpCompare;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Processing;
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
    /// Creates a diff mask <see cref="Image"/> of two images.
    /// </summary>
    public static Image CalcDiffImage(this Image actual, Image expected)
    {
        using var actualStream = actual.ToStream();
        using var expectedStream = expected.ToStream();

        return ImageSharpCompare.CalcDiffMaskImage(actualStream, expectedStream);
    }

    /// <summary>
    /// Clones the <see cref="Image"/>.
    /// </summary>
    /// <param name="image">The source <see cref="Image"/> instance.</param>
    /// <returns>Cloned <see cref="Image"/> instance.</returns>
    public static Image Clone(this Image image) =>
        image.Clone(processingContext => { });

    /// <summary>
    /// Converts the <see cref="Image"/> to <see cref="Stream"/>.
    /// </summary>
    /// <param name="image">The source <see cref="Image"/> instance.</param>
    public static Stream ToStream(this Image image)
    {
        var imageStream = new MemoryStream();

        image.Save(imageStream, new BmpEncoder());

        imageStream.Seek(0, SeekOrigin.Begin);

        return imageStream;
    }
}
