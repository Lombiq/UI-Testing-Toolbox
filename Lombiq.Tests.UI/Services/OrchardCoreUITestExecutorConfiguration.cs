using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
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

    /// <summary>
    /// No browser will be used. Useful for testing things that don't require a browser, like API endpoints or running
    /// security scans.
    /// </summary>
    None,
}

public class OrchardCoreUITestExecutorConfiguration
{
    public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsAreEmptyAsync = app =>
        app.LogsShouldBeEmptyAsync();

    public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsCanContainCacheFolderErrorsAsync =
        app => app.LogsShouldNotContainAsync(AppLogAssertionHelper.NotMediaCacheEntriesPredicate);

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
    public Dictionary<string, object> CustomConfiguration { get; } = [];

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
    [Obsolete("As of xUnit v2.8, the \"conservative\" parallelism algorithm is used by default, which limits the " +
        "number of tests started (not currently running, as before) parallel tests. This feature is no longer needed " +
        "and will be removed in a future version. Set maxParallelThreads in your test project's xunit.runner.json " +
        "instead (see https://xunit.net/docs/running-tests-in-parallel).")]
    // When removing this property, also remove the "ui-test-parallelism" config from Lombiq GitHub Actions.
    public int MaxParallelTests { get; set; } =
        TestConfigurationManager.GetIntConfiguration(
            $"{nameof(OrchardCoreUITestExecutorConfiguration)}:{nameof(MaxParallelTests)}") is { } intValue and > 0
            ? intValue
            : Environment.ProcessorCount;

    public Func<IWebApplicationInstance, Task> AssertAppLogsAsync { get; set; } = AssertAppLogsCanContainCacheFolderErrorsAsync;
    public Action<IEnumerable<LogEntry>> AssertBrowserLog { get; set; } = AssertBrowserLogIsEmpty;
    public ITestOutputHelper TestOutputHelper { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to report <see
    /// href="https://www.jetbrains.com/help/teamcity/reporting-test-metadata.html">test metadata</see> to TeamCity as
    /// <see href="https://www.jetbrains.com/help/teamcity/service-messages.html">service messages</see>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For this to properly work the build artifacts should be configured to contain the TestDumps folder (it can also
    /// contain other folders but it must contain a folder called "TestDumps", e.g.: <c>+:TestDumps =&gt; TestDumps</c>.
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

    public UITestExecutorTestDumpConfiguration TestDumpConfiguration { get; set; } = new();

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
}
