using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using OpenQA.Selenium.Remote;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    public class UITestContext
    {
        /// <summary>
        /// Gets data about the currently executing test.
        /// </summary>
        public UITestManifest TestManifest { get; }

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
            UITestManifest testManifest,
            OrchardCoreUITestExecutorConfiguration configuration,
            SqlServerRunningContext sqlServerContext,
            IWebApplicationInstance application,
            AtataScope scope,
            SmtpServiceRunningContext smtpContext,
            AzureBlobStorageRunningContext blobStorageContext)
        {
            TestManifest = testManifest;
            Configuration = configuration;
            SqlServerRunningContext = sqlServerContext;
            Application = application;
            Scope = scope;
            SmtpServiceRunningContext = smtpContext;
            AzureBlobStorageRunningContext = blobStorageContext;
        }

        /// <summary>
        /// Run an assertion on the browser logs of the current tab with the delegate configured in <see
        /// cref="Configuration"/>.
        /// </summary>
        public async Task AssertBrowserLogAsync()
        {
            var browserLog = await Scope.Driver.GetAndEmptyBrowserLogAsync();
            Configuration.AssertBrowserLog?.Invoke(browserLog);
        }
    }
}
