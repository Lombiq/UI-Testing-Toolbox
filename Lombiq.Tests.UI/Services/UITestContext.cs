using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.SecurityScanning;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

public class UITestContext
{
    private readonly List<LogEntry> _historicBrowserLog = [];

    /// <summary>
    /// Gets the globally unique ID of this context. You can use this ID to refer to the current text execution in
    /// external systems, or in file names.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the current retry count. This should only be edited from <c>UITestExecutionSession</c>.
    /// </summary>
    internal int RetryCount { get; set; }

    /// <summary>
    /// Gets a value indicating whether there may be further retries for this test.
    /// </summary>
    public bool IsFinalTry => RetryCount >= Configuration?.MaxRetryCount;

    /// <summary>
    /// Gets data about the currently executing test.
    /// </summary>
    public UITestManifest TestManifest { get; }

    /// <summary>
    /// Gets the configuration of the test execution.
    /// </summary>
    public OrchardCoreUITestExecutorConfiguration Configuration { get; }

    /// <summary>
    /// Gets the context for the currently used SQL Server instance and database, if SQL Server is the DB used for the
    /// test.
    /// </summary>
    public SqlServerRunningContext SqlServerRunningContext { get; }

    /// <summary>
    /// Gets the web application instance, e.g. an Orchard Core app currently running.
    /// </summary>
    public IWebApplicationInstance Application { get; }

    /// <summary>
    /// Gets a representation of a scope wrapping an Atata-driven UI test.
    /// </summary>
    public AtataScope Scope { get; }

    /// <summary>
    /// Gets the Selenium web driver driving the app via a browser.
    /// </summary>
    public IWebDriver Driver => Scope.Driver;

    /// <summary>
    /// Gets a value indicating whether a browser is currently running for the test. <see langword="false"/> means that
    /// no browser was launched. Note that since the browser is only started on demand, with the first operation
    /// requiring it, a browser might not be currently running even if <see cref="IsBrowserConfigured"/> suggests it
    /// should.
    /// </summary>
    public bool IsBrowserRunning => Scope.IsBrowserRunning;

    /// <summary>
    /// Gets a value indicating whether a browser is configured to be used for the test. <see langword="false"/> means
    /// that no browser will be launched. Note that since the browser is only started on demand, with the first
    /// operation requiring it, a browser might not be currently running even if this suggests it should. Check <see
    /// cref="IsBrowserRunning"/>" to check for that.
    /// </summary>
    public bool IsBrowserConfigured => Configuration.BrowserConfiguration.Browser != Browser.None;

    /// <summary>
    /// Gets the context for the SMTP service running for the test, if it was requested.
    /// </summary>
    public SmtpServiceRunningContext SmtpServiceRunningContext { get; }

    /// <summary>
    /// Gets the context for the currently used Azure Blob Storage folder, if Blob Storage is used for the test.
    /// </summary>
    public AzureBlobStorageRunningContext AzureBlobStorageRunningContext { get; }

    /// <summary>
    /// Gets the service to manage <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see> instances for
    /// security scanning. Usually, it's recommended to use the higher-level ZAP <see
    /// cref="SecurityScanningUITestContextExtensions"/> extension methods instead.
    /// </summary>
    public ZapManager ZapManager { get; }

    /// <summary>
    /// Gets a cumulative log of browser console entries.
    /// </summary>
    public IReadOnlyList<LogEntry> HistoricBrowserLog => _historicBrowserLog;

    /// <summary>
    /// Gets a dictionary storing some custom contextual data.
    /// </summary>
    [SuppressMessage(
        "Design",
        "MA0016:Prefer return collection abstraction instead of implementation",
        Justification = "Deliberately modifiable by consumer code.")]
    public Dictionary<string, object> CustomContext { get; } = [];

    /// <summary>
    /// Gets a dictionary storing some custom data for collecting in the failure dump.
    /// </summary>
    public IDictionary<string, IFailureDumpItem> FailureDumpContainer { get; }
        = new Dictionary<string, IFailureDumpItem>();

    /// <summary>
    /// Gets the current tenant name. When testing sites with multi-tenancy use
    /// <see cref="SwitchCurrentTenant(string, string)"/>.
    /// </summary>
    public string TenantName { get; private set; } = "Default";

    /// <summary>
    /// Gets or sets the prefix used for all relative URLs. It should neither start nor end with a slash.
    /// </summary>
    public string UrlPrefix { get; set; }

    /// <summary>
    /// Gets or sets the current Orchard Core admin prefix. When running UI tests on a site that uses a custom admin
    /// prefix, this value should be set in the test. After that, navigation methods will be able to use the custom
    /// prefix.
    /// </summary>
    public string AdminUrlPrefix { get; set; } = "/Admin";

