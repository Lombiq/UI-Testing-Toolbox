using OpenQA.Selenium;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Extensions;

public static class LoggingWebDriverExtensions
{
    /// <summary>
    /// Retrieves the console logs from the browser. This log will contain all the log messages since the start of the
    /// session, not just the ones for the current page. NOTE that once you call this the log will be emptied and only
    /// subsequent entries will be included in it. Supports Chrome only.
    /// </summary>
    public static IEnumerable<LogEntry> GetAndEmptyBrowserLog(this IWebDriver driver) =>
        driver.Manage().Logs.GetLog(LogType.Browser);
}
