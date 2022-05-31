using SixLabors.ImageSharp;
using System.Reflection;

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
}
