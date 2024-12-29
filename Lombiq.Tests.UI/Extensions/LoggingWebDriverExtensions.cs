using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
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
        driver is FirefoxDriver
            ? throw new NotSupportedException(
                "Firefox doesn't support getting the browser logs this way, and it will never support it, see " +
                "https://github.com/mozilla/geckodriver/issues/284. You can access")
            : driver.Manage().Logs.GetLog(LogType.Browser);
}
