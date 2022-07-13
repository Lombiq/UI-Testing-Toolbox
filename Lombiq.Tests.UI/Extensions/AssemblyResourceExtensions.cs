using SixLabors.ImageSharp;
using System.Reflection;

using DrawingBitmap = System.Drawing.Bitmap;
using DrawingImage = System.Drawing.Image;

namespace Lombiq.Tests.UI.Extensions;

public static class AssemblyResourceExtensions
{
    /// <summary>
    /// Loads resource specified by name from the given assembly.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <returns><see cref="Image"/> instance.</returns>
    public static Image GetResourceImageSharpImage(this Assembly assembly, string name)
    {
        using var resourceStream = assembly.GetManifestResourceStream(name);

        return Image.Load(resourceStream);
    }

    /// <summary>
    /// Loads resource specified by name from the given assembly.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <returns><see cref="DrawingBitmap"/> instance.</returns>
    public static DrawingBitmap TryGetResourceBitmap(this Assembly assembly, string name)
    {
        var resourceStream = assembly.GetManifestResourceStream(name);

        if (resourceStream == null)
        {
            return null;
        }

        return (DrawingBitmap)DrawingImage.FromStream(resourceStream);
    }
}
