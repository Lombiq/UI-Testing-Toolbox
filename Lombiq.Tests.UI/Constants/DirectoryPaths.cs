using System;
using System.IO;

namespace Lombiq.Tests.UI.Constants;

public static class DirectoryPaths
{
    public const string SetupSnapshot = nameof(SetupSnapshot);
    public const string Temp = nameof(Temp);
    public const string Screenshots = nameof(Screenshots);
    public const string Downloads = nameof(Downloads);

    internal static string GetTempDirectoryPathWithFallback(string path) =>
        string.IsNullOrEmpty(path)
            ? Path.Combine(Environment.CurrentDirectory, Temp)
            : path;
}
