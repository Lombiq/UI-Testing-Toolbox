using OpenQA.Selenium.Internal.Logging;

namespace Lombiq.Tests.UI.Services;

public static class SeleniumLogConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether Selenium logging is enabled (with the <see cref="LogEventLevel.Trace"/>
    /// level by default). This can be useful to debug low-level issues.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note that since the Selenium log is global, there will be a single, cumulative log for all tests. This is best
    /// used when troubleshooting a single test.
    /// </para>
    /// </remarks>
    public static bool IsEnabled { get; set; } =
        TestConfigurationManager.GetBoolConfiguration("SeleniumLogConfiguration:IsEnabled", defaultValue: false);

    /// <summary>
    /// Gets or sets the <see cref="LogEventLevel"/> for the global Selenium log.
    /// </summary>
    public static LogEventLevel LogEventLevel { get; set; } =
        TestConfigurationManager.GetConfiguration("SeleniumLogConfiguration:LogEventLevel", LogEventLevel.Trace);
}