    public UITestContext(
        string id,
        UITestManifest testManifest,
        OrchardCoreUITestExecutorConfiguration configuration,
        IWebApplicationInstance application,
        AtataScope scope,
        RunningContextContainer runningContextContainer,
        ZapManager zapManager)
    {
        Id = id;
        TestManifest = testManifest;
        Configuration = configuration;
        SqlServerRunningContext = runningContextContainer.SqlServerRunningContext;
        Application = application;
        Scope = scope;
        SmtpServiceRunningContext = runningContextContainer.SmtpServiceRunningContext;
        AzureBlobStorageRunningContext = runningContextContainer.AzureBlobStorageRunningContext;
        ZapManager = zapManager;
    }

    /// <summary>
    /// Updates <see cref="HistoricBrowserLog"/> with current console entries from the browser.
    /// </summary>
    public Task<IReadOnlyList<LogEntry>> UpdateHistoricBrowserLogAsync()
    {
        var windowHandles = Driver.WindowHandles;

        if (windowHandles.Count > 1)
        {
            var currentWindowHandle = Driver.CurrentWindowHandle;

            foreach (var windowHandle in windowHandles)
            {
                // Not using the logging SwitchTo() deliberately as this is not part of what the test does.
                Driver.SwitchTo().Window(windowHandle);
                _historicBrowserLog.AddRange(Driver.GetAndEmptyBrowserLog());
            }

            try
            {
                Driver.SwitchTo().Window(currentWindowHandle);
            }
            catch (NoSuchWindowException)
            {
                // This can happen in rare instances if the current window/tab was just closed.
                Driver.SwitchTo().Window(Driver.WindowHandles[^1]);
            }
        }
        else
        {
            _historicBrowserLog.AddRange(Driver.GetAndEmptyBrowserLog());
        }

        return Task.FromResult<IReadOnlyList<LogEntry>>(_historicBrowserLog);
    }

    /// <summary>
    /// Clears accumulated historic browser log messages from <see cref="HistoricBrowserLog"/>.
    /// </summary>
    public void ClearHistoricBrowserLog() => _historicBrowserLog.Clear();

    /// <summary>
    /// Run an assertion on the browser logs of the current tab with the delegate configured in <see
    /// cref="Configuration"/>. This doesn't use <see cref="HistoricBrowserLog"/>.
    /// </summary>
    public Task AssertCurrentBrowserLogAsync()
    {
        Configuration.AssertBrowserLog?.Invoke(Scope.Driver.GetAndEmptyBrowserLog());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears the application and historic browser logs.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token for reading the application logs.</param>
    public void ClearLogs(CancellationToken cancellationToken = default)
    {
        foreach (var log in Application.GetLogs(cancellationToken)) log.Remove();
        ClearHistoricBrowserLog();
    }

    /// <summary>
    /// Invokes the registered <see cref="PageChangeEventHandler"/> s. Should be called when the browser loads a new
    /// page in the app.
    /// </summary>
    /// <exception cref="PageChangeAssertionException">
    /// Thrown when any of the event handlers throws an exception.
    /// </exception>
    public async Task TriggerAfterPageChangeEventAsync()
    {
        if (IsNoAlert())
        {
            try
            {
                await Configuration.Events.AfterPageChange.InvokeAsync<PageChangeEventHandler>(eventHandler => eventHandler(this));
            }
            catch (Exception exception)
            {
                throw new PageChangeAssertionException(this, exception);
            }
        }
    }

    /// <summary>
    /// Invokes the registered <see cref="PageChangeEventHandler"/> s and refreshes the Atata context with <see
    /// cref="NavigationUITestContextExtensions.RefreshCurrentAtataContext(UITestContext)"/>. Should be called when the
    /// browser loads a new page in the app.
    /// </summary>
    public async Task TriggerAfterPageChangeEventAndRefreshAtataContextAsync()
    {
        await TriggerAfterPageChangeEventAsync();
        this.RefreshCurrentAtataContext();
    }

    /// <summary>
    /// Changes the current tenant context to the Default one. Note that this doesn't navigate the browser.
    /// </summary>
    public void SwitchCurrentTenantToDefault() => SwitchCurrentTenant("Default", string.Empty);

    /// <summary>
    /// Changes the current tenant context to the provided one. Note that this doesn't navigate the browser.
    /// </summary>
    /// <param name="tenantName">The technical name of the tenant to change to.</param>
    /// <param name="urlPrefix">
    /// The URL prefix configured for the tenant. It should neither start nor end with a slash.
    /// </param>
    public void SwitchCurrentTenant(string tenantName, string urlPrefix)
    {
        TenantName = tenantName;
        UrlPrefix = urlPrefix;
        Scope.BaseUri = new Uri(Scope.BaseUri, "/" + UrlPrefix + (string.IsNullOrEmpty(UrlPrefix) ? string.Empty : "/"));
    }

    private bool IsNoAlert()
    {
        // If there's an alert (which can happen mostly after a click but also after navigating) then all other driver
        // operations, even retrieving the current URL, will throw an UnhandledAlertException. Thus we need to check if
        // an alert is present and that's only possible by catching exceptions.
        try
        {
            Driver.SwitchTo().Alert();
            return false;
        }
        catch (NoAlertPresentException)
        {
            return true;
        }
    }
}
