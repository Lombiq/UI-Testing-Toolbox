using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.SecurityScanning;
using Lombiq.Tests.UI.Services.GitHub;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

public enum Browser
{
    // Chrome will be the default. Either don't change it being the first here, or assign 0 to it if you do.
    Chrome,
    Edge,
    Firefox,
    InternetExplorer,
}

public class OrchardCoreUITestExecutorConfiguration
{
    public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsAreEmptyAsync = app =>
        app.LogsShouldBeEmptyAsync();

    public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsCanContainWarningsAsync =
        app => app.LogsShouldBeEmptyAsync(canContainWarnings: true);

    public static readonly Action<IEnumerable<LogEntry>> AssertBrowserLogIsEmpty =
        logEntries => logEntries.ShouldNotContain(
            logEntry => IsValidBrowserLogEntry(logEntry),
            logEntries.Where(IsValidBrowserLogEntry).ToFormattedString());

    public static readonly Func<LogEntry, bool> IsValidBrowserLogEntry =
        logEntry =>
            logEntry.Level >= LogLevel.Warning &&
            // HTML imports are somehow used by Selenium or something but this deprecation notice is always there for
            // every page.
            !logEntry.Message.ContainsOrdinalIgnoreCase("HTML Imports is deprecated") &&
            // The 404 is because of how browsers automatically request /favicon.ico even if a favicon is declared to be
            // under a different URL.
            !logEntry.IsNotFoundLogEntry("/favicon.ico");

    /// <summary>
    /// Gets the global events available during UI test execution.
    /// </summary>
    public UITestExecutionEvents Events { get; } = new();

    /// <summary>
    /// Gets a dictionary storing some custom configuration data.
    /// </summary>
    [SuppressMessage(
        "Design",
        "MA0016:Prefer return collection abstraction instead of implementation",
        Justification = "Deliberately modifiable by consumer code.")]
    public Dictionary<string, object> CustomConfiguration { get; } = new();

    public BrowserConfiguration BrowserConfiguration { get; set; } = new();
    public TimeoutConfiguration TimeoutConfiguration { get; set; } = TimeoutConfiguration.Default;
    public AtataConfiguration AtataConfiguration { get; set; } = new();
    public OrchardCoreConfiguration OrchardCoreConfiguration { get; set; }

    public int MaxRetryCount { get; set; } =
        TestConfigurationManager.GetIntConfiguration(
            $"{nameof(OrchardCoreUITestExecutorConfiguration)}:{nameof(MaxRetryCount)}",
            2);

    public TimeSpan RetryInterval { get; set; } =
        TimeSpan.FromSeconds(TestConfigurationManager.GetIntConfiguration(
            $"{nameof(OrchardCoreUITestExecutorConfiguration)}:RetryIntervalSeconds",
            0));

    /// <summary>
    /// Gets or sets how many tests should run at the same time. Use a value of 0 to indicate that you would like the
    /// default behavior. Use a value of -1 to indicate that you do not wish to limit the number of tests running at the
    /// same time. The default behavior and 0 uses the <see cref="Environment.ProcessorCount"/> property. Set any other
    /// positive integer to limit to the exact number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The XUnit MaxParallelThreads property controls only the threads, not the actual processes started. See <see
    /// href="https://github.com/xunit/xunit/issues/2003"></see>.
    /// </para>
    /// <para>
    /// This is important only for UI tests as there will be a running instance of the site for each UI test, which can
    /// cause performance issues, like running out of memory.
    /// </para>
    /// </remarks>
    public int MaxParallelTests { get; set; } =
        TestConfigurationManager.GetIntConfiguration(
            $"{nameof(OrchardCoreUITestExecutorConfiguration)}:{nameof(MaxParallelTests)}") is { } intValue and > 0
            ? intValue
            : Environment.ProcessorCount;

