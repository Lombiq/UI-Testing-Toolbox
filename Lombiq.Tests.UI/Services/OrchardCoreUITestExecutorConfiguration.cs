using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.SecurityScanning;
using Lombiq.Tests.UI.Services.GitHub;
using OpenQA.Selenium.BiDi.Modules.Log;
using OpenQA.Selenium.BiDi.Modules.Network;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Services;

public enum Browser
{
    // Chrome will be the default. Either don't change it being the first here, or assign 0 to it if you do.
    Chrome,
    Edge,
    Firefox,

    /// <summary>
    /// No browser will be used. Useful for testing things that don't require a browser, like API endpoints or running
    /// security scans.
    /// </summary>
    None,
}

public class OrchardCoreUITestExecutorConfiguration
{
    public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsAreEmptyAsync = app =>
        app.LogsShouldBeEmptyAsync(TestContext.Current.CancellationToken);

    public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsCanContainCacheFolderErrorsAsync =
        app => app.LogsShouldNotContainAsync(AppLogAssertionHelper.NotMediaCacheEntriesPredicate, TestContext.Current.CancellationToken);

    public static readonly Action<IEnumerable<Entry>> AssertBrowserLogIsEmpty =
        logEntries => logEntries.ShouldBeEmpty(logEntries.ToFormattedString());

    public static readonly Func<Entry, bool> IsNonSuccessBrowserLogEntry =
        entry =>
            entry.Level >= Level.Warn &&
            // HTML imports are somehow used by Selenium or something but this deprecation notice is always there for
            // every page.
            !entry.Text.ContainsOrdinalIgnoreCase("HTML Imports is deprecated");

    // The 404 is because of how browsers automatically request /favicon.ico even if a favicon is declared to be under a
    // different URL.
    public static readonly Func<ResponseCompletedEventArgs, bool> IsNonSuccessResponse = e =>
        e.Response.Status is < 200 or >= 400 && !e.Response.Url.EndsWithOrdinalIgnoreCase("/favicon.ico");

    public static readonly Action<IEnumerable<ResponseData>> AssertResponseLogIsEmpty =
        responses => responses.ShouldBeEmpty(responses.ToFormattedString());

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

    public Func<IWebApplicationInstance, Task> AssertAppLogsAsync { get; set; } = AssertAppLogsCanContainCacheFolderErrorsAsync;

    /// <summary>
    /// Selects which response data get saved to <see cref="UITestContext.CumulativeBrowserLog"/>.
    /// </summary>
    public Func<Entry, bool> BrowserLogFilter = IsNonSuccessBrowserLogEntry;

    public Action<IEnumerable<Entry>> AssertBrowserLog { get; set; } = AssertBrowserLogIsEmpty;

    /// <summary>
    /// Selects which response data get saved to <see cref="UITestContext.CumulativeResponseLog"/>.
    /// </summary>
    public Func<ResponseCompletedEventArgs, bool> ResponseLogFilter = IsNonSuccessResponse;

    public Action<IEnumerable<ResponseData>> AssertResponseLog { get; set; } = AssertResponseLogIsEmpty;

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

    private CancellationToken _testCancellationToken;

    /// <summary>
    /// Gets or sets a <see cref="CancellationToken"/> that cancels the test execution.
    /// </summary>
    // TestContext.Current shouldn't be cached, it always needs to be accessed as needed. So, we can't use a simple
    // property here.
    public CancellationToken TestCancellationToken
    {
        get => _testCancellationToken == default ? TestContext.Current.CancellationToken : _testCancellationToken;
        set => _testCancellationToken = value;
    }
}
