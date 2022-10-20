using SixLabors.ImageSharp;
using System.Reflection;

namespace Lombiq.Tests.UI.Extensions;

public static class AssemblyResourceExtensions
{
    /// <summary>
    /// Loads resource specified by <paramref name="name"/> from the given <paramref name="assembly"/>.
    /// </summary>
    public static Image GetResourceImageSharpImage(this Assembly assembly, string name)
    {
        using var resourceStream = assembly.GetManifestResourceStream(name);

        return Image.Load(resourceStream);
    }
}
