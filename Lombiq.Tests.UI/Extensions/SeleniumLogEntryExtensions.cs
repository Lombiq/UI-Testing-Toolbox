using OpenQA.Selenium;
using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Extensions;

public static class SeleniumLogEntryExtensions
{
    public static string ToFormattedString(this IEnumerable<LogEntry> logEntries) =>
        string.Join(Environment.NewLine, logEntries);

    public static bool IsNotFoundLogEntry(this LogEntry logEntry, string url) =>
        logEntry.Message.ContainsOrdinalIgnoreCase(
            @$"{url} - Failed to load resource: the server responded with a status of 404");

    [Obsolete("Use IsNotFoundLogEntry() instead.")]
    public static bool IsNotFoundMessage(this LogEntry logEntry, string url) => logEntry.IsNotFoundLogEntry(url);
}
