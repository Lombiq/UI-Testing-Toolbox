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

    [Obsolete($"Use {nameof(UITestContext.TempDirectoryPath)} or {nameof(UITestContext.GetTempSubDirectoryPath)}() " +
        $"in {nameof(UITestContext)} instead.")]
    public static string GetTempDirectoryPath(params string[] subDirectoryNames) =>
        Path.Combine([Environment.CurrentDirectory, Temp, .. subDirectoryNames]);

    internal static string GetTempDirectoryPathWithFallback(string path) =>
        string.IsNullOrEmpty(path)
            ? Path.Combine(Environment.CurrentDirectory, Temp)
            : path;
}
