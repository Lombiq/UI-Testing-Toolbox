using Lombiq.Tests.UI.Constants;
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
        public BrowserConfiguration BrowserConfiguration { get; set; } = new BrowserConfiguration();
        public TimeoutConfiguration TimeoutConfiguration { get; set; } = TimeoutConfiguration.Default;
        public AtataConfiguration AtataConfiguration { get; set; } = new AtataConfiguration();
        public OrchardCoreConfiguration OrchardCoreConfiguration { get; set; }

        public int MaxRetryCount { get; set; } =
            // Backwards compatibility with older MaxTryCount config.
            TestConfigurationManager.GetIntConfiguration("OrchardCoreUITestExecutorConfiguration.MaxTryCount") - 1 ??
            TestConfigurationManager.GetIntConfiguration("OrchardCoreUITestExecutorConfiguration.MaxRetryCount", 2);

        public Func<IWebApplicationInstance, Task> AssertAppLogs { get; set; } = AssertAppLogsCanContainWarnings;
        public Action<IEnumerable<BrowserLogMessage>> AssertBrowserLog { get; set; } = AssertBrowserLogIsEmpty;
        public ITestOutputHelper TestOutputHelper { get; set; }

        /// <summary>
        /// Gets or sets the Orchard setup operation so the result can be snapshot and used in subsequent tests.
        /// WARNING: It's highly recommended to put assertions at the end of it to pinpoint setup issues. Also see
        /// <see cref="FastFailSetup"/>.
        /// </summary>
        public Func<UITestContext, Uri> SetupOperation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether if a specific setup operations fails and exhausts <see
        /// cref="MaxRetryCount"/> globally then all tests using the same operation should fail immediately, without
        /// attempting to run it again. If set to <see langword="false"/> then every test will retry the setup until
        /// each tests' <see cref="MaxRetryCount"/> allows. If set to <see langword="true"/> then the setup operation
        /// will only be retried <see cref="MaxRetryCount"/> times (as set by the first test running that operation)
        /// altogether. Defaults to <see langword="true"/>.
        /// </summary>
        public bool FastFailSetup { get; set; } = true;

        public string SetupSnapshotPath { get; set; } = Snapshots.DefaultSetupSnapshotPath;
        public UITestExecutorFailureDumpConfiguration FailureDumpConfiguration { get; set; } = new UITestExecutorFailureDumpConfiguration();
        public bool UseSmtpService { get; set; }
        public SmtpServiceConfiguration SmtpServiceConfiguration { get; set; } = new SmtpServiceConfiguration();
        public AccessibilityCheckingConfiguration AccessibilityCheckingConfiguration { get; set; } = new AccessibilityCheckingConfiguration();
        public bool UseSqlServer { get; set; }
        public SqlServerConfiguration SqlServerDatabaseConfiguration { get; set; } = new SqlServerConfiguration();

        /// <summary>
        /// Gets or sets a value indicating whether to use Azure Blob Storage as the app's file storage instead of the
        /// default local file system.
        /// See <see cref="AzureBlobStorageConfiguration"/> on configuring this.
        /// </summary>
        public bool UseAzureBlobStorage { get; set; }
        public AzureBlobStorageConfiguration AzureBlobStorageConfiguration { get; set; } = new();

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
