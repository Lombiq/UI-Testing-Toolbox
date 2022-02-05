using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    public class UITestContext
    {
        private readonly List<BrowserLogMessage> _historicBrowserLog = new();

        /// <summary>
        /// Gets the technical name of the current test.
        /// </summary>
        public string TestName { get; }

        /// <summary>
        /// Gets the configuration of the test execution.
        /// </summary>
        public OrchardCoreUITestExecutorConfiguration Configuration { get; }

        /// <summary>
        /// Gets the context for the currently used SQL Server instance and database, if SQL Server is the DB used for
        /// the test.
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
        public RemoteWebDriver Driver => Scope.Driver;

        /// <summary>
        /// Gets the context for the SMTP service running for the test, if it was requested.
        /// </summary>
        public SmtpServiceRunningContext SmtpServiceRunningContext { get; }

        /// <summary>
        /// Gets the context for the currently used Azure Blob Storage folder, if Blob Storage is used for the test.
        /// </summary>
        public AzureBlobStorageRunningContext AzureBlobStorageRunningContext { get; }

        /// <summary>
        /// Gets a cumulative log of browser console entries.
        /// </summary>
        public IReadOnlyList<BrowserLogMessage> HistoricBrowserLog => _historicBrowserLog;

        /// <summary>
        /// Gets a dictionary storing some custom contextual data.
        /// </summary>
        [SuppressMessage(
            "Design",
            "MA0016:Prefer return collection abstraction instead of implementation",
            Justification = "Deliberately modifiable by consumer code.")]
        public Dictionary<string, object> CustomContext { get; } = new();

        /// <summary>
        /// Gets or sets the current tenant name when testing multi-tenancy. When testing sites with multi-tenancy you
        /// should set the value to the tenant in question so methods (e.g. <see
        /// cref="TypedRouteUITestContextExtensions"/>) that use this property can refer to it.
        /// </summary>
        public string TenantName { get; set; } = "Default";

        public UITestContext(
            string testName,
            OrchardCoreUITestExecutorConfiguration configuration,
            SqlServerRunningContext sqlServerContext,
            IWebApplicationInstance application,
            AtataScope scope,
            SmtpServiceRunningContext smtpContext,
            AzureBlobStorageRunningContext blobStorageContext)
        {
            TestName = testName;
            Configuration = configuration;
            SqlServerRunningContext = sqlServerContext;
            Application = application;
            Scope = scope;
            SmtpServiceRunningContext = smtpContext;
            AzureBlobStorageRunningContext = blobStorageContext;
        }

        /// <summary>
        /// Updates <see cref="HistoricBrowserLog"/> with current console entries from the browser.
        /// </summary>
        public async Task<IReadOnlyList<BrowserLogMessage>> UpdateHistoricBrowserLogAsync()
        {
            var windowHandles = Driver.WindowHandles;

            if (windowHandles.Count > 1)
            {
                var currentWindowHandle = Driver.CurrentWindowHandle;

                foreach (var windowHandle in windowHandles)
                {
                    // Not using the logging SwitchTo() deliberately as this is not part of what the test does.
                    Driver.SwitchTo().Window(windowHandle);
                    _historicBrowserLog.AddRange(await Driver.GetAndEmptyBrowserLogAsync());
                }

                try
                {
                    Driver.SwitchTo().Window(currentWindowHandle);
                }
                catch (NoSuchWindowException)
                {
                    // This can happen in rare instances if the current window/tab was just closed.
                    Driver.SwitchTo().Window(Driver.WindowHandles.Last());
                }
            }
            else
            {
                _historicBrowserLog.AddRange(await Driver.GetAndEmptyBrowserLogAsync());
            }

            return _historicBrowserLog;
        }

        /// <summary>
        /// Clears accumulated historic browser log messages from <see cref="HistoricBrowserLog"/>.
        /// </summary>
        public void ClearHistoricBrowserLog() => _historicBrowserLog.Clear();

        /// <summary>
        /// Run an assertion on the browser logs of the current tab with the delegate configured in <see
        /// cref="Configuration"/>. This doesn't use <see cref="HistoricBrowserLog"/>.
        /// </summary>
        public async Task AssertCurrentBrowserLogAsync()
        {
            var browserLog = await Scope.Driver.GetAndEmptyBrowserLogAsync();
            Configuration.AssertBrowserLog?.Invoke(browserLog);
        }

        internal async Task TriggerAfterPageChangeEventAsync()
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

        internal async Task TriggerAfterPageChangeEventAndRefreshAtataContextAsync()
        {
            await TriggerAfterPageChangeEventAsync();
            this.RefreshCurrentAtataContext();
        }

        private bool IsNoAlert()
        {
            // If there's an alert (which can happen mostly after a click but also after navigating) then all other
            // driver operations, even retrieving the current URL, will throw an UnhandledAlertException. Thus we need
            // to check if an alert is present and that's only possible by catching exceptions.
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
}
