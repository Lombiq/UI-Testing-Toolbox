using System.Runtime.InteropServices;

namespace Lombiq.Tests.UI.Helpers;

/// <summary>
/// Functions that have different implementations/results based on the current operating system.
/// </summary>
public static class OperatingSystemHelpers
{
    /// <summary>
    /// Returns the file extension of an executable. Executable files end with <c>.exe</c> on Windows but with
    /// nothing on Linux and macOS.
    /// </summary>
    public static string GetExecutableExtension() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
}