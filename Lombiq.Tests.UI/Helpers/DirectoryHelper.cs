using System;
using System.IO;
using System.Threading;

namespace Lombiq.Tests.UI.Helpers;

public static class DirectoryHelper
{
    public static void SafelyDeleteDirectoryIfExists(string path, int maxTryCount = 10)
    {
        if (!Directory.Exists(path)) return;

        var tryCount = 0;

        while (true)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                // Even after the delete seemingly succeeding the folder can remain there with some empty subfolders.
                // Perhaps this happens when one opens it in Windows Explorer and that keeps a handle open.
                if (!Directory.Exists(path)) return;
            }
            catch (DirectoryNotFoundException)
            {
                // This means the directory was actually deleted.
                return;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // This means that somehow a process is still locking the content folder so let's wait and try again.
                Thread.Sleep(1000);
                tryCount++;

                if (tryCount == maxTryCount)
                {
                    throw new IOException(
                        $"The directory under {path} couldn't be cleaned up even after {maxTryCount.ToTechnicalString()} attempts.",
                        ex);
                }
            }
        }
    }

    /// <summary>
    /// Creates a directory with the given path and a numeric suffix from 1 to <see cref="int.MaxValue"/>. This means if
    /// the <paramref name="path"/> is <c>c:\MyDirectory</c> then it will first attempt to create <c>c:\MyDirectory1</c>
    /// but if that already exists it will try to create <c>c:\MyDirectory2</c> instead, and so on.
    /// </summary>
    /// <param name="path">The base path to use when constructing the final path.</param>
    /// <returns>The final path created.</returns>
    public static string CreateEnumeratedDirectory(string path)
    {
        for (var i = 1; i < int.MaxValue; i++)
        {
            var newPath = path + i.ToTechnicalString();
            if (Directory.Exists(newPath)) continue;

            Directory.CreateDirectory(newPath);
            return newPath;
        }
    }
}
