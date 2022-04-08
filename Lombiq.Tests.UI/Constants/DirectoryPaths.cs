using System;
using System.IO;

namespace Lombiq.Tests.UI.Constants;

public static class DirectoryPaths
{
    public const string SetupSnapshot = nameof(SetupSnapshot);
    public const string Temp = nameof(Temp);
    public const string Screenshots = nameof(Screenshots);

    public static string GetTempSubDirectoryPath(string contextId, string subDirectoryName) =>
        Path.Combine(Environment.CurrentDirectory, Temp, contextId, subDirectoryName);

    public static string GetScreenshotsDirectoryPath(string contextId) =>
        GetTempSubDirectoryPath(contextId, Screenshots);
}
