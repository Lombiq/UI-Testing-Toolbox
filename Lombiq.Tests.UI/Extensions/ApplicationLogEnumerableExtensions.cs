using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class ApplicationLogEnumerableExtensions
{
    public static string ToFormattedStringCached(this IEnumerable<MemoryApplicationLog> logs)
    {
        var logsArray = logs.ToArray();

        if (logsArray.Length == 1)
        {
            return Environment.NewLine + logsArray[0].ToFormattedString();
        }

        var logContents = logsArray.Select(log =>
            $"# Log name: {log.Name}" + Environment.NewLine + Environment.NewLine + log.ToFormattedString());

        return string.Join(Environment.NewLine + Environment.NewLine, logContents);
    }

    public static async Task<string> ToFormattedStringAsync(this IEnumerable<IApplicationLog> logs)
    {
        var cached = await logs.AwaitEachAsync(CachedApplicationLog.FromLogAsync);
        return cached.ToFormattedStringCached();
    }

    private static string ToFormattedString(this MemoryApplicationLog log) =>
        string.Join(Environment.NewLine, log.Entries.Select(logEntry => logEntry.ToString()));
}
