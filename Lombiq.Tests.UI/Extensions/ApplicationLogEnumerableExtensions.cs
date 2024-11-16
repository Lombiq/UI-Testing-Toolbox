using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class ApplicationLogEnumerableExtensions
{
    public static async Task<string> ToFormattedStringAsync(this IEnumerable<IApplicationLog> logs)
    {
        var logsArray = logs.ToArray();

        if (logsArray.Length == 1)
        {
            return Environment.NewLine + await LogLinesToFormattedStringAsync(logsArray[0]);
        }

        // Parallelization with Task.WhenAll() isn't really necessary for performance here but would potentially change
        // the order of the logs in the output.
        var logContents = logsArray.AwaitEachAsync(async log =>
            $"# Log name: {log.Name}" + Environment.NewLine + Environment.NewLine + await LogLinesToFormattedStringAsync(log));

        return string.Join(Environment.NewLine + Environment.NewLine, logContents);
    }

    private static async Task<string> LogLinesToFormattedStringAsync(IApplicationLog log) =>
        string.Join(Environment.NewLine, (await log.GetEntriesAsync()).Select(logEntry => logEntry.ToString()));
}
