using System.IO;

namespace Lombiq.Tests.UI.Extensions;

public static class StringExtensions
{
    public static string MakeFileSystemFriendly(this string text) =>
        string
            .Join("_", text.Split(Path.GetInvalidFileNameChars()))
            .Replace('.', '_')
            .Replace(' ', '-');
}
