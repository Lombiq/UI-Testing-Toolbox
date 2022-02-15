using Lombiq.Tests.UI.Extensions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services
{
    public enum Browser
    {
        Chrome,
        Edge,
        Firefox,
        InternetExplorer,
    }

    public class OrchardCoreUITestExecutorConfiguration
    {
        public BrowserConfiguration BrowserConfiguration { get; set; } = new();
        public TimeoutConfiguration TimeoutConfiguration { get; set; } = TimeoutConfiguration.Default;
        public AtataConfiguration AtataConfiguration { get; set; } = new();
        public OrchardCoreConfiguration OrchardCoreConfiguration { get; set; }

        public int MaxRetryCount { get; set; } =
            TestConfigurationManager.GetIntConfiguration("OrchardCoreUITestExecutorConfiguration:MaxRetryCount", 2);

        public TimeSpan RetryInterval { get; set; } =
            TimeSpan.FromSeconds(TestConfigurationManager.GetIntConfiguration("OrchardCoreUITestExecutorConfiguration:RetryIntervalSeconds", 0));

        public Func<IWebApplicationInstance, Task> AssertAppLogsAsync { get; set; } = AssertAppLogsCanContainWarningsAsync;
        public Action<IEnumerable<BrowserLogMessage>> AssertBrowserLog { get; set; } = AssertBrowserLogIsEmpty;
        public ITestOutputHelper TestOutputHelper { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to report <see
        /// href="https://www.jetbrains.com/help/teamcity/reporting-test-metadata.html">test metadata</see> to TeamCity
        /// as <see href="https://www.jetbrains.com/help/teamcity/service-messages.html">service messages</see>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For this to properly work the build artifacts should be configured to contain the FailureDumps folder (it
        /// can also contain other folders but it must contain a folder called "FailureDumps", e.g.:
        /// <c>+:FailureDumps => FailureDumps</c>.
        /// </para>
        /// </remarks>
        public bool ReportTeamCityMetadata { get; set; } =
            TestConfigurationManager.GetBoolConfiguration("OrchardCoreUITestExecutorConfiguration:ReportTeamCityMetadata", defaultValue: false);

        /// <summary>
        /// Gets or sets the configuration for the initial setup of the Orchard Core app under test.
        /// </summary>
        public OrchardCoreSetupConfiguration SetupConfiguration { get; set; } = new();

        public UITestExecutorFailureDumpConfiguration FailureDumpConfiguration { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether to launch and use a local SMTP service to test sending out e-mails.
        /// When enabled, the necessary configuration will be automatically passed to the tested app. See <see
        /// cref="SmtpServiceConfiguration"/> on configuring this.
        /// </summary>
        public bool UseSmtpService { get; set; }
        public SmtpServiceConfiguration SmtpServiceConfiguration { get; set; } = new();

        public AccessibilityCheckingConfiguration AccessibilityCheckingConfiguration { get; set; } = new();

        public HtmlValidationConfiguration HtmlValidationConfiguration { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether the test should verify the Orchard Core logs and the browser logs
        /// for errors after every page load. When enabled and there is an error the test is failed immediately which
        /// prevents false errors related to some expected web element not being present on the error page. Defaults to
        /// <see langword="true"/>.
        /// </summary>
        public bool RunAssertLogsOnAllPageChanges { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to use SQL Server as the app's database instead of the default
        /// SQLite. See <see cref="SqlServerDatabaseConfiguration"/> on configuring this.
        /// </summary>
        public bool UseSqlServer { get; set; }
        public SqlServerConfiguration SqlServerDatabaseConfiguration { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether to use Azure Blob Storage as the app's file storage instead of the
        /// default local file system. When enabled, the necessary configuration will be automatically passed to the
        /// tested app. See <see cref="AzureBlobStorageConfiguration"/> on configuring this.
        /// </summary>
        public bool UseAzureBlobStorage { get; set; }
        public AzureBlobStorageConfiguration AzureBlobStorageConfiguration { get; set; } = new();

        /// <summary>
        /// Gets or sets configuration for the <c>Lombiq.Tests.UI.Shortcuts</c> module. Note that you have to have it
        /// enabled in the app for these to work.
        /// </summary>
        public ShortcutsConfiguration ShortcutsConfiguration { get; set; } = new();

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

        public void AssertBrowserLogMaybe(IList<BrowserLogMessage> browserLogs, Action<string> log)
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

        public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsAreEmptyAsync = app => app.LogsShouldBeEmptyAsync();
        public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsCanContainWarningsAsync =
            app => app.LogsShouldBeEmptyAsync(canContainWarnings: true);

        public static readonly Action<IEnumerable<BrowserLogMessage>> AssertBrowserLogIsEmpty =
            // HTML imports are somehow used by Selenium or something but this deprecation notice is always there for
            // every page.
            messages => messages.ShouldNotContain(
                message => IsValidBrowserLogMessage(message),
                messages.Where(IsValidBrowserLogMessage).ToFormattedString());

        public static readonly Func<BrowserLogMessage, bool> IsValidBrowserLogMessage =
            message =>
                !message.Message.ContainsOrdinalIgnoreCase("HTML Imports is deprecated") &&
                // The 404 is because of how browsers automatically request /favicon.ico even if a favicon is declared
                // to be under a different URL.
                !message.IsNotFoundMessage("/favicon.ico");
    }
}
