using System.IO;
using System.Text;

namespace Lombiq.Tests.UI
{
    internal static class EmbeddedResourceProvider
    {
        internal static string ReadEmbeddedFile(string fileName)
        {
            var assembly = typeof(EmbeddedResourceProvider).Assembly;
            var resourceStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.{fileName}");

            using var reader = new StreamReader(resourceStream, Encoding.UTF8);

            return reader.ReadToEnd();
        }
    }
}
