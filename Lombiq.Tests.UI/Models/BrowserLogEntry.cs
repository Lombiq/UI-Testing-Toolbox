using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium.BiDi.Log;
using OpenQA.Selenium.BiDi.Script;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Models;

public record BrowserLogEntry(
    Level Level,
    Source Source,
    string Text,
    DateTimeOffset Timestamp,
    StackTrace StackTrace)
{
    internal BrowserLogEntry(EntryAddedEventArgs entry)
        : this(entry.Level, entry.Source, entry.Text, entry.Timestamp, entry.StackTrace)
    {
    }

    public string ToFormattedString() =>
        StringHelper.CreateInvariant($"{Timestamp:yyyy-MM-dd HH:mm:ss} {Level} {Text}{FormatStackTrace(StackTrace)}");

    public bool IsNonSuccessBrowserLogEntry() =>
        OrchardCoreUITestExecutorConfiguration.IsNonSuccessBrowserLogEntry(this);

    private static string FormatStackTrace(StackTrace stackTrace)
    {
        if (stackTrace == null) return string.Empty;

        return
            Environment.NewLine +
            "Stack trace:" +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                stackTrace.CallFrames.Select(frame =>
                    "- " +
                    (string.IsNullOrEmpty(frame.FunctionName) ? string.Empty : $"{frame.FunctionName} at ") +
                    // False positive, see https://github.com/meziantou/Meziantou.Analyzer/issues/1316. Remove once
                    // fixed.
#pragma warning disable MA0075
                    StringHelper.CreateInvariant($"{frame.Url}:{frame.LineNumber}:{frame.ColumnNumber}")));
#pragma warning restore MA0075
    }
}

public static class BrowserLogEntryEnumerableExtensions
{
    public static string ToFormattedString(this IEnumerable<BrowserLogEntry> logEntries) =>
        string.Join(Environment.NewLine, logEntries.Select(entry => entry.ToFormattedString()));
}
