using System;
using System.IO;

namespace Lombiq.Tests.UI.Helpers;

public static class WebAppConfigHelper
{
    private static readonly string[] Separator = ["src", "test"];

    /// <summary>
    /// Retrieves the absolute path to the assembly (DLL) of the application being tested.
    /// </summary>
    /// <param name="webAppName">The web app's project name.</param>
    /// <param name="frameworkFolderName">
    /// The name of the folder that corresponds to the .NET version in the build output folder (e.g. "net8.0").
    /// </param>
    /// <returns>The absolute path to the assembly (DLL) of the application being tested.</returns>
    public static string GetAbsoluteApplicationAssemblyPath(string webAppName, string frameworkFolderName = "net8.0")
    {
        string baseDirectory;

        if (File.Exists(webAppName + ".dll"))
        {
            baseDirectory = AppContext.BaseDirectory;
        }
        else
        {
            var outputFolderContainingPath = Path.Combine(
                AppContext.BaseDirectory.Split(Separator, StringSplitOptions.RemoveEmptyEntries)[0],
                "src",
                webAppName,
                "bin");

            baseDirectory = Path.Combine(outputFolderContainingPath, "Debug", frameworkFolderName);

            if (!Directory.Exists(baseDirectory))
            {
                baseDirectory = Path.Combine(outputFolderContainingPath, "Release", frameworkFolderName);
            }
        }

        return Path.Combine(baseDirectory, webAppName + ".dll");
    }
}
