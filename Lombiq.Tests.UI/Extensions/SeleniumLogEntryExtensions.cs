using OpenQA.Selenium;
using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Extensions;

public static class SeleniumLogEntryExtensions
{
    public static string ToFormattedString(this IEnumerable<LogEntry> messages) =>
        string.Join(Environment.NewLine, messages);

    public static bool IsNotFoundMessage(this LogEntry logEntry, string url) =>
        logEntry.Message.ContainsOrdinalIgnoreCase(
            @$"{url} - Failed to load resource: the server responded with a status of 404");
}
