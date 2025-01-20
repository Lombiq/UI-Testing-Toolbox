using Lombiq.Tests.UI.Services;
using System;
using System.IO;

namespace Lombiq.Tests.UI.Constants;

public static class DirectoryPaths
{
    public const string SetupSnapshot = nameof(SetupSnapshot);
    public const string Temp = nameof(Temp);
    public const string Screenshots = nameof(Screenshots);
    public const string Downloads = nameof(Downloads);

    public static string GetTempDirectoryPath(params string[] subDirectoryNames) =>
        Path.Combine([Environment.CurrentDirectory, Temp, .. subDirectoryNames]);

    [Obsolete($"Use {nameof(UITestContext)}.{nameof(UITestContext.GetTempSubDirectoryPath)}() instead.")]
    public static string GetTempSubDirectoryPath(string contextId, params string[] subDirectoryNames) =>
        GetTempDirectoryPath([contextId, .. subDirectoryNames]);

    [Obsolete($"Use {nameof(UITestContext)}.{nameof(UITestContext.ScreenshotsDirectoryPath)} instead.")]
    public static string GetScreenshotsDirectoryPath(string contextId) =>
        GetTempSubDirectoryPath(contextId, Screenshots);
}
