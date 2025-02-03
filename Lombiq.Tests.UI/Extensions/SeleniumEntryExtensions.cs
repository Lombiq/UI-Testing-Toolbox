using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium.BiDi.Modules.Log;
using OpenQA.Selenium.BiDi.Modules.Script;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Extensions;

public static class SeleniumEntryExtensions
{
    public static string ToFormattedString(this IEnumerable<Entry> logEntries) =>
        string.Join(Environment.NewLine, logEntries.Select(ToFormattedString));

    public static string ToFormattedString(this Entry entry) =>
        StringHelper.CreateInvariant($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} {entry.Level} {entry.Text}{FormatStackTrace(entry.StackTrace)}");

    public static bool IsNonSuccessBrowserLogEntry(this Entry entry) =>
        OrchardCoreUITestExecutorConfiguration.IsNonSuccessBrowserLogEntry(entry);

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
