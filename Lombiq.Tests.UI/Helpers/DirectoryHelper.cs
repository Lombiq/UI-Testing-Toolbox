using System;
using System.IO;
using System.Threading;

namespace Lombiq.Tests.UI.Helpers
{
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
                    // Even after the delete seemingly succeeding the folder can remain there with some empty
                    // subfolders. Perhaps this happens when one opens it in Explorer and that keeps a handle open.
                    if (!Directory.Exists(path)) return;
                }
                catch (DirectoryNotFoundException)
                {
                    // This means the directory was actually deleted.
                    return;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    // This means that somehow a process is still locking the content folder so let's wait and try
                    // again.
                    Thread.Sleep(1000);
                    tryCount++;

                    if (tryCount == maxTryCount)
                    {
                        throw new IOException(
                            $"The directory under {path} couldn't be cleaned up even after {maxTryCount} attempts.",
                            ex);
                    }
                }
            }
        }

        public static void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
