using System;
using System.IO;

namespace Lombiq.Tests.UI.Constants;

public static class Paths
{
    public const string DefaultSetupSnapshotDirectoryPath = "SetupSnapshot";
    public const string TempFolderPath = "Temp";

    public static string GetTempSubDirectoryPath(string contextId, string subDirectoryName) =>
        Path.Combine(Environment.CurrentDirectory, TempFolderPath, contextId, subDirectoryName);
}
