using System;
using System.IO;

namespace Lombiq.Tests.UI.Constants;

public static class DirectoryPaths
{
    public const string SetupSnapshot = nameof(SetupSnapshot);
    public const string Temp = nameof(Temp);
    public const string Screenshots = nameof(Screenshots);

    public static string GetTempDirectoryPath(params string[] subDirectoryNames) =>
        Path.Combine(
            Path.Combine(Environment.CurrentDirectory, Temp),
            Path.Combine(subDirectoryNames));

    public static string GetTempSubDirectoryPath(string contextId, params string[] subDirectoryNames) =>
        GetTempDirectoryPath(
            Path.Combine(contextId, Path.Combine(subDirectoryNames)));

    public static string GetScreenshotsDirectoryPath(string contextId) =>
        GetTempSubDirectoryPath(contextId, Screenshots);
}
