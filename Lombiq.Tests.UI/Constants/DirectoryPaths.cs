using System;
using System.IO;
using System.Linq;

namespace Lombiq.Tests.UI.Constants;

public static class DirectoryPaths
{
    public const string SetupSnapshot = nameof(SetupSnapshot);
    public const string Temp = nameof(Temp);
    public const string Screenshots = nameof(Screenshots);

    public static string GetTempSubDirectoryPath(string contextId, params string[] subDirectoryNames) =>
        Path.Combine(new[] { Environment.CurrentDirectory, Temp, contextId }.Union(subDirectoryNames).ToArray());

    public static string GetScreenshotsDirectoryPath(string contextId) =>
        GetTempSubDirectoryPath(contextId, Screenshots);
}
