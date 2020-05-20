using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.Linq;

namespace Lombiq.Tests.UI.Helpers
{
    public static class OrchardCoreDirectoryHelper
    {
        /// <summary>
        /// Copies an Orchard Core application folder to a new location, without any generated or temporary files.
        /// </summary>
        /// <returns></returns>
        public static void CopyAppFolder(string sourcePath, string destinationPath)
        {
            var destinationAppData = Path.Combine(destinationPath, "App_Data");
            FileSystem.CopyDirectory(Path.Combine(sourcePath, "App_Data"), destinationAppData, true);

            // It's easier to delete the logs folder from the destination then to exclude it from the copy.
            var logsPath = Path.Combine(destinationAppData, "logs");
            DirectoryHelper.SafelyDeleteDirectoryIfExists(logsPath);

            CopyAppConfigFiles(sourcePath, destinationPath);
        }

        public static void CopyAppConfigFiles(string sourcePath, string destinationPath)
        {
            var configFilePaths = Directory
                .EnumerateFiles(sourcePath)
                .Where(filePath => filePath.EndsWith(".config") || filePath.EndsWith(".json"));

            foreach (var filePath in configFilePaths)
            {
                File.Copy(filePath, Path.Combine(destinationPath, Path.GetFileName(filePath)));
            }
        }

        public static string GetAppRootPath(string appAssemblyPath) =>
            Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(appAssemblyPath))));
    }
}
