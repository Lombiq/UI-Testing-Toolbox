using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium.BiDi.Log;
using OpenQA.Selenium.BiDi.Script;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Models;

public class BrowserLogEntry
{
    public Level Level { get; init; }
    public Source Source { get; init; }
    public string Text { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public StackTrace StackTrace { get; set; }

    internal BrowserLogEntry(LogEntry entry)
    {
        Level = entry.Level;
        Source = entry.Source;
        Text = entry.Text;
        Timestamp = entry.Timestamp;
        StackTrace = entry.StackTrace;
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
            "Stack trace: " +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                stackTrace.CallFrames.Select(frame =>
                    "- " +
                    (string.IsNullOrEmpty(frame.FunctionName) ? string.Empty : frame.FunctionName + " at ") +
                    StringHelper.CreateInvariant($"{frame.Url}:{frame.LineNumber}:{frame.ColumnNumber}")));
    }
}

public static class BrowserLogEntryEnumerableExtensions
{
    public static string ToFormattedString(this IEnumerable<BrowserLogEntry> logEntries) =>
        string.Join(Environment.NewLine, logEntries.Select(entry => entry.ToFormattedString()));
}
