using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

[Obsolete($"Use {nameof(LogEntry)} instead.")]
public class BrowserLogMessage
{
    public string Source { get; set; }

    public MessageLevel Level { get; set; }
    public DateTime DateTimeUtc { get; set; }
    public string Message { get; set; }

    public enum MessageLevel
    {
        Severe,
        Warning,
        Info,
    }
}

public static class LoggingWebDriverExtensions
{
    /// <summary>
    /// Retrieves the console logs from the browser. This log will contain all the log messages since the start of the
    /// session, not just the ones for the current page. NOTE that once you call this the log will be emptied and only
    /// subsequent entries will be included in it. Supports Chrome only.
    /// </summary>
    public static IEnumerable<LogEntry> GetAndEmptyBrowserLog(this IWebDriver driver) =>
        driver.Manage().Logs.GetLog(LogType.Browser);

    [Obsolete($"Use {nameof(GetAndEmptyBrowserLog)} instead.")]
    public static Task<IEnumerable<BrowserLogMessage>> GetAndEmptyBrowserLogAsync(this IWebDriver driver) =>
        throw new NotSupportedException($"Use {nameof(GetAndEmptyBrowserLog)} instead.");
}
