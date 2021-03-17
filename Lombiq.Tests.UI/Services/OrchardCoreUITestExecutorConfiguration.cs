using Lombiq.Tests.UI.Extensions;
using Shouldly;
using System;
using System.Collections.Generic;
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

        public Func<IWebApplicationInstance, Task> AssertAppLogs { get; set; } = AssertAppLogsCanContainWarnings;
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
            TestConfigurationManager.GetBoolConfiguration("OrchardCoreUITestExecutorConfiguration:ReportTeamCityMetadata", false);

        /// <summary>
        /// Gets or sets the configuration for the initial setup of the Orchard Core app under test.
        /// </summary>
        public OrchardCoreSetupConfiguration SetupConfiguration { get; set; } = new();

        public UITestExecutorFailureDumpConfiguration FailureDumpConfiguration { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether to launch and use a local SMTP service to test sending out e-mails.
        /// See <see cref="SmtpServiceConfiguration"/> on configuring this.
        /// </summary>
        public bool UseSmtpService { get; set; }
        public SmtpServiceConfiguration SmtpServiceConfiguration { get; set; } = new();

        public AccessibilityCheckingConfiguration AccessibilityCheckingConfiguration { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether to use SQL Server as the app's database instead of the default
        /// SQLite. See <see cref="SqlServerDatabaseConfiguration"/> on configuring this.
        /// </summary>
        public bool UseSqlServer { get; set; }
        public SqlServerConfiguration SqlServerDatabaseConfiguration { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether to use Azure Blob Storage as the app's file storage instead of the
        /// default local file system.
        /// See <see cref="AzureBlobStorageConfiguration"/> on configuring this.
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
            if (instance == null || AssertAppLogs == null) return;

            try
            {
                await AssertAppLogs(instance);
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

        public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsAreEmpty = app => app.LogsShouldBeEmptyAsync();
        public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsCanContainWarnings = app => app.LogsShouldBeEmptyAsync(true);

        public static readonly Action<IEnumerable<BrowserLogMessage>> AssertBrowserLogIsEmpty =
            // HTML imports are somehow used by Selenium or something but this deprecation notice is always there for
            // every page.
            messages => messages.ShouldNotContain(message =>
                message.Source != BrowserLogMessage.Sources.Deprecation ||
                !message.Message.Contains("HTML Imports is deprecated", StringComparison.InvariantCultureIgnoreCase));
    }

    public class UITestExecutorFailureDumpConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether the subfolder of each test's dumps will use a shortened name, only
        /// containing the name of the test method, without the name of the test class and its namespace. This is to
        /// overcome the 260 character path length limitations on Windows. Defaults to <see langword="true"/>.
        /// </summary>
        public bool UseShortNames { get; set; } = true;

        public string DumpsDirectoryPath { get; set; } = "FailureDumps";
        public bool CaptureAppSnapshot { get; set; } = true;
        public bool CaptureScreenshot { get; set; } = true;
        public bool CaptureHtmlSource { get; set; } = true;
        public bool CaptureBrowserLog { get; set; } = true;
    }
}
