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
        InternetExplorer
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
            TestConfigurationManager.GetIntConfiguration("OrchardCoreUITestExecutorConfiguration.MaxRetryCount", 5);
        public Func<IWebApplicationInstance, Task> AssertAppLogs { get; set; } = AssertAppLogsCanContainWarnings;
        public Action<IEnumerable<BrowserLogMessage>> AssertBrowserLog { get; set; } = AssertBrowserLogIsEmpty;
        public ITestOutputHelper TestOutputHelper { get; set; }
        public Func<UITestContext, Uri> SetupOperation { get; set; }
        public string SetupSnapshotPath { get; set; } = Snapshots.DefaultSetupSnapshotPath;
        public UITestExecutorFailureDumpConfiguration FailureDumpConfiguration { get; set; } = new UITestExecutorFailureDumpConfiguration();
        public bool UseSmtpService { get; set; }
        public SmtpServiceConfiguration SmtpServiceConfiguration { get; set; } = new SmtpServiceConfiguration();
        public AccessibilityCheckingConfiguration AccessibilityCheckingConfiguration { get; set; } = new AccessibilityCheckingConfiguration();


        public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsAreEmpty = app => app.LogsShouldBeEmpty();
        public static readonly Func<IWebApplicationInstance, Task> AssertAppLogsCanContainWarnings = app => app.LogsShouldBeEmpty(true);

        public static readonly Action<IEnumerable<BrowserLogMessage>> AssertBrowserLogIsEmpty =
            // HTML imports are somehow used by Selenium or something but this deprecation notice is always there for
            // every page.
            messages => messages.ShouldNotContain(message =>
                message.Source != BrowserLogMessage.Sources.Deprecation ||
                !message.Message.Contains("HTML Imports is deprecated"));
    }


    public class UITestExecutorFailureDumpConfiguration
    {
        /// <summary>
        /// When set to <c>true</c> the subfolder of each test's dumps will use a shortened name, only containing the
        /// name of the test method, without the name of the test class and its namespace. This is to overcome the 260
        /// character path length limitations on Windows. Defaults to <c>true</c>.
        /// </summary>
        public bool UseShortNames { get; set; } = true;

        public string DumpsDirectoryPath { get; set; } = "FailureDumps";
        public bool CaptureAppSnapshot { get; set; } = true;
        public bool CaptureScreenshot { get; set; } = true;
        public bool CaptureHtmlSource { get; set; } = true;
        public bool CaptureBrowserLog { get; set; } = true;
    }
}