    public Func<IWebApplicationInstance, Task> AssertAppLogsAsync { get; set; } = AssertAppLogsCanContainWarningsAsync;
    public Action<IEnumerable<LogEntry>> AssertBrowserLog { get; set; } = AssertBrowserLogIsEmpty;
    public ITestOutputHelper TestOutputHelper { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to report <see
    /// href="https://www.jetbrains.com/help/teamcity/reporting-test-metadata.html">test metadata</see> to TeamCity as
    /// <see href="https://www.jetbrains.com/help/teamcity/service-messages.html">service messages</see>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For this to properly work the build artifacts should be configured to contain the FailureDumps folder (it can
    /// also contain other folders but it must contain a folder called "FailureDumps", e.g.: <c>+:FailureDumps =&gt;
    /// FailureDumps</c>.
    /// </para>
    /// </remarks>
    public bool ReportTeamCityMetadata { get; set; } =
        TestConfigurationManager.GetBoolConfiguration("OrchardCoreUITestExecutorConfiguration:ReportTeamCityMetadata", defaultValue: false);

    /// <summary>
    /// Gets or sets a value indicating whether, when running in a GitHub Actions workflow, the workflow run output
    /// should be extended with test-level grouping and error annotations.
    /// </summary>
    public bool ExtendGitHubActionsOutput { get; set; } = true;

    public GitHubActionsOutputConfiguration GitHubActionsOutputConfiguration { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration for the initial setup of the Orchard Core app under test.
    /// </summary>
    public OrchardCoreSetupConfiguration SetupConfiguration { get; set; } = new();

    public UITestExecutorFailureDumpConfiguration FailureDumpConfiguration { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to launch and use a local SMTP service to test sending out e-mails. When
    /// enabled, the necessary configuration will be automatically passed to the tested app. See <see
    /// cref="SmtpServiceConfiguration"/> on configuring this.
    /// </summary>
    public bool UseSmtpService { get; set; }

    public SmtpServiceConfiguration SmtpServiceConfiguration { get; set; } = new();

    public AccessibilityCheckingConfiguration AccessibilityCheckingConfiguration { get; set; } = new();

    public HtmlValidationConfiguration HtmlValidationConfiguration { get; set; } = new();

    public SecurityScanningConfiguration SecurityScanningConfiguration { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the test should verify the Orchard Core logs and the browser logs for
    /// errors after every page load. When enabled and there is an error the test is failed immediately which prevents
    /// false errors related to some expected web element not being present on the error page. Defaults to <see
    /// langword="true"/>.
    /// </summary>
    public bool RunAssertLogsOnAllPageChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use SQL Server as the app's database instead of the default SQLite.
    /// See <see cref="SqlServerDatabaseConfiguration"/> on configuring this.
    /// </summary>
    public bool UseSqlServer { get; set; }

    public SqlServerConfiguration SqlServerDatabaseConfiguration { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to use Azure Blob Storage as the app's file storage instead of the
    /// default local file system. When enabled, the necessary configuration will be automatically passed to the tested
    /// app. See <see cref="AzureBlobStorageConfiguration"/> on configuring this.
    /// </summary>
    public bool UseAzureBlobStorage { get; set; }

    public AzureBlobStorageConfiguration AzureBlobStorageConfiguration { get; set; } = new();

    /// <summary>
    /// Gets or sets configuration for the <c>Lombiq.Tests.UI.Shortcuts</c> module. Note that you have to have it
    /// enabled in the app for these to work.
    /// </summary>
    public ShortcutsConfiguration ShortcutsConfiguration { get; set; } = new();

    public async Task AssertAppLogsMaybeAsync(IWebApplicationInstance instance, Action<string> log)
    {
        if (instance == null || AssertAppLogsAsync == null) return;

        try
        {
            await AssertAppLogsAsync(instance);
        }
        catch (Exception)
        {
            log("Application logs: " + Environment.NewLine);
            log(await instance.GetLogOutputAsync());

            throw;
        }
    }

    public void AssertBrowserLogMaybe(IList<LogEntry> browserLogs, Action<string> log)
    {
        if (AssertBrowserLog == null) return;

        try
        {
            AssertBrowserLog(browserLogs);
        }
        catch (Exception)
        {
            log("Browser logs: " + Environment.NewLine);
            log(browserLogs.ToFormattedString());

            throw;
        }
    }

    /// <summary>
    /// Similar to <see cref="AssertAppLogsCanContainWarningsAsync"/>, but also permits certain <c>|ERROR</c> log
    /// entries which represent correct reaction to incorrect or malicious user behavior during a security scan.
    /// </summary>
    public static Func<IWebApplicationInstance, Task> UseAssertAppLogsForSecurityScan(params string[] additionalPermittedErrorLines)
    {
        var permittedErrorLines = new List<string>
        {
            // The model binding will throw FormatException exception with this text during ZAP active scan, when
            // the bot tries to send malicious query strings or POST data that doesn't fit the types expected by the
            // model. This is correct, safe behavior and should be logged in production.
            "is not a valid value for Boolean",
            "An unhandled exception has occurred while executing the request. System.FormatException: any",
            // Happens when the static file middleware tries to access a path that doesn't exist or access a file as
            // a directory. Presumably this is an attempt to access protected files using source path manipulation.
            // This is handled by ASP.NET Core and there is nothing for us to worry about.
            "System.IO.IOException: Not a directory",
            // This happens when a request's model contains a dictionary and a key is missing. While this can be a
            // legitimate application error, during a security scan it's more likely the result of an incomplete
            // artificially constructed request. So the means the ASP.NET Core model binding is working as intended.
            "An unhandled exception has occurred while executing the request. System.ArgumentNullException: Value cannot be null. (Parameter 'key')",
        };

        permittedErrorLines.AddRange(additionalPermittedErrorLines);

        return app => app.LogsShouldBeEmptyAsync(canContainWarnings: true, permittedErrorLines);
    }
}
