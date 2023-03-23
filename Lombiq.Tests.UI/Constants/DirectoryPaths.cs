using System;
using System.IO;
using System.Linq;

namespace Lombiq.Tests.UI.Constants;

public static class DirectoryPaths
{
    public const string SetupSnapshot = nameof(SetupSnapshot);
    public const string Temp = nameof(Temp);
    public const string Screenshots = nameof(Screenshots);

    public static string GetTempDirectoryPath(params string[] subDirectoryNames) =>
        Path.Combine(new[] { Environment.CurrentDirectory, Temp }.Concat(subDirectoryNames).ToArray());

    public static string GetTempSubDirectoryPath(string contextId, params string[] subDirectoryNames) =>
        GetTempDirectoryPath(new[] { contextId }.Concat(subDirectoryNames).ToArray());

    public static string GetScreenshotsDirectoryPath(string contextId) =>
        GetTempSubDirectoryPath(contextId, Screenshots);
}
