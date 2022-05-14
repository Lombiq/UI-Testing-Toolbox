using System.IO;
using System.Runtime.InteropServices;

namespace Lombiq.Tests.UI.Services;

public class UITestExecutorFailureDumpConfiguration
{
    public static readonly char[] InvalidPathCharacters =
    {
        '\0',
        '\u0001',
        '\u0002',
        '\u0003',
        '\u0004',
        '\u0005',
        '\u0006',
        '\a',
        '\b',
        '\t',
        '\n',
        '\v',
        '\f',
        '\r',
        '\u000e',
        '\u000f',
        '\u0010',
        '\u0011',
        '\u0012',
        '\u0013',
        '\u0014',
        '\u0015',
        '\u0016',
        '\u0017',
        '\u0018',
        '\u0019',
        '\u001a',
        '\u001b',
        '\u001c',
        '\u001d',
        '\u001e',
        '\u001f',
        '|',
        '"',
        '<',
        '>',
        '*',
        '?',
    };

    /// <summary>
    /// Gets or sets a value indicating whether the subfolder of each test's dumps will use a shortened name, only
    /// containing the name of the test method, without the name of the test class and its namespace. This is to
    /// overcome the 260 character path length limitations on Windows. Defaults to <see langword="true"/> on Windows.
    /// </summary>
    public bool UseShortNames { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="DumpsDirectoryPath"/> should be stripped of all invalid
    /// characters (<see cref="InvalidPathCharacters"/>) regardless of the current OS's <see
    /// cref="Path.GetInvalidFileNameChars"/>. (On Linux that's only <c>\0</c> and <c>/</c> and everything else is ok,
    /// while on Windows it's a much longer array.)
    /// </summary>
    public bool StrictSanitizeDirectoryPath { get; set; } = true;

    public string DumpsDirectoryPath { get; set; } = "FailureDumps";
    public bool CaptureAppSnapshot { get; set; } = true;
    public bool CaptureScreenshots { get; set; } = true;
    public bool CaptureHtmlSource { get; set; } = true;
    public bool CaptureBrowserLog { get; set; } = true;
}
