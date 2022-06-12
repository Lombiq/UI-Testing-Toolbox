using System;
using System.IO;
using System.Linq;

namespace Lombiq.Tests.UI.Extensions;

public static class StringExtensions
{
    // The first array contains every invalid character in Windows. This is the union of GetInvalidFileNameChars() and
    // GetInvalidPathChars() evaluated on Windows (for FAT and NTFS). Linux file systems aren't as strict, they only
    // forbid forward slash ('/') and null character ('\0'). This is a subset of the invalid characters on Windows. So
    // to make the paths work across all systems (e.g. when we plan to back up to an NTFS store or use GitHub's
    // "actions/upload-artifact" action) we have to carry the banned Windows characters across operating systems.
    // GetInvalidFileNameChars() and GetInvalidPathChars() are still included in case they contain more characters on
    // untested operating systems.
    private static readonly Lazy<char[]> _invalidPathCharacters = new(() => new[]
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
            ':',
            '"',
            '<',
            '>',
            '*',
            '?',
        }
        .Union(Path.GetInvalidFileNameChars())
        .Union(Path.GetInvalidPathChars())
        .ToArray());

    public static string MakeFileSystemFriendly(this string text) =>
        string
            .Join("_", text.Split(_invalidPathCharacters.Value))
            .Replace('.', '_')
            .Replace(' ', '-');

    // Concatenates an array of strings, using the specified separator between each member. Empty or null strings are
    // filtered out.
    public static string JoinNotEmptySafe(this string[] strings, string separator = "") =>
        string
            .Join(separator, strings.Where(item => !string.IsNullOrEmpty(item)));
}
