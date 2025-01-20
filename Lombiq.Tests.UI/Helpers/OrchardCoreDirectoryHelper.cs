using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Helpers;

internal static class OrchardCoreDirectoryHelper
{
    /// <summary>
    /// Copies an Orchard Core application folder to a new location, without any generated or temporary files.
    /// </summary>
    public static async Task CopyAppFolderAsync(string sourcePath, string destinationPath)
    {
        var destinationAppData = Path.Combine(destinationPath, "App_Data");
        FileSystem.CopyDirectory(Path.Combine(sourcePath, "App_Data"), destinationAppData, overwrite: true);

        // It's easier to delete the logs folder from the destination then to exclude it from the copy.
        var logsPath = Path.Combine(destinationAppData, "logs");
        await DirectoryHelper.SafelyDeleteDirectoryIfExistsAsync(logsPath);

        CopyAppConfigFiles(sourcePath, destinationPath);
    }

    public static void CopyAppConfigFiles(string sourcePath, string destinationPath)
    {
        var configFilePaths = Directory
            .EnumerateFiles(sourcePath)
            .Where(filePath =>
                filePath.EndsWithOrdinalIgnoreCase(".config") ||
                filePath.EndsWithOrdinalIgnoreCase(".json"));

        foreach (var filePath in configFilePaths)
        {
            File.Copy(filePath, Path.Combine(destinationPath, Path.GetFileName(filePath)));
        }

        var recipesSourceFolderPath = Path.Combine(sourcePath, "Recipes");
        if (Directory.Exists(recipesSourceFolderPath))
        {
            FileSystem.CopyDirectory(recipesSourceFolderPath, Path.Combine(destinationPath, "Recipes"), overwrite: true);
        }
    }

    public static string GetAppRootPath(string appAssemblyPath) =>
        Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(appAssemblyPath))));
}
