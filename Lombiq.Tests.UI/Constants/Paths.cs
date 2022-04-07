using System;
using System.IO;

namespace Lombiq.Tests.UI.Constants;

public static class Paths
{
    public const string DefaultSetupSnapshotDirectoryPath = "SetupSnapshot";
    public const string TempDirectoryPath = "Temp";
    public const string ScreenshotsDirectoryName = "Screenshots";

    public static string GetTempSubDirectoryPath(string contextId, string subDirectoryName) =>
        Path.Combine(Environment.CurrentDirectory, TempDirectoryPath, contextId, subDirectoryName);

    public static string GetScreenshotsDirectoryPath(string contextId) =>
        GetTempSubDirectoryPath(contextId, ScreenshotsDirectoryName);
}
