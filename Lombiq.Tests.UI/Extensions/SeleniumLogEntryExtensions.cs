using OpenQA.Selenium.BiDi.Modules.Log;
using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Extensions;

public static class SeleniumLogEntryExtensions
{
    public static string ToFormattedString(this IEnumerable<Entry> logEntries) =>
        string.Join(Environment.NewLine, logEntries);

    public static bool IsNotFoundLogEntry(this Entry logEntry, string url) =>
        logEntry.Text.ContainsOrdinalIgnoreCase(
            @$"{url} - Failed to load resource: the server responded with a status of 404");
}
