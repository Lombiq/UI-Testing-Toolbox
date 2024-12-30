using Lombiq.Tests.UI.Services;
using OpenQA.Selenium.BiDi.Modules.Log;
using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Extensions;

public static class SeleniumLogEntryExtensions
{
    public static string ToFormattedString(this IEnumerable<Entry> logEntries) =>
        string.Join(Environment.NewLine, logEntries);

    public static bool IsNonSuccessBrowserLogEntry(this Entry entry) =>
        OrchardCoreUITestExecutorConfiguration.IsNonSuccessBrowserLogEntry(entry);
}
