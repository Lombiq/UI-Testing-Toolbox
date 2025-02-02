using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium.BiDi.Modules.Log;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Extensions;

public static class SeleniumEntryExtensions
{
    public static string ToFormattedString(this IEnumerable<Entry> logEntries) =>
        string.Join(Environment.NewLine, logEntries.Select(ToFormattedString));

    public static string ToFormattedString(this Entry entry) =>
        StringHelper.CreateInvariant($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} {entry.Level} {entry.Text}");

    public static bool IsNonSuccessBrowserLogEntry(this Entry entry) =>
        OrchardCoreUITestExecutorConfiguration.IsNonSuccessBrowserLogEntry(entry);
}
