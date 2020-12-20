using Lombiq.Tests.UI.Extensions;
using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    public class UITestContext
    {
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

        public UITestContext(
            string testName,
            OrchardCoreUITestExecutorConfiguration configuration,
            SqlServerRunningContext sqlServerContext,
            IWebApplicationInstance application,
            AtataScope scope,
            SmtpServiceRunningContext smtpContext)
        {
            TestName = testName;
            Configuration = configuration;
            SqlServerRunningContext = sqlServerContext;
            Application = application;
            Scope = scope;
            SmtpServiceRunningContext = smtpContext;
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
